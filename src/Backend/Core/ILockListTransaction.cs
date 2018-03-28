// <copyright file="ILockListTransaction.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    using IOperationOverrides = Microsoft.Azure.Networking.Infrastructure.RingMaster.Data.IOperationOverrides;
    using ISessionAuth = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.ISessionAuth;
    using Perm = Microsoft.Azure.Networking.Infrastructure.RingMaster.Data.Acl.Perm;

    /// <summary>
    /// Lock list interface to abstract the locking of in-memory tree and committing to the persistence
    /// </summary>
    public interface ILockListTransaction : IDisposable
    {
        /// <summary>
        /// Gets the time for this transaction
        /// </summary>
        long TxTime { get; }

        /// <summary>
        /// Gets the transaction id
        /// </summary>
        long TxId { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this will finish synchronously
        /// </summary>
        bool FinishSynchronous { get; set; }

        /// <summary>
        /// initializes the object
        /// </summary>
        /// <param name="sessionAuth">Session authentication data</param>
        /// <param name="operationOverrides">Operation overrides</param>
        void Initialize(ISessionAuth sessionAuth, IOperationOverrides operationOverrides);

        /// <summary>
        /// Adds a RW lock to this tx. Validates Acls
        /// </summary>
        /// <param name="node">Node to be locked</param>
        /// <param name="permission">Permission required</param>
        /// <param name="level">Level counted from the root node</param>
        /// <param name="isChildEphemeral">Whether the child node is ephemeral</param>
        /// <returns>Whether the operation is successful</returns>
        bool AddLockRw(Node node, Perm permission, int level, bool isChildEphemeral = false);

        /// <summary>
        /// Adds a RO lock to this transaction. Validates Acls
        /// </summary>
        /// <param name="node">Node to be locked</param>
        /// <param name="level">Level counted from the root node</param>
        /// <returns>Whether the operation is successful</returns>
        bool AddLockRo(Node node, int level);

        /// <summary>
        /// Acquires all locks
        /// </summary>
        /// <param name="cancelled">Flag to cancel the long-running lock acquisition</param>
        void LockAll(ref bool cancelled);

        /// <summary>
        /// snaps the stat of the given persisted data if appropriate
        /// </summary>
        /// <param name="persistedData">Persisted data object</param>
        /// <returns>Stat associated with the persisted data</returns>
        IMutableStat SnapStatIfNeeded(IPersistedData persistedData);

        /// <summary>
        /// validates that the proper locks are acquired for the nodes and hierarchy
        /// </summary>
        /// <param name="parent">Parent persisted data object</param>
        /// <param name="permParent">Permission of the parent</param>
        /// <param name="child">Child persisted data object</param>
        /// <param name="permChild">Permission of the child</param>
        void ValidateLockList(IPersistedData parent, Perm permParent, IPersistedData child, Perm permChild);

        /// <summary>
        /// Appends a create operation
        /// </summary>
        /// <param name="persistedDataFactory">Persisted data factory</param>
        /// <param name="persistedData">Persisted data created</param>
        /// <param name="txTime">Transaction time</param>
        void AppendCreate(IPersistedDataFactory<Node> persistedDataFactory, IPersistedData persistedData, long txTime);

        /// <summary>
        /// Appends an add child operation
        /// </summary>
        /// <param name="persisted">Persisted data object</param>
        /// <param name="childData">Child of the persisted data</param>
        /// <param name="txTime">Transaction time</param>
        /// <param name="prevStat">Stat to be used on abortion of append</param>
        void AppendAddChild(IPersistedData persisted, IPersistedData childData, long txTime, IMutableStat prevStat);

        /// <summary>
        /// Appends a remove operation
        /// </summary>
        /// <param name="parent">Parent persisted data object</param>
        /// <param name="child">Child persisted data object</param>
        /// <param name="txTime">Transaction time</param>
        /// <param name="prevChildStat">Stat of child to be used on abortion of append</param>
        /// <param name="prevParentStat">Stat of parent to be used on abortion of append</param>
        /// <param name="recordUndeleteAction">Action to invoke on abortion</param>
        void AppendRemove(IPersistedData parent, IPersistedData child, long txTime, IMutableStat prevChildStat, IMutableStat prevParentStat, Action recordUndeleteAction);

        /// <summary>
        /// Appends a remove operation for the node and all children
        /// </summary>
        /// <param name="child">Child persisted data object</param>
        /// <param name="txTime">Transaction time</param>
        /// <param name="recordUndeleteAction">Action to invoke on abortion</param>
        void AppendRemoveNodeAndAllChildren(IPersistedData child, long txTime, Action recordUndeleteAction);

        /// <summary>
        /// Appends a setacl operation
        /// </summary>
        /// <param name="persisted">Persisted data object</param>
        /// <param name="txTime">Transaction time</param>
        /// <param name="prevAcl">ACL to be applied on abortion of append</param>
        /// <param name="prevStat">Stat to be used on abortion of append</param>
        void AppendSetAcl(IPersistedData persisted, long txTime, IReadOnlyList<Acl> prevAcl, IMutableStat prevStat);

        /// <summary>
        /// Appends a SetData operation
        /// </summary>
        /// <param name="persisted">Persisted data object</param>
        /// <param name="txTime">Transaction time</param>
        /// <param name="prevData">Data to be applied on abortion of append</param>
        /// <param name="prevStat">Stat to be used on abortion of append</param>
        void AppendSetData(IPersistedData persisted, long txTime, byte[] prevData, IMutableStat prevStat);

        /// <summary>
        /// Appends a poison pill for the given path.
        /// </summary>
        /// <param name="data">The PD to poison.</param>
        /// <param name="spec">The poison pill specification</param>
        /// <param name="txTime">The tx time.</param>
        void AppendPoison(IPersistedData data, string spec, long txTime);

        /// <summary>
        /// Appends a move operation.
        /// </summary>
        /// <param name="parentSrc">The parent node where the moved node lives.</param>
        /// <param name="parentDst">The parent node where the node will move into.</param>
        /// <param name="child">The child node moved.</param>
        /// <param name="txTime">The tx time.</param>
        /// <param name="prevChildStat">The previous child stat.</param>
        /// <param name="prevStatParentSrc">The previous stat parent source.</param>
        /// <param name="prevStatParentDst">The previous stat parent DST.</param>
        void AppendMove(IPersistedData parentSrc, IPersistedData parentDst, IPersistedData child, long txTime, IMutableStat prevChildStat, IMutableStat prevStatParentSrc, IMutableStat prevStatParentDst);

        /// <summary>
        /// Runs an action upon commit
        /// </summary>
        /// <param name="p">Action to run on commit</param>
        void RunOnCommit(Action p);

        /// <summary>
        /// Runs an action upon abort
        /// </summary>
        /// <param name="p">Action to invoke on abort</param>
        void RunOnAbort(Action p);

        /// <summary>
        /// marks this locklist to be aborted.
        /// </summary>
        void MarkForAbort();

        /// <summary>
        /// indicates if this locklist is marked aborted
        /// </summary>
        /// <returns>True if this lock list is marked aborted, false if otherwise</returns>
        bool IsMarkedForAbort();

        /// <summary>
        /// closes the locklist
        /// </summary>
        /// <param name="task">async task to indicate the completion of the replication on output</param>
        /// <returns>true if it needs to be disposed</returns>
        bool Complete(out Task task);
    }
}
