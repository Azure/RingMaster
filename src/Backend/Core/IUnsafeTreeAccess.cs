// <copyright file="IUnsafeTreeAccess.cs" company="Microsoft">
//     Copyright Microsoft Corporation
// </copyright>
// <summary></summary>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System.Collections.Generic;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence;

    /// <summary>
    /// Interface that is used to directly interact with the node tree.
    /// </summary>
    public interface IUnsafeTreeAccess
    {
        void LockRoot();

        void LockRootNoSync();

        void ReleaseRoot();

        void DoNodeForAddChildren(IPersistedData parent, List<IPersistedData> children);

        void UnsafeCreate(IPersistedData node, long txtime, string path, long xid, ILockListTransaction locklist);

        void UnsafeAddChild(IPersistedData parent, IPersistedData child, long txtime, string path, long xid, ILockListTransaction locklist);

        void UnsafeAddEphemeralChild(IPersistedData parent, string path);

        void UnsafeRemove(IPersistedData parent, IPersistedData node, long txtime, string path, long xid, ILockListTransaction locklist);

        void UnsafeRemoveChild(IPersistedData parent, IPersistedData node, long txtime, string path, long xid, ILockListTransaction locklist);

        void UnsafeDeleteNode(IPersistedData parent, IPersistedData node, long txtime, string path, long xid, ILockListTransaction locklist);

        void UnsafeSetAcl(IPersistedData node, IReadOnlyList<Acl> list, long txtime, string path, long xid, ILockListTransaction locklist);

        void UnsafeSetPoisonPill(IPersistedData node, string spec, long txtime, long xid);

        void UnsafeSetData(IPersistedData node, byte[] data, long txtime, string path, long xid, ILockListTransaction locklist);

        void UnsafeClearTree();
    }
}