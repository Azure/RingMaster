// ***********************************************************************
// Assembly         : RingMaster
// <copyright file="EphemeralFactory.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Class EphemeralFactory.
    /// This factory is used to keep ephemeral nodes (i.e. not replicated nor persisted).
    /// </summary>
    public class EphemeralFactory : IPersistedDataFactory<Node>
    {
        /// <summary>
        /// Class PersistedData.
        /// </summary>
        public class PersistedData : IPersistedData
        {
            /// <summary>
            /// Ensures the data is fresh before reading it. May block the call until it is fresh
            /// </summary>
            /// <param name="chgs">The changelist.</param>
            public void AppendRead(ref IChangeList chgs)
            {
            }

            /// <summary>
            /// The _uid
            /// </summary>
            private static UIdProvider _uid = new UIdProvider();

            /// <summary>
            /// Initializes a new instance of the <see cref="PersistedData"/> class.
            /// </summary>
            public PersistedData()
            {
                this.Stat = new MutableStat();
                this.Id = _uid.NextUniqueId();
            }

            /// <summary>
            /// Gets a value indicating whether this instance is ephemeral.
            /// </summary>
            /// <value><c>true</c> if this instance is ephemeral; otherwise, <c>false</c>.</value>
            public bool IsEphemeral { get { return true; } }

            /// <summary>
            /// Gets or sets the node.
            /// </summary>
            /// <value>The node.</value>
            public Node Node { get; set; }

            /// <summary>
            /// Gets the saved zxid.
            /// </summary>
            /// <value>The saved zxid.</value>
            public long SavedZxid { get { return this.Stat.Mzxid; } }

            /// <summary>
            /// Gets or sets the identifier.
            /// </summary>
            /// <value>The identifier.</value>
            public ulong Id { get; set; }

            /// <summary>
            /// the parent. Not to be set directly!
            /// </summary>
            public IPersistedData Parent { get; set; }

            /// <summary>
            /// Gets or sets the stat.
            /// </summary>
            /// <value>The stat.</value>
            public IMutableStat Stat { get; set; }

            /// <summary>
            /// Gets or sets the data.
            /// </summary>
            /// <value>The data.</value>
            public byte[] Data { get; set; }

            /// <summary>
            /// the count of children.
            /// </summary>
            private int _childrenCount = 0;

            /// <summary>
            /// Deletes the specified instance.
            /// </summary>
            public void Delete()
            {
                this.Parent = null;
            }

            /// <summary>
            /// Adds the child.
            /// </summary>
            /// <param name="child">The child.</param>
            public void AddChild(IPersistedData child)
            {
                if (child == null)
                {
                    throw new ArgumentNullException("child");
                }

                if (child.Parent != null)
                {
                    child.Parent.RemoveChild(child);
                }
                child.Parent = this;
                this._childrenCount++;
            }

            /// <summary>
            /// Removes the child.
            /// </summary>
            /// <param name="child">The child.</param>
            public void RemoveChild(IPersistedData child)
            {
                if (child == null)
                {
                    throw new ArgumentNullException("child");
                }

                if (child.Parent == null)
                {
                    return;
                }

                if (child.Parent != this)
                {
                    RmAssert.Fail("cannot remove a node that is not a child.");
                }
                child.Parent = null;
                this._childrenCount--;
            }

            /// <summary>
            /// Gets the children count.
            /// </summary>
            /// <returns>the count of children</returns>
            public int GetChildrenCount()
            {
                return this._childrenCount;
            }

            /// <summary>
            /// Gets or sets the acl.
            /// </summary>
            /// <value>The acl.</value>
            public IReadOnlyList<Acl> Acl { get; set; }

            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            /// <value>The name.</value>
            public string Name { get; set; }

            /// <summary>
            /// Appends the create.
            /// </summary>
            /// <param name="chgs">The CHGS.</param>
            /// <returns>IChangeList.</returns>
            public void AppendCreate(ref IChangeList chgs)
            {
            }

            /// <summary>
            /// Appends the add child.
            /// </summary>
            /// <param name="chgs">The CHGS.</param>
            /// <param name="child">The child.</param>
            /// <returns>IChangeList.</returns>
            public void AppendAddChild(ref IChangeList chgs, IPersistedData child)
            {
            }

            /// <summary>
            /// Appends the remove child operation to the changelist.
            /// </summary>
            /// <param name="chgs">The changelist object, or null if none exists yet.</param>
            /// <param name="child">The child being removed.</param>
            public void AppendRemoveChild(ref IChangeList chgs, IPersistedData child)
            {
            }

            /// <summary>
            /// Appends the remove.
            /// </summary>
            /// <param name="chgs">The CHGS.</param>
            /// <param name="parent">The parent.</param>
            /// <param name="isRecursive">if true the deletion is recursive</param>
            /// <returns>IChangeList.</returns>
            public void AppendRemove(ref IChangeList chgs, IPersistedData parent, bool isRecursive = false)
            {
            }

            /// <summary>
            /// records the parent node
            /// </summary>
            /// <param name="parent">the parent, not null</param>
            public void AppendSetParent(IPersistedData parent)
            {
                this.Parent = parent;
            }

            /// <summary>
            /// Appends the set acl.
            /// </summary>
            /// <param name="chgs">The CHGS.</param>
            /// <returns>IChangeList.</returns>
            public void AppendSetAcl(ref IChangeList chgs)
            {
            }

            /// <summary>
            /// Appends the set data.
            /// </summary>
            /// <param name="chgs">The CHGS.</param>
            public void AppendSetData(ref IChangeList chgs)
            {
            }

            /// <summary>
            /// Appends a poison pill for this node to the changelist.
            /// </summary>
            /// <param name="spec">Poison pill specification</param>
            /// <param name="chgs">The changelist.</param>
            public void AppendPoison(string spec, ref IChangeList chgs)
            {
            }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get { return  "ephemeral"; } }

        private long _totalData;
        private long _totalNodes;

        /// <summary>
        /// Gets the total data.
        /// </summary>
        /// <value>The total data.</value>
        public ulong TotalData { get { return (ulong)_totalData; } }

        /// <summary>
        /// Gets the total nodes.
        /// </summary>
        /// <value>The total nodes.</value>
        public ulong TotalNodes { get { return (ulong)_totalNodes; } }

        /// <summary>
        /// Gets a value indicating whether this instance is active.
        /// </summary>
        /// <value><c>true</c> if this instance is active; otherwise, <c>false</c>.</value>
        public bool IsActive { get { return true; } }

        /// <summary>
        /// Dumps all nodes for debugging purposes only.
        /// </summary>
        /// <returns>string contained one line per PersistedData object managed by the factory</returns>
        public string DumpAllNodesForDebug()
        {
            return string.Empty;
        }

        /// <summary>
        /// Gets the health.
        /// </summary>
        /// <returns>Dictionary&lt;System.String, System.String&gt;.</returns>
        public Dictionary<string, HealthDefinition> GetHealth()
        {
            Dictionary<string, HealthDefinition> d = new Dictionary<string, HealthDefinition>();
            d.Add(this.Name, new HealthDefinition(true, 1, "ok"));
            return d;
        }

        /// <summary>
        /// Returns true if it is unsafe for the cluster (i.e. primary doesn't strictly depend on it) to stop an instance
        /// </summary>
        /// <param name="name">name of the instance intended to be stopped</param>
        /// <param name="failoverWillHappen">if true, a failover is predicted</param>
        /// <returns>true is it is unsafe</returns>
        public bool UnsafeToStopInstance(string name, out bool failoverWillHappen)
        {
            failoverWillHappen = true;
            return false;
        }

        /// <summary>
        /// Indicates the factory our wish to failover if posible into a different replica
        /// </summary>
        /// <param name="replicaToFailover">The replica to failover.</param>
        /// <param name="onInitiated">invoked if not null if when failover is initiated</param>
        public void FailoverInto(string replicaToFailover, Action onInitiated)
        {
        }

        /// <summary>
        /// returns the members. null for this provider
        /// </summary>
        /// <returns>null</returns>
        public ClusterMember[] GetAgreedMembers()
        {
            return null;
        }

        /// <summary>
        /// changes the agreed memberset based on the given mapping
        /// </summary>
        /// <param name="changeMapping">the mapping function for the modified members</param>
        public bool ChangeAgreedMembers(Dictionary<string, ClusterMember> changeMapping)
        {
            return false;
        }

        /// <summary>
        /// Activates the specified argBackend.
        /// </summary>
        /// <param name="argBackend">The argBackend.</param>
        /// <param name="client">The client.</param>
        public void Activate(RingMasterBackendCore argBackend, IPersistedDataFactoryClient client)
        {
            if (client!=null)
            {
                while (!client.CanBecomePrimary())
                {
                    Thread.Sleep(100);
                }
                client.OnBecomePrimary();
            }
        }

        /// <summary>
        /// Deactivates this instance.
        /// </summary>
        public void Deactivate()
        {
        }

        /// <summary>
        /// Loads the tree.
        /// </summary>
        /// <param name="lastXId">The last x identifier.</param>
        /// <returns>Node.</returns>
        public Node LoadTree(out long lastXId)
        {
            lastXId = 0;
            return null;
        }

        public long GetLastXId()
        {
            return 0;
        }

        /// <summary>
        /// Creates the new.
        /// </summary>
        /// <returns>IPersistedData.</returns>
        public IPersistedData CreateNew()
        {
            IPersistedData res= new PersistedData();
            RecordStatsDelta(1, 0);
            return res;
        }

        /// <summary>
        /// Deletes the specified IPersistedData.
        /// </summary>
        /// <param name="node">The node.</param>
        public void Delete(IPersistedData node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            node.Delete();
            RecordStatsDelta(-1, -node.Stat.DataLength);
        }

        /// <summary>
        /// Gets a value indicating whether the factory requires calls for each delete object, or the caller can just not call Delete for each,
        /// and instead invoke RecordDataDelta
        /// </summary>
        /// <value><c>true</c> if the factory requires calls for each delete; otherwise, <c>false</c>.</value>
        public bool RequiresCallsForEachDelete
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Records the data delta.
        /// </summary>
        /// <param name="size">The size.</param>
        public void RecordDataDelta(int size)
        {
            RecordStatsDelta(0, size);
        }

        /// <summary>
        /// Records the data delta size.
        /// </summary>
        /// <param name="numNodes">number of nodes changed</param>
        /// <param name="dataSize">The data size changed.</param>
        public void RecordStatsDelta(int numNodes, int dataSize)
        {
            Interlocked.Add(ref this._totalNodes, numNodes);
            Interlocked.Add(ref this._totalData, dataSize);
        }

        /// <summary>
        /// Records undeletion of a node.
        /// </summary>
        /// <param name="node">Node that is undeleted.</param>
        public void RecordNodeUndelete(IPersistedData node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            RecordStatsDelta(1, node.Stat.DataLength);
        }

        /// <summary>
        /// Wipes all data and restart the process.
        /// </summary>
        public void WipeAllDataAndShutdown()
        {
        }

        /// <summary>
        /// Imports a checkpoint from the given file location.
        /// </summary>
        /// <param name="location">The file location for the checkpoint file.</param>
        /// <returns><c>true</c> if the import succeeded and the process will die soon, <c>false</c> otherwise.</returns>
        public bool ImportCheckpoint(string location)
        {
            return false;
        }

        /// <summary>
        /// Schedules an immediate checkpoint (memory snapshot).
        /// Note: For the duration of the checkpoint, no write will be allowed.
        /// </summary>
        /// <returns><c>true</c> if the checkpoint is scheduled, <c>false</c> otherwise.</returns>
        public bool TakeCheckpoint()
        {
            return false;
        }

        /// <summary>
        /// Schedules an immediate checkpoint (memory snapshot).
        /// Note: For the duration of the checkpoint, no write will be allowed.
        /// </summary>
        /// <param name="topNode">The top node.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="version">The version, or -1 for any.</param>
        /// <returns><c>true</c> if the checkpoint is scheduled, <c>false</c> otherwise.</returns>
        public bool TakeCheckpoint(IPersistedData topNode, string filename, int version)
        {
            return false;
        }

    }
}