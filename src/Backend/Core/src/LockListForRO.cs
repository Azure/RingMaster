// <copyright file="LockListForRO.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
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
    /// Class LockList. Implements a LockList that is used only for RO lockfree operations
    /// </summary>
    public class LockListForRO : ILockListTransaction
    {
        private ISessionAuth auth;
        private bool isMarked = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="LockListForRO"/> class.
        /// </summary>
        public LockListForRO()
        {
        }

        /// <inheritdoc />
        public long TxTime
        {
            get
            {
                return 0;
            }
        }

        /// <inheritdoc />
        public long TxId
        {
            get
            {
                return 0;
            }
        }

        /// <inheritdoc />
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

        /// <summary>
        /// initializes the object
        /// </summary>
        /// <param name="auth">Session auth</param>
        /// <param name="over">Not in use</param>
        public void Initialize(ISessionAuth auth, IOperationOverrides over)
        {
            this.auth = auth;
        }

        /// <summary>
        /// marks this locklist to be aborted.
        /// </summary>
        public void MarkForAbort()
        {
            this.isMarked = true;
        }

        /// <inheritdoc />
        public bool IsMarkedForAbort()
        {
            return this.isMarked;
        }

        /// <inheritdoc />
        public bool Complete(out Task task)
        {
            task = Task.FromResult(0);
            return true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public bool AddLockRw(Node parent, Perm cREATE, int parentLevel, bool isChildEphemeral)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public bool AddLockRo(Node n, int level)
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

        /// <inheritdoc />
        public void LockAll(ref bool cancelled)
        {
        }

        /// <inheritdoc />
        public IMutableStat SnapStatIfNeeded(IPersistedData persisted)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public void ValidateLockList(IPersistedData parent, Perm permParent, IPersistedData child, Perm permChild)
        {
        }

        /// <inheritdoc />
        public void AppendCreate(IPersistedDataFactory<Node> persFact, IPersistedData childData, long txTime)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public void AppendAddChild(IPersistedData persisted, IPersistedData childData, long txTime, IMutableStat prevStat)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public void AppendRemove(IPersistedData parent, IPersistedData child, long txTime, IMutableStat prevChildStat, IMutableStat prevParentStat, Action recordUndeleteAction)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public void AppendRemoveNodeAndAllChildren(IPersistedData child, long txTime, Action recordUndeleteAction)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public void AppendMove(IPersistedData parentSrc, IPersistedData parentDst, IPersistedData child, long txTime, IMutableStat prevChildStat, IMutableStat prevStatParentSrc, IMutableStat prevStatParentDst)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public void AppendSetAcl(IPersistedData persisted, long txTime, IReadOnlyList<Acl> prevAcl, IMutableStat prevStat)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public void AppendSetData(IPersistedData persisted, long txTime, byte[] prevData, IMutableStat prevStat)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public void AppendPoison(IPersistedData data, string spec, long txTime)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public void RunOnCommit(Action p)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public void RunOnAbort(Action p)
        {
            throw new NotSupportedException();
        }
    }
}