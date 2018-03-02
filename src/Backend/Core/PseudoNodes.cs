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

    public class PseudoNodes : IServiceHealingManagerCallbacks
    {
        private const int TimerPeriodMillis = 10000;

        /// <summary>
        /// the backend for this instance 
        /// </summary>
        private readonly RingMasterBackendCore _backend;

        /// <summary>
        /// the function to be used to get the runtime member set
        /// </summary>
        private Func<ClusterMember[]> _getRuntimeMembersetFunction;

        /// <summary>
        /// client to the primary
        /// </summary>
        private AbstractRingMaster _self;

        /// <summary>
        /// The scheduler of background commands
        /// </summary>
        private ScheduledCommand scheduler;

        /// <summary>
        /// The timer for pseudonode management
        /// </summary>
        private Timer _tim;

        /// <summary>
        /// are pseudonodes initialized?
        /// </summary>
        private bool _pseudosInitialized;

        /// <summary>
        /// have we created the primary node
        /// </summary>
        private bool _ephemeralPrimaryAdded;

        /// <summary>
        /// the service healing manager
        /// </summary>
        private readonly ServiceHealingManager _serviceHealingMgr;

        /// <summary>
        /// what is the current string for service healing
        /// </summary>
        private string _serviceHealingString;

        /// <summary>
        /// what is, if any, the current registered path for service healing
        /// </summary>
        private string _registeredPath;

        /// <summary>
        /// min number of members that has to agree in order to change the memberset
        /// </summary>
        private int _minMembers = int.MaxValue;

        /// <summary>
        /// what is, if any, the current mapping for service healing
        /// </summary>
        private Dictionary<string, ClusterMember> _newMapping;

        /// <summary>
        /// The function used to retrieve settings
        /// </summary>
        private readonly Func<string, string> GetSetting;

        /// <summary>
        /// The monitor object to synchronize executions of phseudonodes with other parts of the system
        /// </summary>
        private object monitor = null;

        /// <summary>
        /// The function to use to get a client to the primary
        /// </summary>
        private Func<AbstractRingMaster> getClientToPrimary;

        /// <summary>
        /// constructor for the pseudonodes
        /// </summary>
        /// <param name="backend">the backend to use</param>
        public PseudoNodes(RingMasterBackendCore backend, Func<string, string> getSettingFunction)
        {
            if (getSettingFunction == null)
            {
                getSettingFunction = (s) => null;
            }

            this.GetSetting = getSettingFunction;
            this._backend = backend;
            this._serviceHealingMgr = new ServiceHealingManager(this);
        }

        /// <summary>
        /// installs a routine to be used to retrieve the runtime replica set
        /// </summary>
        /// <param name="getRuntimeMemberset">the routine</param>
        public void SetRuntimeMembersetFunction(Func<ClusterMember[]> getRuntimeMemberset)
        {
            this._getRuntimeMembersetFunction = getRuntimeMemberset;
        }

        #region implementation of IServiceHealingManagerCallbacks
        /// <summary>
        /// return the runtime members
        /// </summary>
        /// <returns>the list of members as per the runtime</returns>
        public ClusterMember[] GetRuntimeMemberset()
        {
            if (this._getRuntimeMembersetFunction == null)
            {
                return null;
            }
            return this._getRuntimeMembersetFunction();
        }

        /// <summary>
        /// return the cluster members
        /// </summary>
        /// <returns>the list of members as agreed by the cluster members themselves (e.g. the codex)</returns>
        public ClusterMember[] GetClusterMemberset()
        {
            return this._backend.Factory.GetAgreedMembers();
        }

        /// <summary>
        /// establishes a candidate for the new member set.
        /// </summary>
        /// <param name="clusterMemberset">cluste memberset</param>
        /// <param name="proposedMemberset">proposed memberset</param>
        public void EnableNewRuntimeMemberset(List<ClusterMember> clusterMemberset, List<ClusterMember> proposedMemberset)
        {
            this._newMapping = null;
            if (clusterMemberset != null && proposedMemberset != null)
            {
                this._newMapping = ServiceHealingManager.GetMapping(clusterMemberset, proposedMemberset);

                if (this._newMapping.Count == 0)
                {
                    this._newMapping = null;
                }
            }

            if (this._newMapping == null)
            {
                this._newMapping = null;
                this._minMembers = int.MaxValue;
                this._serviceHealingString = null;
            }
            else
            {
                if (proposedMemberset == null || clusterMemberset == null)
                {
                    return;
                }

                if (clusterMemberset.Count == proposedMemberset.Count)
                {
                    Trace.TraceInformation("EnableNewRuntimeMemberset: Service Healing detected: {0}", ServiceHealingManager.ToString(this._newMapping.Values));
                }
                else
                {
                    Trace.TraceInformation("EnableNewRuntimeMemberset: Scale Out detected: {0}", ServiceHealingManager.ToString(this._newMapping.Values));
                }

                this._minMembers = (int)(clusterMemberset.Count / 2) + 1;
                this._serviceHealingString = string.Format("Cluster={0}-Runtime={1}", ServiceHealingManager.ToString(clusterMemberset), ServiceHealingManager.ToString(proposedMemberset));
            }
        }

        #endregion IServiceHealingManagerCallbacks

        /// <summary>
        /// Initiates the logics for the pseudonodes runner.
        /// </summary>
        /// <param name="port">The port for this replica if it were the primary. It assumes all replicas listen at the same port</param>
        /// <param name="ssl"></param>
        /// <param name="monitor">the optional object to use to synchronize executions</param>
        public void SetupPseudoNodes(int port, SslWrapping ssl, object monitor = null)
        {
            SetupPseudoNodes(() =>
            {
                return this.GetClientToPrimary(port, ssl);
            }, monitor);
        }

        /// <summary>
        /// Initiates the logics for the pseudonodes runner.
        /// </summary>
        /// <param name="getClientToPrimary">the function to be used to produce a client to the primary</param>
        /// <param name="monitor">the optional object to use to synchronize executions</param>
        public void SetupPseudoNodes(Func<AbstractRingMaster> getClientToPrimary, object monitor = null)
        {
            this._self = null;
            this._serviceHealingMgr.Start();

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

            this._tim = new Timer(_ =>
            {
                try
                {
                    lock (this.monitor)
                    {
                        if (this._tim == null || this._backend.Factory == null || !this._backend.Factory.IsActive)
                        {
                            Trace.TraceInformation("SetupPseudoNodes: disabling pseudonodes timer");
                            this._self.Close();
                            this._tim = null;
                            this._self = null;
                            this._serviceHealingMgr.Stop();
                            return;
                        }

                        OnTimerTriggered();
                    }
                }
                catch (Exception e)
                {
                    //log e and continue:
                    Trace.TraceWarning("SetupPseudoNodes: {0}", e);
                }

                if (this._tim != null && this._backend.Factory != null && this._backend.Factory.IsActive)
                {
                    Timer timer = this._tim;
                    if (timer != null)
                    {
                        timer.Change(TimerPeriodMillis, Timeout.Infinite);
                    }
                }
            }, null, Timeout.Infinite, Timeout.Infinite);

            this._tim.Change(0, Timeout.Infinite);
        }

        public void Stop()
        {
            Timer t = this._tim;

            if (t == null)
            {
                return;
            }

            this._tim = null;
            t.Change(0, Timeout.Infinite);
        }

        /// <summary>
        /// Called when the backend becomes primary to setup the tree structure.
        /// </summary>
        public void SetupTreeStructure()
        {
            LoopbackRingMaster rm = new LoopbackRingMaster(this._backend);
            try
            {
                EnsureBaseTreeBuilt(rm);

                InitializePseudoNodesStructure(rm);
            }
            finally
            {
                rm.Close();
            }
        }

        /// <summary>
        /// executes when the timer is triggered
        /// </summary>
        private void OnTimerTriggered()
        {
            if (!this._backend.IsInFullLockDown)
            {
                InitializeConnectionToPrimary();

                EnsureBaseTreeBuilt(this._self);

                InitializePseudoNodesStructure(this._self);

                // if we are the primary, report health
                if (this._backend.WritesAllowed)
                {
                    CreatePrimaryNodeIfNeeded();

                    UpdateHealthNodes();
                }

                // and all (primary and secondary)... handle replica set voting
                UpdateServiceHealingNodes();

                UpdateCounters();
            }

            UpdateSchedulerState();

            // create/delete the lockdown node
            CreateLockDownNodeIfNeeded();
        }

        private void UpdateSchedulerState()
        {
            bool mustenable = true;

            // Every iteration we will make sure that, if we are primary and we are not in lockdown, 
            // there is a SchedulerCommand, and it is started.
            // and if we are not primary or we are in lockdown, make sure there is no scheduler running anymore.

            if (this._backend == null)
            {
                mustenable = false;
            }
            else
            {
                if (this._backend.IsInFullLockDown)
                {
                    mustenable = false;
                }
                else if (!this._backend.IsPrimary())
                {
                    mustenable = false;
                }
            }

            if (mustenable)
            {
                if (this.scheduler == null)
                {
                    this.scheduler = new ScheduledCommand(() => { return (this._backend != null && this._backend.IsPrimary()); }, new LoopbackRingMaster(this._backend), new MarshallerChannel(null));
                }

                this.scheduler.Start();
            }
            else if (this.scheduler != null)
            {
                this.scheduler.Close();
                this.scheduler = null;
            }
        }

        private string appliedFromConfig = null;

        private ClientSession selfSession = null;

        /// <summary>
        /// ensures that the initial tree from configuration is initialized.
        /// This only happens on the primary, and only once after primary election.
        /// </summary>
        /// <param name="rm">the rm object to use</param>
        private void EnsureBaseTreeBuilt(AbstractRingMaster rm)
        {
            if (rm == null || !this._backend.IsPrimary())
            {
                return;
            }

            try
            {
                string fromConfig = this.GetSetting("RingMaster.InitialTree");
                if (fromConfig == null || String.Equals(appliedFromConfig, fromConfig))
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
                        if (String.Equals(el, "/"))
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
                appliedFromConfig = fromConfig;
            }
            catch (Exception)
            {
                //ignore
            }
        }

        /// <summary>
        /// updates some conuters of the service
        /// </summary>
        private void UpdateCounters()
        {
            if (_backend == null)
            {
                return;
            }

            if (_backend.Factory != null)
            {
                RingMasterServerInstrumentation.Instance.UpdatePersistentNodeCounts(_backend.Factory.TotalData, _backend.Factory.TotalNodes);
            }

            if (_backend.EphemeralFactory != null)
            {
                RingMasterServerInstrumentation.Instance.UpdateEphemeralNodeCounts(_backend.EphemeralFactory.TotalData, _backend.EphemeralFactory.TotalNodes);
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
                if (this._serviceHealingString == null && this._registeredPath == null)
                {
                    //Nothing to be done
                    return;
                }

                // CASE 2: if we were registered for a SH and now there is no SH in flight, 
                if (this._serviceHealingString == null && this._registeredPath != null)
                {
                    Trace.TraceInformation("SH: Cleaning up old SH {0}. ", this._registeredPath);

                    // we need to unregister the SH path we created
                    // and potentially the replica set changed, so we need to re-initiate the client to primary
                    DiscardConnectionToPrimary();
                    return;
                }

                // Here, serviceHealingString != null

                // we may need to register the SH path
                string registeredPathFolder = "/$metadata/servicehealing/" + this._serviceHealingString;
                string tempRegisteredPath = registeredPathFolder + "/" + this._backend.Factory.Name;

                // CASE 3: we were registered on something else, 
                if (this._registeredPath != null && !string.Equals(this._registeredPath, tempRegisteredPath))
                {
                    // unregister
                    Trace.TraceInformation("SH: Abandoning previous SH {0}. ", this._registeredPath);

                    // we need to unregister the SH path we created
                    // and potentially the replica set changed, so we need to re-initiate the client to primary
                    DiscardConnectionToPrimary();
                    return;
                }

                // CASE 4: we were not registered but now we have a SH in hands:

                // this is a new SH. proceed.
                this._registeredPath = tempRegisteredPath;

                Trace.TraceInformation("SH: Installing new SH {0}. ", this._serviceHealingString);

                // NOTE: this will leave a trace of the service healing. We want this!
                this._self.Create(registeredPathFolder, new byte[0], null, CreateMode.Persistent);

                // create the ephemeral 'vote for the replica set change'
                this._self.Create(this._registeredPath, new byte[0], null, CreateMode.Ephemeral);

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
                        DiscardConnectionToPrimary();
                        return;
                    }

                    // we are primary and a node children changed. 
                    if (ev.EventType == WatchedEvent.WatchedEventType.NodeChildrenChanged && this._backend.WritesAllowed)
                    {
                        // re-register the watcher
                        IStat stat = this._self.Exists(registeredPathFolder, watcher, false);

                        // if we have enough 'voters' (note if we are here is because we also voted) 
                        // then change the replica set.
                        if (stat != null && stat.NumChildren >= this._minMembers)
                        {
                            Trace.TraceInformation("SH: Watcher: majority voted.");

                            // delete my vote.. which will unregister the watcher.
                            this._self.Delete(this._registeredPath, -1);

                            // set as not under SH
                            this._serviceHealingString = null;

                            // do the change, and report.
                            if (this._backend.Factory.ChangeAgreedMembers(this._newMapping))
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

                if (this._serviceHealingString != null)
                {
                    // set the watcher
                    this._self.Exists(registeredPathFolder, watcher);
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
            this._registeredPath = null;
            this._self.Close();
            this._self = null;
        }

        /// <summary>
        /// updates the health pesudo-nodes
        /// </summary>
        private void UpdateHealthNodes()
        {
            try
            {
                string basepath = "/$metadata/health";
                Dictionary<string, HealthDefinition> health = this._backend.Factory.GetHealth();

                List<Op> ops = new List<Op>();

                foreach (string child in this._self.GetChildren(basepath, false))
                {
                    ops.Add(Op.Delete(basepath + "/" + child, -1, DeleteMode.SuccessEvenIfNodeDoesntExist));
                }

                foreach (KeyValuePair<string, HealthDefinition> line in health)
                {
                    ops.Add(Op.Delete(basepath + "/" + line.Key + " : " + line.Value.Description, -1, DeleteMode.SuccessEvenIfNodeDoesntExist));
                    ops.Add(Op.Create(basepath + "/" + line.Key + " : " + line.Value.Description, null, null, CreateMode.Ephemeral));
                }

                string clusterpath = "/$metadata/clusterreplicaset";

                foreach (string child in this._self.GetChildren(clusterpath, false))
                {
                    ops.Add(Op.Delete(clusterpath + "/" + child, -1));
                }

                ops.Add(Op.Create(clusterpath + "/" + ServiceHealingManager.ToString(GetClusterMemberset()), null, null, CreateMode.Ephemeral | CreateMode.SuccessEvenIfNodeExistsFlag));

                this._self.Multi(ops.AsReadOnly(), true, null, 0);
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
            if (!this._pseudosInitialized)
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
                this._pseudosInitialized = true;
            }
        }

        /// <summary>
        /// initializes the connection string to the primary.
        /// </summary>
        private void InitializeConnectionToPrimary()
        {
            // if we don't have a hold on the primary yet... initialize the client
            if (this._self != null)
            {
                return;
            }

            this._self = this.getClientToPrimary();
        }

        internal RingMaster GetClientToPrimary(int port, SslWrapping ssl)
        {
            return PseudoNodes.GetClientToPrimary(this._backend, port, ssl);
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
        /// creates the ephemeral node representing the primary
        /// </summary>
        private void CreatePrimaryNodeIfNeeded()
        {
            try
            {
                if (!this._ephemeralPrimaryAdded)
                {
                    this._ephemeralPrimaryAdded = true;
                    try
                    {
                        string primaryNodeString = string.Format("/$metadata/primary/{0} (PRIMARY since {1}) Binary={2}", this._backend.Factory.Name, DateTime.UtcNow.ToString("o"), GetAssemblyDescription());

                        this._self.Create(primaryNodeString, null, null, CreateMode.Ephemeral);
                        this._self.Exists(primaryNodeString, new DelegateWatcher(ev =>
                        {
                            if (ev.EventType == WatchedEvent.WatchedEventType.WatcherRemoved || ev.EventType == WatchedEvent.WatchedEventType.NodeDeleted)
                            {
                                this._ephemeralPrimaryAdded = false;
                            }
                        }));
                    }
                    catch (Exception)
                    {
                        this._ephemeralPrimaryAdded = false;
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceWarning("While CreatePrimaryNodeIfNeeded: {0}", e);
            }
        }
        
        /// <summary>
        /// creates the ephemeral node representing the lockdown, if needed
        /// </summary>
        internal void CreateLockDownNodeIfNeeded()
        {
            if (!this._backend.Factory.IsActive)
            {
                return;
            }

            try
            {
                if (this._backend.IsInFullLockDown)
                {
                    if (selfSession == null)
                    {
                        selfSession = this._backend.GetLoopbackSession(RingMasterBackendCore.RootDigest, true);
                    }

                    const string lockDownNodeString = "/$metadata/lockDown";

                    this._backend.ProcessMessage(new RequestCreate(lockDownNodeString, null, null, null, CreateMode.Ephemeral, null), selfSession, (RequestResponse res) =>
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

            return String.Empty;
        }
    }
}
