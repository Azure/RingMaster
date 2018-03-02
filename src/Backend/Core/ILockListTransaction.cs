// ***********************************************************************
// Assembly         : RingMaster
// <copyright file="ILockListTransaction.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    using IOperationOverrides = Microsoft.Azure.Networking.Infrastructure.RingMaster.Data.IOperationOverrides;
    using ISessionAuth = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.ISessionAuth;
    using Perm = Microsoft.Azure.Networking.Infrastructure.RingMaster.Data.Acl.Perm;

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
        /// Gets the property indicating if this will finish synchronously
        /// </summary>
        bool FinishSynchronous { get; set; }

        /// <summary>
        /// initializes the object
        /// </summary>
        /// <param name="auth"></param>
        /// <param name="over"></param>
        void Initialize(ISessionAuth auth, IOperationOverrides over);

        /// <summary>
        /// Adds a RW lock to this tx. Validates Acls
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="cREATE"></param>
        /// <param name="level"></param>
        /// <param name="isChildEphemeral"></param>
        bool AddAndLockRw(Node parent, Perm cREATE, int level, bool isChildEphemeral=false);

        /// <summary>
        /// Adds a RO lock to this tx. Validates Acls
        /// </summary>
        /// <param name="n"></param>
        /// <param name="level"></param>
        bool AddAndLockRo(Node n, int level);

        /// <summary>
        /// snaps the stat of the given persistent data if appropriate
        /// </summary>
        /// <param name="persisted"></param>
        /// <returns></returns>
        IMutableStat SnapStatIfNeeded(IPersistedData persisted);

        /// <summary>
        /// validates that the proper locks are acquired for the nodes and hierarchy
        /// </summary>
        void ValidateLockList(IPersistedData parent, Perm permParent, IPersistedData child, Perm permChild);

        /// <summary>
        /// Appends a create operation
        /// </summary>
        /// <param name="persFact"></param>
        /// <param name="childData"></param>
        /// <param name="txTime"></param>
        void AppendCreate(IPersistedDataFactory<Node> persFact, IPersistedData childData, long txTime);

        /// <summary>
        /// Appends an addchild operation
        /// </summary>
        /// <param name="persisted"></param>
        /// <param name="childData"></param>
        /// <param name="txTime"></param>
        /// <param name="prevStat"></param>
        void AppendAddChild(IPersistedData persisted, IPersistedData childData, long txTime, IMutableStat prevStat);

        /// <summary>
        /// Appends a remove operation
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="child"></param>
        /// <param name="txTime"></param>
        /// <param name="prevChildStat"></param>
        /// <param name="prevParentStat"></param>
        /// <param name="recordUndeleteAction"></param>
        void AppendRemove(IPersistedData parent, IPersistedData child, long txTime, IMutableStat prevChildStat, IMutableStat prevParentStat, Action recordUndeleteAction);

        /// <summary>
        /// Appends a remove operation for the node and all children
        /// </summary>
        /// <param name="child"></param>
        /// <param name="txTime"></param>
        /// <param name="recordUndeleteAction"></param>
        void AppendRemoveNodeAndAllChildren(IPersistedData child, long txTime, Action recordUndeleteAction);

        /// <summary>
        /// Appends a setacl operation
        /// </summary>
        /// <param name="persisted"></param>
        /// <param name="txTime"></param>
        /// <param name="prevAcl"></param>
        /// <param name="prevStat"></param>
        void AppendSetAcl(IPersistedData persisted, long txTime, IReadOnlyList<Acl> prevAcl, IMutableStat prevStat);

        /// <summary>
        /// Appends a SetData operation
        /// </summary>
        /// <param name="persisted"></param>
        /// <param name="txTime"></param>
        /// <param name="prevData"></param>
        /// <param name="prevStat"></param>
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
        /// <param name="p"></param>
        void RunOnCommit(Action p);

        /// <summary>
        /// Runs an action upon abort
        /// </summary>
        /// <param name="p"></param>
        void RunOnAbort(Action p);

        /// <summary>
        /// marks this locklist to be aborted.
        /// </summary>
        void MarkForAbort();

        /// <summary>
        /// indicates if this locklist is marked aborted
        /// </summary>
        /// <returns></returns>
        bool IsMarkedForAbort();

        /// <summary>
        /// closes the locklist
        /// </summary>
        /// <param name="task">async task to indicate the completion of the replication on output</param>
        /// <returns>true if it needs to be disposed</returns>
        bool Complete(out Task task);
    }

}