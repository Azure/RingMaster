// <copyright file="RingMasterBackendCore.SecondaryPreprocessor.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;

    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    using Code = Microsoft.Azure.Networking.Infrastructure.RingMaster.Data.RingMasterException.Code;
    using Perm = Microsoft.Azure.Networking.Infrastructure.RingMaster.Data.Acl.Perm;
    using RequestResponse = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestResponse;

    /// <summary>
    /// Backend core - secondary pre-processor
    /// </summary>
    public partial class RingMasterBackendCore
    {
        /// <summary>
        /// Class SecondaryPreprocessor abstracts what a secondary does additionally when processing commands.
        /// </summary>
        protected class SecondaryPreprocessor
        {
            /// <summary>
            /// the path for a command for all replicas
            /// </summary>
            private readonly string thisCommandPathAllReplicas;

            /// <summary>
            /// the path for a command for the primary
            /// </summary>
            private readonly string thisCommandPathPrimary;

            /// <summary>
            /// the path for a command for all secondaries
            /// </summary>
            private readonly string thisCommandPathAllSecondaries;

            /// <summary>
            /// the path for a command for just one secondary (the healthy secondary with smaller id)
            /// </summary>
            private readonly string thisCommandPathOneSecondary;

            /// <summary>
            /// the wire backup object
            /// </summary>
            private WireBackup wirebackup = null;

            /// <summary>
            /// this number is used to synchronize on the transaction Id.
            /// When the txId % runOnTxIdMultiplesOf == 0, we will run the runOnTxIdMultiples action
            /// </summary>
            private int runOnTxIdMultiplesOf;

            /// <summary>
            /// The last txId used to run the runOnTxIdMultiples action
            /// </summary>
            private long lastTxRun;

            /// <summary>
            /// The action to run on every X transactions, based on the txId
            /// </summary>
            private RunOnTxIdMultiplesFunction runOnTxIdMultiples;

            /// <summary>
            /// The current wire backup snapshot identifier
            /// </summary>
            private int wbackupId = 0;

            /// <summary>
            /// the backend this processor works for
            /// </summary>
            private RingMasterBackendCore backend;

            /// <summary>
            /// the cached replica name for this process
            /// </summary>
            private string thisReplicaName = null;

            /// <summary>
            /// the cached path to place commands
            /// </summary>
            private string thisCommandPath = null;

            /// <summary>
            /// Initializes a new instance of the <see cref="SecondaryPreprocessor"/> class.
            /// </summary>
            /// <param name="backend">Backend core</param>
            /// <param name="runOnTxIdMultiplesOf">The action will be run whenever txId % runOnTxIdMultiplesOf == 0.</param>
            /// <param name="runOnTxIdMultiples">The action to run whenever txId % runOnTxIdMultiplesOf == 0.</param>
            public SecondaryPreprocessor(RingMasterBackendCore backend, int runOnTxIdMultiplesOf, RunOnTxIdMultiplesFunction runOnTxIdMultiples)
            {
                if (backend == null)
                {
                    throw new ArgumentNullException(nameof(backend));
                }

                this.backend = backend;

                if (runOnTxIdMultiplesOf < 0)
                {
                    throw new ArgumentException("runOnTxIdMultiplesOf cannot be < 0");
                }

                this.runOnTxIdMultiplesOf = runOnTxIdMultiplesOf;
                this.lastTxRun = 0;

                this.runOnTxIdMultiples = runOnTxIdMultiples;
                this.thisCommandPathAllReplicas = this.backend.ReplicaCommandPathPrefix + "/$$<all>";
                this.thisCommandPathAllSecondaries = this.backend.ReplicaCommandPathPrefix + "/$$<sec>";
                this.thisCommandPathOneSecondary = this.backend.ReplicaCommandPathPrefix + "/$$<onesec>";
                this.thisCommandPathPrimary = this.backend.ReplicaCommandPathPrefix + "/$$<prim>";

                Trace.TraceInformation("this.runOnTxIdMultiples={0}", this.runOnTxIdMultiplesOf);
            }

            /// <summary>
            /// Delegate RunOnTxIdMultiplesFunction. The function to run on TxIds multiples of a given number
            /// </summary>
            /// <param name="txtime">The tx time.</param>
            /// <param name="xid">The tx id.</param>
            public delegate void RunOnTxIdMultiplesFunction(long txtime, long xid);

            /// <summary>
            /// Gets the current wire backup snapshot identifier.
            /// </summary>
            /// <value>The current wire backup identifier.</value>
            public int WbackupId
            {
                get { return this.wbackupId; }
            }

            /// <summary>
            /// Gets the lazily cached replica name for this process
            /// </summary>
            public string ThisReplicaName
            {
                get
                {
                    if (this.thisReplicaName == null)
                    {
                        this.thisReplicaName = this.backend.Factory.Name;
                        this.thisCommandPath = this.backend.ReplicaCommandPathPrefix + "/$$" + this.thisReplicaName;
                    }

                    return this.thisReplicaName;
                }
            }

            /// <summary>
            /// Cancels any ongoing wb snapshot.
            /// </summary>
            public void CancelAnyOngoingActivity()
            {
                // this will cancel any ongoing WB snapshots
                int newId = this.GenerateNewWBackupId();
                Debug.Assert(newId >= 0, "New wire backup ID should be positive");
            }

            /// <summary>
            /// Generates the new wire backup identifier, this will cancal any ongoing wbsnapshot.
            /// </summary>
            /// <returns>the new wire backup identifier</returns>
            internal int GenerateNewWBackupId()
            {
                return Interlocked.Increment(ref this.wbackupId);
            }

            /// <summary>
            /// Sets the wire backup for the preprocessor, and optionally starts it.
            /// </summary>
            /// <param name="wirebackup">The wirebackup.</param>
            /// <param name="start">if set to <c>true</c> start the wirebackup.</param>
            internal void SetWireBackup(WireBackup wirebackup, bool start)
            {
                this.wirebackup = wirebackup;

                if (start && wirebackup != null)
                {
                    this.wirebackup.Start();
                }
            }

            /// <summary>
            /// Stops the wire backup for this preprocessor.
            /// </summary>
            internal void StopWireBackup()
            {
                if (this.wirebackup != null)
                {
                    this.wirebackup.Stop();
                    this.wirebackup = null;
                }
            }

            /// <summary>
            /// Appends the Create operation to the secondary preprocessor
            /// </summary>
            /// <param name="node">The node object.</param>
            /// <param name="txtime">The transaction time.</param>
            /// <param name="xid">The tranaction id.</param>
            internal void AppendCreate(IPersistedData node, long txtime, long xid)
            {
                if (this.wirebackup != null)
                {
                    this.wirebackup.AppendCreate(node, txtime, xid);
                }

                this.DoActionsOnApply(txtime, xid, this.runOnTxIdMultiples);
            }

            /// <summary>
            /// Appends the AddChild operation to the secondary preprocessor
            /// </summary>
            /// <param name="parentId">The parent identifier.</param>
            /// <param name="childId">The child identifier.</param>
            /// <param name="txtime">The transaction time.</param>
            /// <param name="xid">The tranaction id.</param>
            internal void AppendAddChild(ulong parentId, ulong childId, long txtime, long xid)
            {
                if (this.wirebackup != null)
                {
                    this.wirebackup.AppendAddChild(parentId, childId, txtime, xid);
                }

                this.DoActionsOnApply(txtime, xid, this.runOnTxIdMultiples);
            }

            /// <summary>
            /// Appends the RemoveChild operation to the secondary preprocessor
            /// </summary>
            /// <param name="parentId">The parent identifier.</param>
            /// <param name="childId">The child identifier.</param>
            /// <param name="txtime">The transaction time.</param>
            /// <param name="xid">The tranaction id.</param>
            internal void AppendDelete(ulong parentId, ulong childId, long txtime, long xid)
            {
                if (this.wirebackup != null)
                {
                    this.wirebackup.AppendDelete(parentId, childId, txtime, xid);
                }

                this.DoActionsOnApply(txtime, xid, this.runOnTxIdMultiples);
            }

            /// <summary>
            /// Appends the RemoveChild operation to the secondary preprocessor
            /// </summary>
            /// <param name="parentId">The parent identifier.</param>
            /// <param name="childId">The child identifier.</param>
            /// <param name="txtime">The transaction time.</param>
            /// <param name="xid">The tranaction id.</param>
            internal void AppendRemoveChild(ulong parentId, ulong childId, long txtime, long xid)
            {
                if (this.wirebackup != null)
                {
                    this.wirebackup.AppendRemoveChild(parentId, childId, txtime, xid);
                }

                this.DoActionsOnApply(txtime, xid, this.runOnTxIdMultiples);
            }

            /// <summary>
            /// Appends the SetAcl operation to the secondary preprocessor
            /// </summary>
            /// <param name="id">The node identifier.</param>
            /// <param name="list">The Acl set.</param>
            /// <param name="txtime">The transaction time.</param>
            /// <param name="xid">The tranaction id.</param>
            internal void AppendSetAcl(ulong id, IReadOnlyList<Acl> list, long txtime, long xid)
            {
                if (this.wirebackup != null)
                {
                    this.wirebackup.AppendSetAcl(id, list, txtime, xid);
                }

                this.DoActionsOnApply(txtime, xid, this.runOnTxIdMultiples);
            }

            /// <summary>
            /// Appends the SetData operation to the secondary preprocessor
            /// </summary>
            /// <param name="id">The node identifier.</param>
            /// <param name="data">The data set.</param>
            /// <param name="txtime">The transaction time.</param>
            /// <param name="xid">The tranaction id.</param>
            internal void AppendSetData(ulong id, byte[] data, long txtime, long xid)
            {
                if (this.wirebackup != null)
                {
                    this.wirebackup.AppendSetData(id, data, txtime, xid);
                }

                this.DoActionsOnApply(txtime, xid, this.runOnTxIdMultiples);
            }

            /// <summary>
            /// Tries to run the command associated to the node, and encoded in the byte[]
            /// </summary>
            /// <param name="node">the node representing who will run the command</param>
            /// <param name="data">the data encoding the command. data must be a string encoded in UTF8, where the string is a RM command. RM commands are "$/[command]?arguments".</param>
            /// <param name="txtime">the time of the tx which contains the command</param>
            /// <param name="xid">the id of the tx which contains the command</param>
            /// <returns>trus if the command was executed. False otherwise</returns>
            internal bool TryRunCommand(IPersistedData node, byte[] data, long txtime, long xid)
            {
                if (node == null)
                {
                    throw new ArgumentNullException(nameof(node));
                }

                bool didrun = false;
                string path = Node.BuildPath(node);

                if (data == null || !this.CanRunInThisReplica(path))
                {
                    return didrun;
                }

                this.DoActionsOnApply(
                    txtime,
                    xid,
                    (t, i) =>
                    {
                        didrun = true;

                        string requestedCommand = null;
                        object content = null;
                        string command = null;

                        try
                        {
                            string datastr = Encoding.UTF8.GetString(data);
                            byte[] argumentdata = null;
                            int p = datastr.IndexOf('?');

                            if (p == -1)
                            {
                                command = datastr;
                            }
                            else
                            {
                                command = datastr.Substring(0, p);
                                argumentdata = Encoding.UTF8.GetBytes(datastr.Substring(p + 1));
                            }

                            Code res = this.backend.RmCommands.RunCommandPath(command, argumentdata, null, null, out requestedCommand, out content);
                            Trace.TraceInformation("command {0} was run at {1} with result {2}. content={3}", command, this.thisCommandPath, res, content);
                        }
                        catch (Exception e)
                        {
                            Trace.TraceInformation("command {0} was run at {1} (content={2}) with exception {3}. ", command, this.thisCommandPath, content, e);
                        }
                    },
                    false);

                return didrun;
            }

            /// <summary>
            /// sets up the tree for commands, with the given Acls and structure
            /// </summary>
            internal void SetupCommandTree()
            {
                // instantiate a fake session with proper credentials
                ClientSession ses = this.backend.GetLoopbackSession(RMCommands.CommanderDigest);

                try
                {
                    // extract the digest from Auth, and build Acls
                    string[] digestieces = ses.Auth.ClientDigest.Split(':');

                    List<Acl> acls = null;
                    if (digestieces.Length == 2)
                    {
                        acls = new List<Acl>() { new Acl((int)Perm.ALL, new Id(digestieces[0], digestieces[1])) };
                    }

                    // now, build the tree
                    RequestResponse resp;

                    // try to create the path to the commands node, with the Acls
                    resp = this.backend.ProcessMessage(new RequestCreate(this.backend.ReplicaCommandPathPrefix, null, null, acls, CreateMode.PersistentAllowPathCreation, null), ses);
                    Trace.TraceInformation("SetupCommandTree: create({0})-->{1}", this.backend.ReplicaCommandPathPrefix, (Code)resp.ResultCode);

                    bool needsAcl = true;

                    // try to override the ACls, in case those were wrong
                    Stat anyStat = new Stat();
                    anyStat.Aversion = anyStat.Cversion = anyStat.Version = -1;
                    resp = this.backend.ProcessMessage(new RequestGetAcl(this.backend.ReplicaCommandPathPrefix, null, anyStat, null), ses);

                    if (resp != null && resp.ResultCode == (int)Code.Ok)
                    {
                        IList<Acl> prevAcl = resp.Content as IList<Acl>;

                        if (EqualityHelper.Equals(acls, prevAcl))
                        {
                            needsAcl = false;
                        }
                    }

                    if (needsAcl)
                    {
                        // try to override the ACls, in case those were wrong
                        resp = this.backend.ProcessMessage(new RequestSetAcl(this.backend.ReplicaCommandPathPrefix, null, acls, -1, null), ses);
                        Trace.TraceInformation("SetupCommandTree: setacl({0})-->{1}", this.backend.ReplicaCommandPathPrefix, (Code)resp.ResultCode);
                    }
                    else
                    {
                        Trace.TraceInformation("SetupCommandTree: setacl({0})-->{1}", this.backend.ReplicaCommandPathPrefix, "not needed");
                    }

                    // now we add the paths we need to have
                    List<string> paths = new List<string>()
                    {
                        this.thisCommandPathPrimary,
                        this.thisCommandPathOneSecondary,
                        this.thisCommandPathAllReplicas,
                        this.thisCommandPathAllSecondaries,
                    };

                    // append the instance command nodes
                    foreach (ClusterMember m in this.backend.Factory.GetAgreedMembers())
                    {
                        paths.Add(this.backend.ReplicaCommandPathPrefix + "/$$" + m.MemberId);
                    }

                    // create the command nodes
                    foreach (string path in paths)
                    {
                        resp = this.backend.ProcessMessage(new RequestCreate(path, null, null, null, CreateMode.Persistent, null), ses);
                        Trace.TraceInformation("SetupCommandTree: create({0})-->{1}", path, (Code)resp.ResultCode);
                    }
                }
                finally
                {
                    if (ses != null)
                    {
                        ses.Close();
                    }
                }
            }

            /// <summary>
            /// sets up the initial structure for commodity nodes
            /// </summary>
            internal void SetupCommodityTree()
            {
                // instantiate a fake session with proper credentials
                ClientSession ses = this.backend.GetLoopbackSession(RMCommands.CommanderDigest);

                try
                {
                    List<Acl> acls = null;

                    // now, build the tree
                    RequestResponse resp;

                    // try to create the path to the bulkwatcher node
                    string path = BulkOperation.GetBulkWatcherName(null);
                    resp = this.backend.ProcessMessage(new RequestCreate(path, null, null, acls, CreateMode.PersistentAllowPathCreation, null), ses);

                    Trace.TraceInformation("SetupCommodityTree: create({0})-->{1}", path, resp == null ? Code.Unknown : (Code)resp.ResultCode);
                }
                catch (Exception e)
                {
                    Trace.TraceWarning("SetupCommodityTree: ignorable error {0}", e.Message);
                }
                finally
                {
                    if (ses != null)
                    {
                        ses.Close();
                    }
                }
            }

            /// <summary>
            /// performs the actions on apply, and maybe run the scheduled action
            /// </summary>
            /// <param name="txtime">the time the transaction has</param>
            /// <param name="xid">the id for the transaction</param>
            /// <param name="func">the function to run</param>
            /// <param name="onMultiples">if true, this is only a run in multiples of</param>
            private void DoActionsOnApply(long txtime, long xid, RunOnTxIdMultiplesFunction func, bool onMultiples = true)
            {
                // if we already saw this txid (multiple calls to DoActionsOnApply per txid are possible), exit now
                if (xid == this.lastTxRun)
                {
                    return;
                }

                // if we are still loading, exit now
                if (this.ThisReplicaName == null)
                {
                    return;
                }

                // first produce an instrumentation signal indicating we are applying this transaction
                RingMasterServerInstrumentation.Instance.OnApply(txtime, xid);

                // if we don't have a function, exit now
                if (func == null)
                {
                    return;
                }

                // we only run the periodic action if we have a proper configuration, and it is time to run it based on such config
                if (onMultiples && (this.runOnTxIdMultiplesOf == 0 || (xid % this.runOnTxIdMultiplesOf) != 0))
                {
                    return;
                }

                // if the time is too off, exit
                if (Math.Abs((DateTime.UtcNow - MutableStat.ConvertTime(txtime)).TotalSeconds) > 60)
                {
                    Trace.TraceInformation("skipping action because it likely comes from the past");
                    return;
                }

                // set the last txid seen
                this.lastTxRun = xid;

                // do the action
                func(txtime, xid);
            }

            /// <summary>
            /// Indicates if the given path to a command node is suitable for the current replica at this time
            /// </summary>
            /// <param name="path">path to a command node</param>
            /// <returns>true if we should execute the command</returns>
            private bool CanRunInThisReplica(string path)
            {
                if (string.Equals(path, this.thisCommandPath))
                {
                    // if the path is our instance path, we can run it
                    return true;
                }

                if (string.Equals(path, this.thisCommandPathAllReplicas))
                {
                    // if the path is a 'everybody runs', we can run it
                    return true;
                }

                if (this.backend.IsPrimary())
                {
                    // if the path is a 'primary runs' AND we are primary, we can run it
                    return string.Equals(path, this.thisCommandPathPrimary);
                }

                if (string.Equals(path, this.thisCommandPathAllSecondaries))
                {
                    // if the path is a 'secondaries run' AND we are secondary, we can run it
                    return true;
                }

                if (string.Equals(path, this.thisCommandPathOneSecondary))
                {
                    // if the path is a 'oneSecondaries run' AND we are the lowest ID secondary, we can run it

                    // minSec will be the lowest Id of a healthy secondary
                    string minSec = null;

                    foreach (KeyValuePair<string, HealthDefinition> mem in this.backend.Factory.GetHealth())
                    {
                        if (!mem.Value.IsPrimary && mem.Value.HealthRatio > 0.5)
                        {
                            if (minSec == null || minSec.CompareTo(mem.Key) > 0)
                            {
                                minSec = mem.Key;
                            }
                        }
                    }

                    if (string.Equals(minSec, this.ThisReplicaName))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
