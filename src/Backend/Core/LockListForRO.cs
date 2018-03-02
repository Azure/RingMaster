// ***********************************************************************
// Assembly         : RingMaster
// <copyright file="LockListForRO.cs" company="Microsoft">
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

    /// <summary>
    /// Class LockList. Implements a LockList that is used only for RO lockfree operations
    /// </summary>
    public class LockListForRO : ILockListTransaction
    {
        private ISessionAuth auth;

        public LockListForRO()
        {
        }

        /// <summary>
        /// initializes the object
        /// </summary>
        /// <param name="auth"></param>
        /// <param name="over"></param>
        public void Initialize(ISessionAuth auth, IOperationOverrides over)
        {
            this.auth = auth;
        }

        public long TxTime
        {
            get
            {
                return 0;
            }
        }

        public long TxId
        {
            get
            {
                return 0;
            }
        }

        public bool FinishSynchronous
        {
            get
            {
                return true;
            }

            set
            {
            }
        }

        private bool isMarked = false;

        /// <summary>
        /// marks this locklist to be aborted.
        /// </summary>
        public void MarkForAbort()
        {
            isMarked = true;
        }

        /// <summary>
        /// indicates if this locklist is marked aborted
        /// </summary>
        /// <returns></returns>
        public bool IsMarkedForAbort()
        {
            return isMarked;
        }

        public bool Complete(out Task task)
        {
            task = Task.FromResult(0);
            return true;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public bool AddAndLockRw(Node parent, Perm cREATE, int parentLevel, bool isChildEphemeral)
        {
            throw new NotSupportedException();
        }

        public bool AddAndLockRo(Node n, int level)
        {
            if (n == null)
            {
                throw new ArgumentNullException("n");
            }

            if (!this.auth.IsSuperSession)
            {
                n.AclAllows(this.auth, Perm.READ);
            }
            return false;
        }

        public IMutableStat SnapStatIfNeeded(IPersistedData persisted)
        {
            throw new NotSupportedException();
        }

        public void ValidateLockList(IPersistedData parent, Perm permParent, IPersistedData child, Perm permChild)
        {
        }

        public void AppendCreate(IPersistedDataFactory<Node> persFact, IPersistedData childData, long txTime)
        {
            throw new NotSupportedException();
        }

        public void AppendAddChild(IPersistedData persisted, IPersistedData childData, long txTime, IMutableStat prevStat)
        {
            throw new NotSupportedException();
        }

        public void AppendRemove(IPersistedData parent, IPersistedData child, long txTime, IMutableStat prevChildStat, IMutableStat prevParentStat, Action recordUndeleteAction)
        {
            throw new NotSupportedException();
        }

        public void AppendRemoveNodeAndAllChildren(IPersistedData child, long txTime, Action recordUndeleteAction)
        {
            throw new NotSupportedException();
        }

        public void AppendMove(IPersistedData parentSrc, IPersistedData parentDst, IPersistedData child, long txTime, IMutableStat prevChildStat, IMutableStat prevStatParentSrc, IMutableStat prevStatParentDst)
        {
            throw new NotSupportedException();
        }

        public void AppendSetAcl(IPersistedData persisted, long txTime, IReadOnlyList<Acl> prevAcl, IMutableStat prevStat)
        {
            throw new NotSupportedException();
        }

        public void AppendSetData(IPersistedData persisted, long txTime, byte[] prevData, IMutableStat prevStat)
        {
            throw new NotSupportedException();
        }

        public void AppendPoison(IPersistedData data, string spec, long txTime)
        {
            throw new NotSupportedException();
        }

        public void RunOnCommit(Action p)
        {
            throw new NotSupportedException();
        }
        public void RunOnAbort(Action p)
        {
            throw new NotSupportedException();
        }
    }
}