// <copyright file="IPersistedDataFactory.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Interface IPersistedDataFactory.
    /// A factory of IPersistedData.
    /// </summary>
    /// <typeparam name="T">Type name of node class</typeparam>
    public interface IPersistedDataFactory<T>
    {
        /// <summary>
        /// Gets a value indicating whether the factory requires calls for each delete object, or the caller can just not call Delete for each,
        /// and instead invoke RecordDataDelta
        /// </summary>
        /// <value><c>true</c> if the factory requires calls for each delete; otherwise, <c>false</c>.</value>
        bool RequiresCallsForEachDelete { get; }

        /// <summary>
        /// Gets the name of this factory
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Gets the total data hosted by this factory.
        /// </summary>
        /// <value>The total data.</value>
        ulong TotalData { get; }

        /// <summary>
        /// Gets the total nodes hosted by this factory
        /// </summary>
        /// <value>The total nodes.</value>
        ulong TotalNodes { get; }

        /// <summary>
        /// Gets a value indicating whether this factory is active.
        /// </summary>
        /// <value><c>true</c> if this instance is active; otherwise, <c>false</c>.</value>
        bool IsActive { get; }

        /// <summary>
        /// Loads the initial tree of nodes.
        /// </summary>
        /// <param name="lastXId">The highest transaction Id loaded.</param>
        /// <returns>T.</returns>
        T LoadTree(out long lastXId);

        /// <summary>
        /// retrieves the current latest txId.
        /// </summary>
        /// <returns>Last transaction ID</returns>
        long GetLastXId();

        /// <summary>
        /// Creates a new IPersistedData.
        /// </summary>
        /// <returns>IPersistedData.</returns>
        IPersistedData CreateNew();

        /// <summary>
        /// Deletes the specified IPersistedData.
        /// </summary>
        /// <param name="node">The node.</param>
        void Delete(IPersistedData node);

        /// <summary>
        /// Records the data delta size.
        /// </summary>
        /// <param name="size">The size.</param>
        void RecordDataDelta(int size);

        /// <summary>
        /// Records the data delta size.
        /// </summary>
        /// <param name="numNodes">number of nodes changed</param>
        /// <param name="dataSize">The data size changed.</param>
        void RecordStatsDelta(int numNodes, int dataSize);

        /// <summary>
        /// Activates the specified instance using the given argBackend and client.
        /// </summary>
        /// <param name="argBackend">The argBackend.</param>
        /// <param name="client">The client.</param>
        void Activate(RingMasterBackendCore argBackend, IPersistedDataFactoryClient client);

        /// <summary>
        /// Deactivates this instance.
        /// </summary>
        void Deactivate();

        /// <summary>
        /// Dumps all nodes for debugging purposes only.
        /// </summary>
        /// <returns>string contained one line per PersistedData object managed by the factory</returns>
        string DumpAllNodesForDebug();

        /// <summary>
        /// Gets the health definition of this factory.
        /// </summary>
        /// <returns>Dictionary&lt;System.String, System.String&gt;.</returns>
        Dictionary<string, HealthDefinition> GetHealth();

        /// <summary>
        /// returns the list of members as agreed by the cluster itself, or null if no need for tracking this.
        /// </summary>
        /// <returns>the list of agreed members if tracking is needed</returns>
        ClusterMember[] GetAgreedMembers();

        /// <summary>
        /// changes the agreed memberset based on the given mapping
        /// </summary>
        /// <param name="changeMapping">the mapping function for the modified members</param>
        /// <returns>If the operation is succeeded</returns>
        bool ChangeAgreedMembers(Dictionary<string, ClusterMember> changeMapping);

        /// <summary>
        /// Returns true if it is unsafe for the cluster (i.e. primary doesn't strictly depend on it) to stop an instance
        /// </summary>
        /// <param name="name">name of the instance intended to be stopped</param>
        /// <param name="failoverWillHappen">if true, a failover is predicted</param>
        /// <returns>true is it is unsafe</returns>
        bool UnsafeToStopInstance(string name, out bool failoverWillHappen);

        /// <summary>
        /// Indicates the factory our wish to failover if posible into a different replica
        /// </summary>
        /// <param name="replicaToFailover">The replica to failover.</param>
        /// <param name="onInitiate">if not null, it will be called when the failover is initiated.</param>
        void FailoverInto(string replicaToFailover, Action onInitiate);

        /// <summary>
        /// Records undeletion of a node.
        /// </summary>
        /// <param name="node">Node that is undeleted.</param>
        void RecordNodeUndelete(IPersistedData node);

        /// <summary>
        /// Wipes all data and restart the process.
        /// </summary>
        void WipeAllDataAndShutdown();

        /// <summary>
        /// Imports a checkpoint from the given file location.
        /// </summary>
        /// <param name="location">The file location for the checkpoint file.</param>
        /// <returns><c>true</c> if the import succeeded and the process will die soon, <c>false</c> otherwise.</returns>
        bool ImportCheckpoint(string location);

        /// <summary>
        /// Schedules an immediate checkpoint (memory snapshot).
        /// Note: For the duration of the checkpoint, no write will be allowed.
        /// </summary>
        /// <returns><c>true</c> if the checkpoint is scheduled, <c>false</c> otherwise.</returns>
        bool TakeCheckpoint();

        /// <summary>
        /// Schedules an immediate checkpoint (memory snapshot).
        /// Note: For the duration of the checkpoint, no write will be allowed.
        /// </summary>
        /// <param name="topNode">The top node.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="version">The version, or -1 for any.</param>
        /// <returns><c>true</c> if the checkpoint is scheduled, <c>false</c> otherwise.</returns>
        bool TakeCheckpoint(IPersistedData topNode, string filename, int version);
    }
}
