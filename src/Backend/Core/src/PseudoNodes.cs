// <copyright file="PseudoNodes.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Reflection;
    using System.Threading;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Watcher.Event;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// Manages the cluster pseudo nodes
    /// </summary>
    public class PseudoNodes : IServiceHealingManagerCallbacks
    {
        private const int TimerPeriodMillis = 10000;

        /// <summary>
        /// the backend for this instance
        /// </summary>
        private readonly RingMasterBackendCore backend;

        /// <summary>
        /// the service healing manager
        /// </summary>
        private readonly ServiceHealingManager serviceHealingMgr;

        /// <summary>
        /// The function used to retrieve settings
        /// </summary>
        private readonly Func<string, string> getSetting;

        /// <summary>
        /// the function to be used to get the runtime member set
        /// </summary>
        private Func<ClusterMember[]> getRuntimeMembersetFunction;

        /// <summary>
        /// client to the primary
        /// </summary>
        private AbstractRingMaster self;

        /// <summary>
        /// The scheduler of background commands
        /// </summary>
        private ScheduledCommand scheduler;

        /// <summary>
        /// The timer for pseudonode management
        /// </summary>
        private Timer timer;

        /// <summary>
        /// are pseudonodes initialized?
        /// </summary>
        private bool pseudosInitialized;

        /// <summary>
        /// have we created the primary node
        /// </summary>
        private bool ephemeralPrimaryAdded;

        /// <summary>
        /// what is the current string for service healing
        /// </summary>
        private string serviceHealingString;

        /// <summary>
        /// what is, if any, the current registered path for service healing
        /// </summary>
        private string registeredPath;

        /// <summary>
        /// min number of members that has to agree in order to change the memberset
        /// </summary>
        private int minMembers = int.MaxValue;

        /// <summary>
        /// what is, if any, the current mapping for service healing
        /// </summary>
        private Dictionary<string, ClusterMember> newMapping;

        /// <summary>
        /// The monitor object to synchronize executions of phseudonodes with other parts of the system
        /// </summary>
        private object monitor = null;

        /// <summary>
        /// The function to use to get a client to the primary
        /// </summary>
        private Func<AbstractRingMaster> getClientToPrimary;

        private string appliedFromConfig = null;

        private ClientSession selfSession = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="PseudoNodes"/> class.
        /// </summary>
        /// <param name="backend">the backend to use</param>
        /// <param name="getSettingFunction">Function to get settings</param>
        public PseudoNodes(RingMasterBackendCore backend, Func<string, string> getSettingFunction)
        {
            if (getSettingFunction == null)
            {
                getSettingFunction = (s) => null;
            }

            this.getSetting = getSettingFunction;
            this.backend = backend;
            this.serviceHealingMgr = new ServiceHealingManager(this);
        }

        /// <summary>
        /// installs a routine to be used to retrieve the runtime replica set
        /// </summary>
        /// <param name="getRuntimeMemberset">the routine</param>
        public void SetRuntimeMembersetFunction(Func<ClusterMember[]> getRuntimeMemberset)
        {
            this.getRuntimeMembersetFunction = getRuntimeMemberset;
        }

        /// <summary>
        /// return the runtime members
        /// </summary>
        /// <returns>the list of members as per the runtime</returns>
        public ClusterMember[] GetRuntimeMemberset()
        {
            if (this.getRuntimeMembersetFunction == null)
            {
                return null;
            }

            return this.getRuntimeMembersetFunction();
        }

        /// <summary>
        /// return the cluster members
        /// </summary>
        /// <returns>the list of members as agreed by the cluster members themselves (e.g. the codex)</returns>
        public ClusterMember[] GetClusterMemberset()
        {
            return this.backend.Factory.GetAgreedMembers();
        }

        /// <summary>
        /// establishes a candidate for the new member set.
        /// </summary>
        /// <param name="clusterMemberset">cluste memberset</param>
        /// <param name="proposedMemberset">proposed memberset</param>
        public void EnableNewRuntimeMemberset(List<ClusterMember> clusterMemberset, List<ClusterMember> proposedMemberset)
        {
            this.newMapping = null;
            if (clusterMemberset != null && proposedMemberset != null)
            {
                this.newMapping = ServiceHealingManager.GetMapping(clusterMemberset, proposedMemberset);

                if (this.newMapping.Count == 0)
                {
                    this.newMapping = null;
                }
            }

            if (this.newMapping == null)
            {
                this.newMapping = null;
                this.minMembers = int.MaxValue;
                this.serviceHealingString = null;
            }
            else
            {
                if (proposedMemberset == null || clusterMemberset == null)
                {
                    return;
                }

                if (clusterMemberset.Count == proposedMemberset.Count)
                {
                    Trace.TraceInformation("EnableNewRuntimeMemberset: Service Healing detected: {0}", ServiceHealingManager.ToString(this.newMapping.Values));
                }
                else
                {
                    Trace.TraceInformation("EnableNewRuntimeMemberset: Scale Out detected: {0}", ServiceHealingManager.ToString(this.newMapping.Values));
                }

                this.minMembers = (int)(clusterMemberset.Count / 2) + 1;
                this.serviceHealingString = string.Format("Cluster={0}-Runtime={1}", ServiceHealingManager.ToString(clusterMemberset), ServiceHealingManager.ToString(proposedMemberset));
            }
        }

        /// <summary>
        /// Initiates the logics for the pseudonodes runner.
        /// </summary>
        /// <param name="port">The port for this replica if it were the primary. It assumes all replicas listen at the same port</param>
        /// <param name="ssl">SSL wrapping object</param>
        /// <param name="monitor">the optional object to use to synchronize executions</param>
        public void SetupPseudoNodes(int port, SslWrapping ssl, object monitor = null)
        {
            this.SetupPseudoNodes(
                () =>
                {
                    return this.GetClientToPrimary(port, ssl);
                },
                monitor);
        }

        /// <summary>
        /// Initiates the logics for the pseudonodes runner.
        /// </summary>
        /// <param name="getClientToPrimary">the function to be used to produce a client to the primary</param>
        /// <param name="monitor">the optional object to use to synchronize executions</param>
        public void SetupPseudoNodes(Func<AbstractRingMaster> getClientToPrimary, object monitor = null)
        {
            this.self = null;
            this.serviceHealingMgr.Start();

            if (monitor == null)
            {
                this.monitor = new object();
            }
            else
            {
                this.monitor = monitor;
            }

            if (getClientToPrimary == null)
            {
                throw new ArgumentNullException("getClientToPrimary");
            }

            this.getClientToPrimary = getClientToPrimary;

            this.timer = new Timer(
                _ =>
                {
                    try
                    {
                        lock (this.monitor)
                        {
                            if (this.timer == null || this.backend.Factory == null || !this.backend.Factory.IsActive)
                            {
                                Trace.TraceInformation("SetupPseudoNodes: disabling pseudonodes timer");
                                this.self.Close();
                                this.timer = null;
                                this.self = null;
                                this.serviceHealingMgr.Stop();
                                return;
                            }

                            this.OnTimerTriggered();
                        }
                    }
                    catch (Exception e)
                    {
                        // log e and continue:
                        Trace.TraceWarning("SetupPseudoNodes: {0}", e);
                    }

                    if (this.timer != null && this.backend.Factory != null && this.backend.Factory.IsActive)
                    {
                        Timer timer = this.timer;
                        if (timer != null)
                        {
                            timer.Change(TimerPeriodMillis, Timeout.Infinite);
                        }
                    }
                },
                null,
                Timeout.Infinite,
                Timeout.Infinite);

            this.timer.Change(0, Timeout.Infinite);
        }

        /// <summary>
        /// Stops this instance
        /// </summary>
        public void Stop()
        {
            Timer t = this.timer;

            if (t == null)
            {
                return;
            }

            this.timer = null;
            t.Change(0, Timeout.Infinite);
        }

        /// <summary>
        /// Called when the backend becomes primary to setup the tree structure.
        /// </summary>
        public void SetupTreeStructure()
        {
            LoopbackRingMaster rm = new LoopbackRingMaster(this.backend);
            try
            {
                this.EnsureBaseTreeBuilt(rm);

                this.InitializePseudoNodesStructure(rm);
            }
            finally
            {
                rm.Close();
            }
        }

        /// <summary>
        /// Gets a client to the primary.
        /// </summary>
        /// <param name="backend">The backend.</param>
        /// <param name="port">The port.</param>
        /// <param name="ssl">The SSL.</param>
        /// <returns>the new client to the primary</returns>
        internal static RingMaster GetClientToPrimary(RingMasterBackendCore backend, int port, SslWrapping ssl)
        {
            if (backend == null)
            {
                throw new ArgumentNullException("backend");
            }

            // addr will contain the list of peers, or '127.0.0.1' if none is there
            string addr = string.Empty;

            ClusterMember[] cm = backend.Factory.GetAgreedMembers();

            if (cm != null)
            {
                foreach (ClusterMember m in cm)
                {
                    if (addr == string.Empty)
                    {
                        addr = string.Format("{0}:{1}", m.Address, port);
                    }
                    else
                    {
                        addr = string.Format("{0};{1}:{2}", addr, m.Address, port);
                    }
                }
            }
            else
            {
                // nothing on cm, just us.
                addr = "127.0.0.1:" + port;
            }

            Trace.TraceInformation("SetupPseudoNodes {0} ", addr);

            RingMaster rm = new RingMaster(addr, 1000, null);
            rm.AddAuthInfo(AuthSchemes.Digest, "root");
            rm.SetSsl(ssl);
            return rm;
        }

        /// <summary>
        /// Gets a client to the primary
        /// </summary>
        /// <param name="port">Port number</param>
        /// <param name="ssl">SSL wrapping object</param>
        /// <returns>New clien to the primary</returns>
        internal RingMaster GetClientToPrimary(int port, SslWrapping ssl)
        {
            return PseudoNodes.GetClientToPrimary(this.backend, port, ssl);
        }

        /// <summary>
        /// creates the ephemeral node representing the lockdown, if needed
        /// </summary>
        internal void CreateLockDownNodeIfNeeded()
        {
            if (!this.backend.Factory.IsActive)
            {
                return;
            }

            try
            {
                if (this.backend.IsInFullLockDown)
                {
                    if (this.selfSession == null)
                    {
                        this.selfSession = this.backend.GetLoopbackSession(RingMasterBackendCore.RootDigest, true);
                    }

                    const string lockDownNodeString = "/$metadata/lockDown";

                    this.backend.ProcessMessage(
                        new RequestCreate(lockDownNodeString, null, null, null, CreateMode.Ephemeral, null),
                        this.selfSession,
                        (RequestResponse res, Exception ex) =>
                        {
                            if (res == null)
                            {
                                Trace.TraceWarning("Lockdown pseudonode couldnt be created: result was null");
                                return;
                            }

                            if (res.ResultCode == (int)RingMasterException.Code.Ok)
                            {
                                Trace.TraceWarning("Lockdown pseudonode created");
                            }
                            else if (res.ResultCode != (int)RingMasterException.Code.Nodeexists)
                            {
                                Trace.TraceWarning("Lockdown pseudonode couldnt be created: {0}", KeeperException.ExceptionHelper.GetCodeString(res.ResultCode));
                            }
                        });
                }
            }
            catch (Exception e)
            {
                Trace.TraceWarning("While CreateLockDownNodeIfNeeded: {0}", e);
            }
        }

        /// <summary>
        /// returns the string with the creation time of the file path, if possible
        /// </summary>
        /// <param name="location">path to the file</param>
        /// <returns>the assembly description</returns>
        private static string GetCreationTime(string location)
        {
            try
            {
                FileInfo fi = new FileInfo(location);
                if (fi.Exists)
                {
                    return fi.CreationTimeUtc.ToString("o");
                }
            }
            catch (Exception)
            {
                // ignore
            }

            return string.Empty;
        }

        /// <summary>
        /// executes when the timer is triggered
        /// </summary>
        private void OnTimerTriggered()
        {
            if (!this.backend.IsInFullLockDown)
            {
                this.InitializeConnectionToPrimary();

                this.EnsureBaseTreeBuilt(this.self);

                this.InitializePseudoNodesStructure(this.self);

                // if we are the primary, report health
                if (this.backend.WritesAllowed)
                {
                    this.CreatePrimaryNodeIfNeeded();

                    this.UpdateHealthNodes();
                }

                // and all (primary and secondary)... handle replica set voting
                this.UpdateServiceHealingNodes();

                this.UpdateCounters();
            }

            this.UpdateSchedulerState();

            // create/delete the lockdown node
            this.CreateLockDownNodeIfNeeded();
        }

        private void UpdateSchedulerState()
        {
            bool mustenable = true;

            // Every iteration we will make sure that, if we are primary and we are not in lockdown,
            // there is a SchedulerCommand, and it is started.
            // and if we are not primary or we are in lockdown, make sure there is no scheduler running anymore.
            if (this.backend == null)
            {
                mustenable = false;
            }
            else
            {
                if (this.backend.IsInFullLockDown)
                {
                    mustenable = false;
                }
                else if (!this.backend.IsPrimary())
                {
                    mustenable = false;
                }
            }

            if (mustenable)
            {
                if (this.scheduler == null)
                {
                    this.scheduler = new ScheduledCommand(
                        () => { return this.backend != null && this.backend.IsPrimary(); },
                        new LoopbackRingMaster(this.backend),
                        new MarshallerChannel(null));
                }

                this.scheduler.Start();
            }
            else if (this.scheduler != null)
            {
                this.scheduler.Close();
                this.scheduler = null;
            }
        }

        /// <summary>
        /// ensures that the initial tree from configuration is initialized.
        /// This only happens on the primary, and only once after primary election.
        /// </summary>
        /// <param name="rm">the rm object to use</param>
        private void EnsureBaseTreeBuilt(AbstractRingMaster rm)
        {
            if (rm == null || !this.backend.IsPrimary())
            {
                return;
            }

            try
            {
                string fromConfig = this.getSetting("RingMaster.InitialTree");
                if (fromConfig == null || string.Equals(this.appliedFromConfig, fromConfig))
                {
                    return;
                }

                string[] elements = fromConfig.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string elem in elements)
                {
                    string el = elem;
                    if (el.StartsWith("-"))
                    {
                        el = el.Substring(1);
                        if (string.Equals(el, "/"))
                        {
                            Trace.TraceError("It is not allowed to cleanup the whole tree");
                            throw new InvalidOperationException("It is not allowed to cleanup the whole tree");
                        }

                        IStat s = rm.Exists(el, false, false);
                        if (s != null)
                        {
                            rm.Delete(el, s.Version, DeleteMode.CascadeDelete);
                            Trace.TraceInformation("PseudoNodes.Init: Deleting " + el);
                        }
                    }
                    else
                    {
                        rm.Create(el, null, null, CreateMode.PersistentAllowPathCreation);
                        Trace.TraceInformation("PseudoNodes.Init: Creating " + el);
                    }
                }

                this.appliedFromConfig = fromConfig;
            }
            catch (Exception)
            {
                // ignore
            }
        }

        /// <summary>
        /// updates some conuters of the service
        /// </summary>
        private void UpdateCounters()
        {
            if (this.backend == null)
            {
                return;
            }

            if (this.backend.Factory != null)
            {
                RingMasterServerInstrumentation.Instance.UpdatePersistentNodeCounts(this.backend.Factory.TotalData, this.backend.Factory.TotalNodes);
            }

            if (this.backend.EphemeralFactory != null)
            {
                RingMasterServerInstrumentation.Instance.UpdateEphemeralNodeCounts(this.backend.EphemeralFactory.TotalData, this.backend.EphemeralFactory.TotalNodes);
            }
        }

        /// <summary>
        /// updates the tree nodes regarding service healing
        /// </summary>
        private void UpdateServiceHealingNodes()
        {
            try
            {
                // CASE 1. no SH ongoing, and no SH detected.
                if (this.serviceHealingString == null && this.registeredPath == null)
                {
                    // Nothing to be done
                    return;
                }

                // CASE 2: if we were registered for a SH and now there is no SH in flight,
                if (this.serviceHealingString == null && this.registeredPath != null)
                {
                    Trace.TraceInformation("SH: Cleaning up old SH {0}. ", this.registeredPath);

                    // we need to unregister the SH path we created
                    // and potentially the replica set changed, so we need to re-initiate the client to primary
                    this.DiscardConnectionToPrimary();
                    return;
                }

                // Here, serviceHealingString != null

                // we may need to register the SH path
                string registeredPathFolder = "/$metadata/servicehealing/" + this.serviceHealingString;
                string tempRegisteredPath = registeredPathFolder + "/" + this.backend.Factory.Name;

                // CASE 3: we were registered on something else,
                if (this.registeredPath != null && !string.Equals(this.registeredPath, tempRegisteredPath))
                {
                    // unregister
                    Trace.TraceInformation("SH: Abandoning previous SH {0}. ", this.registeredPath);

                    // we need to unregister the SH path we created
                    // and potentially the replica set changed, so we need to re-initiate the client to primary
                    this.DiscardConnectionToPrimary();
                    return;
                }

                // CASE 4: we were not registered but now we have a SH in hands:

                // this is a new SH. proceed.
                this.registeredPath = tempRegisteredPath;

                Trace.TraceInformation("SH: Installing new SH {0}. ", this.serviceHealingString);

                // NOTE: this will leave a trace of the service healing. We want this!
                this.self.Create(registeredPathFolder, new byte[0], null, CreateMode.Persistent);

                // create the ephemeral 'vote for the replica set change'
                this.self.Create(this.registeredPath, new byte[0], null, CreateMode.Ephemeral);

                // register a watcher to notice if we were unregister (primary failover, or something else)
                IWatcher watcher = null;

                watcher = new DelegateWatcher(ev =>
                {
                    Trace.TraceInformation("SH: Watcher: {0} {1}. ", ev.EventType, ev.Path);

                    // we were unregistered (either the node was deleted or the primary died).
                    if (ev.EventType == WatchedEvent.WatchedEventType.WatcherRemoved || ev.EventType == WatchedEvent.WatchedEventType.NodeDeleted)
                    {
                        Trace.TraceInformation("SH: Watcher: removing registration and flushing self");

                        // record we are not registered and trust the monitors will kick in again
                        this.DiscardConnectionToPrimary();
                        return;
                    }

                    // we are primary and a node children changed.
                    if (ev.EventType == WatchedEvent.WatchedEventType.NodeChildrenChanged && this.backend.WritesAllowed)
                    {
                        // re-register the watcher
                        IStat stat = this.self.Exists(registeredPathFolder, watcher, false);

                        // if we have enough 'voters' (note if we are here is because we also voted)
                        // then change the replica set.
                        if (stat != null && stat.NumChildren >= this.minMembers)
                        {
                            Trace.TraceInformation("SH: Watcher: majority voted.");

                            // delete my vote.. which will unregister the watcher.
                            this.self.Delete(this.registeredPath, -1);

                            // set as not under SH
                            this.serviceHealingString = null;

                            // do the change, and report.
                            if (this.backend.Factory.ChangeAgreedMembers(this.newMapping))
                            {
                                Trace.TraceInformation("SH: Watcher: Change happened. cleaning up.");
                            }
                            else
                            {
                                Trace.TraceInformation("SH: Watcher: Change failed!");
                            }
                        }
                        else
                        {
                            Trace.TraceInformation("SH: Watcher: majority not voted yet ({0}).", stat == null ? "<null>" : stat.NumChildren.ToString());
                        }
                    }
                }); // note, this is a one use watcher.

                // invoke the watcher once, just in case we have already enough children
                watcher.Process(new WatchedEvent(WatchedEvent.WatchedEventType.NodeChildrenChanged, WatchedEvent.WatchedEventKeeperState.SyncConnected, string.Empty));

                if (this.serviceHealingString != null)
                {
                    // set the watcher
                    this.self.Exists(registeredPathFolder, watcher);
                }
            }
            catch (Exception e)
            {
                Trace.TraceWarning("While UpdateServiceHealingNodes: {0}", e);
            }
        }

        /// <summary>
        /// discards the connection to the primary, which will unregister all callbacks and destroy all ephemerals.
        /// </summary>
        private void DiscardConnectionToPrimary()
        {
            this.registeredPath = null;
            this.self.Close();
            this.self = null;
        }

        /// <summary>
        /// updates the health pesudo-nodes
        /// </summary>
        private void UpdateHealthNodes()
        {
            try
            {
                string basepath = "/$metadata/health";
                Dictionary<string, HealthDefinition> health = this.backend.Factory.GetHealth();

                List<Op> ops = new List<Op>();

                foreach (string child in this.self.GetChildren(basepath, false))
                {
                    ops.Add(Op.Delete(basepath + "/" + child, -1, DeleteMode.SuccessEvenIfNodeDoesntExist));
                }

                foreach (KeyValuePair<string, HealthDefinition> line in health)
                {
                    ops.Add(Op.Delete(basepath + "/" + line.Key + " : " + line.Value.Description, -1, DeleteMode.SuccessEvenIfNodeDoesntExist));
                    ops.Add(Op.Create(basepath + "/" + line.Key + " : " + line.Value.Description, null, null, CreateMode.Ephemeral));
                }

                string clusterpath = "/$metadata/clusterreplicaset";

                foreach (string child in this.self.GetChildren(clusterpath, false))
                {
                    ops.Add(Op.Delete(clusterpath + "/" + child, -1));
                }

                ops.Add(Op.Create(clusterpath + "/" + ServiceHealingManager.ToString(this.GetClusterMemberset()), null, null, CreateMode.Ephemeral | CreateMode.SuccessEvenIfNodeExistsFlag));

                this.self.Multi(ops.AsReadOnly(), true, null, 0);
            }
            catch (Exception e)
            {
                Trace.TraceWarning("While UpdateHealthNodes: {0}", e);
            }
        }

        /// <summary>
        /// initializes the pseudonodes tree structure
        /// </summary>
        /// <param name="rm">the rm object to use</param>
        private void InitializePseudoNodesStructure(AbstractRingMaster rm)
        {
            // ensure the tree structure is built
            if (!this.pseudosInitialized)
            {
                rm.Create("/$bulkwatcher", null, null, CreateMode.Persistent);
                rm.Create("/$metadata", null, null, CreateMode.Persistent);

                foreach (string path in ScheduledCommand.GetPaths())
                {
                    rm.Create(path, null, null, CreateMode.PersistentAllowPathCreation);
                }

                rm.Delete("/$metadata/ring", -1);
                rm.Create("/$metadata/clusterreplicaset", null, null, CreateMode.Persistent);
                rm.Create("/$metadata/health", null, null, CreateMode.Persistent);
                rm.Create("/$metadata/servicehealing", null, null, CreateMode.Persistent);
                rm.Create("/$metadata/primary", null, null, CreateMode.Persistent);
                rm.Create("/$metadata/synchronization/$syncpoint", null, null, CreateMode.PersistentAllowPathCreation);

                Trace.TraceInformation("PseudoNodes initialized");
                this.pseudosInitialized = true;
            }
        }

        /// <summary>
        /// initializes the connection string to the primary.
        /// </summary>
        private void InitializeConnectionToPrimary()
        {
            // if we don't have a hold on the primary yet... initialize the client
            if (this.self != null)
            {
                return;
            }

            this.self = this.getClientToPrimary();
        }

        /// <summary>
        /// creates the ephemeral node representing the primary
        /// </summary>
        private void CreatePrimaryNodeIfNeeded()
        {
            try
            {
                if (!this.ephemeralPrimaryAdded)
                {
                    this.ephemeralPrimaryAdded = true;
                    try
                    {
                        string primaryNodeString = string.Format("/$metadata/primary/{0} (PRIMARY since {1}) Binary={2}", this.backend.Factory.Name, DateTime.UtcNow.ToString("o"), this.GetAssemblyDescription());

                        this.self.Create(primaryNodeString, null, null, CreateMode.Ephemeral);
                        this.self.Exists(primaryNodeString, new DelegateWatcher(ev =>
                        {
                            if (ev.EventType == WatchedEvent.WatchedEventType.WatcherRemoved || ev.EventType == WatchedEvent.WatchedEventType.NodeDeleted)
                            {
                                this.ephemeralPrimaryAdded = false;
                            }
                        }));
                    }
                    catch (Exception)
                    {
                        this.ephemeralPrimaryAdded = false;
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceWarning("While CreatePrimaryNodeIfNeeded: {0}", e);
            }
        }

        /// <summary>
        /// returns a string with the assembly description
        /// </summary>
        /// <returns>the assembly description</returns>
        private string GetAssemblyDescription()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;

            string description = "<unknown>";
            foreach (Attribute v in assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute)))
            {
                AssemblyDescriptionAttribute at = v as AssemblyDescriptionAttribute;
                if (at != null)
                {
                    description = at.Description;
                    break;
                }
            }

            return string.Format("{0} version {1} built at {2}", description, version, GetCreationTime(assembly.Location));
        }
    }
}
