// ***********************************************************************
// Assembly         : RingMaster
// <copyright file="LockList.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    using IOperationOverrides = Microsoft.Azure.Networking.Infrastructure.RingMaster.Data.IOperationOverrides;
    using ISessionAuth = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.ISessionAuth;
    using Perm = Microsoft.Azure.Networking.Infrastructure.RingMaster.Data.Acl.Perm;

    /// <summary>
    /// Class LockList. Implements a nestable lock escrow to track what objects are being read or write within a RM operation. Locking whatever is needed.
    /// </summary>
    public class LockListForRW : ILockListTransaction
    {
        /// <summary>
        /// The _locks ro
        /// </summary>
        private List<ILockObject> _locksRo = null;
        
        /// <summary>
        /// The _locks rw
        /// </summary>
        private List<ILockObject> _locksRw = null;

        private IChangeList _chg;

        internal ISessionAuth Auth;

        /// <summary>
        /// if true, this means the locklist is in lockdown for commits or any operation other than unwinding and aborting.
        /// </summary>
        private bool _isLockDown;
        
        /// <summary>
        /// list of actions to be run upon commit
        /// </summary>
        private List<Action> _onCommit;

        /// <summary>
        /// If true, the commit will be synchronous
        /// </summary>
        public bool FinishSynchronous { get; set; }

        /// <summary>
        /// list of actions to be run upon commit
        /// </summary>
        private LinkedList<Action> _onAbort;

        /// <summary>
        /// transaction id.
        /// </summary>
        public long TxId { get; private set; }

        /// <summary>
        /// time of this transaction
        /// </summary>
        public long TxTime { get; private set; }

        /// <summary>
        /// The tx_id provider
        /// </summary>
        internal static UIdProvider XidProvider = null;

        /// <summary>
        /// do we want to do lock validation?
        /// </summary>
        private static bool DoLockingValidation = false;

        /// <summary>
        /// indicates if this locklist requires for RO operations to acquire the lock.
        /// true by default
        /// </summary>
        internal bool ROInterfaceRequiresLocks;

        /// <summary>
        /// If not null, the list of paths that are in lockdown mode
        /// </summary>
        private LockDownSet lockDownPaths;

        /// <summary>
        /// Changes only allowed on ephemeral nodes
        /// </summary>
        private bool onlyOnEphemeral;

        /// <summary>
        /// Initializes a new instance of the <see cref="LockListForRW"/> class.
        /// </summary>
        /// <param name="lockDownPaths">The paths that must be in lockdown (or null)</param>
        public LockListForRW(LockDownSet lockDownPaths, bool onlyOnEphemeral)
        {
            if (lockDownPaths==null)
            {
                throw new ArgumentNullException("lockDownPaths");
            }

            this.ROInterfaceRequiresLocks = true;
            this.onlyOnEphemeral = onlyOnEphemeral;
            this.lockDownPaths = lockDownPaths;
        }

        /// <summary>
        /// Initializes a locklist, enclosing the ability of locking nodes, and creating a "replicated action" underneath
        /// </summary>
        /// <param name="auth">the authorization token</param>
        /// <param name="over">the overrides, optional</param>
        public void Initialize(ISessionAuth auth, IOperationOverrides over)
        {
            // we provision transactions at construction, which means they can commit in a different 
            // order than monotonical increasing (if Tx1 starts becore Tx2 but Tx2 finishes before Tx1 does)
            SetTxIdAndTime(over);

            this._isLockDown = false;

            this._chg = null;

            this._onAbort = null;

            this.ROInterfaceRequiresLocks = true;

            this.Auth = auth;
        }

        /// <summary>
        /// sets the txid and time for this locklist
        /// </summary>
        /// <param name="over">optional, the overrides</param>
        internal void SetTxIdAndTime(IOperationOverrides over)
        {
            if (over != null && over.TxId != ulong.MaxValue)
            {
                this.TxId = (long)over.TxId;
            }
            else
            {
                this.TxId = (long)XidProvider.NextUniqueId();
            }

            if (over != null && over.TxTime != ulong.MaxValue)
            {
                this.TxTime = (long)over.TxTime;
            }
            else
            {
                this.TxTime = MutableStat.ConvertTime(DateTime.UtcNow);
            }
        }

        /// <summary>
        /// Adds the and lock ro.
        /// </summary>
        /// <param name="n">The n.</param>
        /// <param name="level"></param>
        /// <returns>true if the lock was acquired with this call. false if it was already acquired</returns>
        public bool AddAndLockRo(Node n, int level)
        {
            if (n == null)
            {
                throw new ArgumentNullException("n");
            }

            n.Persisted.AppendRead(ref this._chg);

            ISessionAuth auth = this.Auth;

            if (!auth.IsSuperSession)
            {
                n.AclAllows(auth, Perm.READ);
            }

            // check for lockdown path
            ValidatePathNotInLockDown(n, false);

            ILockObject lck = null;

            if (ROInterfaceRequiresLocks)
            {
                if (!auth.IsLockFreeSession)
                {
                    lck = n.AcquireLockRo(level);
                }
            }

            if (lck != null)
            {
                if (this._locksRo == null)
                {
                    this._locksRo = new List<ILockObject>();
                }
                this._locksRo.Add(lck);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds the and lock rw.
        /// </summary>
        /// <param name="n">The n.</param>
        /// <param name="perm"></param>
        /// <param name="level"></param>
        /// <param name="isChildEphemeral"></param>
        /// <returns>true if the lock was acquired with this call. false if it was already acquired</returns>
        public bool AddAndLockRw(Node n, Perm perm, int level, bool isChildEphemeral)
        {
            if (n == null)
            {
                throw new ArgumentNullException("n");
            }

            n.Persisted.AppendRead(ref this._chg);

            ISessionAuth auth = this.Auth;
            if (!auth.IsSuperSession)
            {
                n.AclAllows(auth, perm);
            }

            if (onlyOnEphemeral)
            {
                bool allowed = true;

                // n is ephemeral --> allowed.

                // n is not ephemeral --> maybe.
                if (!n.Persisted.IsEphemeral)
                {
                    if (perm != Perm.CREATE)
                    {
                        // not CREATE --> disallowed
                        allowed = false;
                    }
                    else
                    {
                        // if child is not ephemeral, disallowed
                        if (!isChildEphemeral)
                        {
                            allowed = false;
                        }
                    }
                }

                if (!allowed)
                {
                    throw new InvalidAclException(Node.BuildPath(n.Persisted), "only ephemerals can be modified by this session");
                }
            }

            // check for lockdown path
            ValidatePathNotInLockDown(n, true);

            ILockObject lck = null;

            if (!auth.IsLockFreeSession)
            {
                lck = n.AcquireLockRw(level);
            }

            if (lck != null)
            {
                if (this._locksRw == null)
                {
                    this._locksRw = new List<ILockObject>();
                }

                this._locksRw.Add(lck);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Validates the path for the node is not in lock down.
        /// </summary>
        /// <param name="n">The node.</param>
        /// <param name="rwMode">true if this is for rw mode</param>
        /// <exception cref="InvalidAclException">if the node is in lockdown</exception>
        private void ValidatePathNotInLockDown(Node n, bool rwMode)
        {
            if (this.lockDownPaths.IsEmpty())
            {
                return;
            }

            string nodepath = Node.BuildPath(n.Persisted);

            if (this.lockDownPaths.Contains(nodepath))
            {
                RingMasterServerInstrumentation.Instance.OnLockDownAccess(nodepath, rwMode);
                throw new InvalidAclException(nodepath, "lockdown");
            }
        }

        /// <summary>
        /// Unlocks all.
        /// </summary>
        public void UnlockAll()
        {
            if (this._locksRw != null)
            {
                foreach (ILockObject n in this._locksRw)
                {
                    n.ReleaseWriterLock();
                }
                this._locksRw = null;
            }

            if (this._locksRo != null)
            {
                foreach (ILockObject n in this._locksRo)
                {
                    n.ReleaseReaderLock();
                }
                this._locksRo = null;
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                CompleteAbortion();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// aborts the changes and locklist if needed
        /// </summary>
        private void CompleteAbortion()
        {
            MarkForAbort();

            if (this._chg != null)
            {
                this._onCommit = null;
                this._chg.Abort();
                this._chg = null;
            }

            UnlockAll();
        }

        /// <summary>
        /// marks this locklist to be aborted.
        /// </summary>
        public void MarkForAbort()
        {
            this._isLockDown = true;
        }

        /// <summary>
        /// indicates if this locklist is marked aborted
        /// </summary>
        /// <returns></returns>
        public bool IsMarkedForAbort()
        {
            return this._isLockDown;
        }

        /// <summary>
        /// closes this locklist, and commits changes and unlocks all if context gets down to 0
        /// </summary>
        /// <param name="task">async task to indicate the completion of the replication on output</param>
        /// <returns>true iif it needs to be disposed</returns>
        public bool Complete(out Task task)
        {
            ManualResetEvent ev = null;
            
            bool abort = this.IsMarkedForAbort();
            task = Task.FromResult(0);

            try
            {

                // if this is an abort and we are not in the topmost context, lock down this object so nothing else can be done other that unwind and abort 
                if (abort)
                {
                    if (this._onAbort != null)
                    {
                        // CAREFUL! this iteration will execute IN REVERSE ORDER. Because elements were inserted each on the first place!!!
                        foreach (Action elem in this._onAbort)
                        {
                            elem();
                        }
                    }

                    if (this._chg != null)
                    {
                        this._chg.Abort();
                    }
                }
                else
                {
                    List<Action> actions = this._onCommit;
                    this._onCommit = null;

                    // execute all oncommit actions now
                    if (actions != null)
                    {
                        foreach (Action act in actions)
                        {
                            act();
                        }
                    }

                    if (this._chg != null && !onlyOnEphemeral)
                    {
                        try
                        {
                            if (this.FinishSynchronous)
                            {
                                ev = ManualResetEventPool.InstancePool.GetOne();
                                this._chg.CommitSync(this.TxId, ev, out task);
                            }
                            else
                            {
                                this._chg.Commit(this.TxId, out task);
                            }
                        }
                        catch (Exception e)
                        {
                            RmAssert.Fail("Commit failed: " + e);
                        }

                        RingMasterServerInstrumentation.Instance.OnTxCommitted();
                    }
                }

            }
            finally
            {
                // make sure we unlock the tree in all cases
                this._chg = null;
                this._onAbort = null;
                this._onCommit = null;

                UnlockAll();
            }

            if (ev != null)
            {
                ManualResetEventPool.InstancePool.WaitOneAndReturn(ref ev);
            }

            return true;
        }

        public void AppendCreate(IPersistedDataFactory<Node> factory, IPersistedData data, long txtime)
        {
            if (factory == null)
            {
                throw new ArgumentNullException("factory");
            }

            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            data.AppendCreate(ref this._chg);

            if (this._chg != null)
            {
                this._chg.SetTime(txtime);
            }

            if (this._isLockDown)
            {
                return;
            }

            this.RunOnAbort(() =>
            {
                factory.Delete(data);
            });
        }

        public void AppendAddChild(IPersistedData parent, IPersistedData child, long txtime, IMutableStat prevStat)
        {
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            if (child == null)
            {
                throw new ArgumentNullException("child");
            }

            if (prevStat == null)
            {
                throw new ArgumentNullException("prevStat");
            }

            parent.AppendAddChild(ref this._chg, child);
            child.AppendSetParent(parent);

            if (this._chg != null)
            {
                this._chg.SetTime(txtime);
            }

            if (this._isLockDown)
            {
                return;
            }

            this.RunOnAbort(() =>
            {
                ValidateLockList(parent, Perm.WRITE, null, Perm.NONE);

                parent.Node.RemoveChild(child.Name);
                parent.Stat = prevStat;
            });
        }

        public void AppendRemoveNodeAndAllChildren(IPersistedData child, long txtime, Action recordUndeleteAction)
        {
            if (child == null)
            {
                throw new ArgumentNullException("child");
            }

            if (recordUndeleteAction != null)
            {
                this.RunOnAbort(recordUndeleteAction);
            }

            child.AppendRemove(ref this._chg, child.Parent, true);

            if (this._chg != null)
            {
                this._chg.SetTime(txtime);
            }
        }

        public void AppendRemove(IPersistedData parent, IPersistedData child, long txtime, IMutableStat prevChildStat, IMutableStat prevParentStat, Action recordUndeleteAction)
        {
            if (child == null)
            {
                throw new ArgumentNullException("child");
            }
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }
            if (prevChildStat == null)
            {
                throw new ArgumentNullException("prevChildStat");
            }
            if (prevParentStat == null)
            {
                throw new ArgumentNullException("prevParentStat");
            }
            if (recordUndeleteAction == null)
            {
                throw new ArgumentNullException("recordUndeleteAction");
            }

            child.AppendRemove(ref this._chg, parent);
            if (this._chg != null)
            {
                this._chg.SetTime(txtime);
            }

            if (this._isLockDown)
            {
                return;
            }

            this.RunOnAbort(() =>
            {
                ValidateLockList(parent, Perm.WRITE, null, Perm.NONE);

                recordUndeleteAction();

                parent.Node.AddChild(child.Node);
                child.Stat = prevChildStat;
                parent.Stat = prevParentStat;
            });
        }

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
        /// <exception cref="System.NotSupportedException"></exception>
        public void AppendMove(IPersistedData parentSrc, IPersistedData parentDst, IPersistedData child, long txTime, IMutableStat prevChildStat, IMutableStat prevStatParentSrc, IMutableStat prevStatParentDst)
        {
            if (child == null)
            {
                throw new ArgumentNullException("child");
            }
            if (parentSrc == null)
            {
                throw new ArgumentNullException("parentSrc");
            }
            if (parentDst == null)
            {
                throw new ArgumentNullException("parentDst");
            }
            if (prevChildStat == null)
            {
                throw new ArgumentNullException("prevChildStat");
            }
            if (prevChildStat == null)
            {
                throw new ArgumentNullException("prevChildStat");
            }
            if (prevStatParentSrc == null)
            {
                throw new ArgumentNullException("prevStatParentSrc");
            }
            if (prevChildStat == null)
            {
                throw new ArgumentNullException("prevChildStat");
            }
            if (prevStatParentDst == null)
            {
                throw new ArgumentNullException("prevStatParentDst");
            }

            parentSrc.AppendRemoveChild(ref this._chg, child);
            parentDst.AppendAddChild(ref this._chg, child);

            if (this._chg != null)
            {
                this._chg.SetTime(txTime);
            }

            if (this._isLockDown)
            {
                return;
            }

            this.RunOnAbort(() =>
            {
                ValidateLockList(parentDst, Perm.WRITE, null, Perm.NONE);
                ValidateLockList(parentSrc, Perm.WRITE, null, Perm.NONE);

                parentDst.Node.RemoveChild(child.Name);
                parentSrc.Node.AddChild(child.Node);

                child.Stat = prevChildStat;
                parentDst.Stat = prevStatParentDst;
                parentSrc.Stat = prevStatParentSrc;
            });
        }

        /// <summary>
        /// performs the validation for the locks acquired at this time.
        /// all nodes ancestors to child need to be in the RO lock list
        /// parent node needs to be in the RO if permParent is READ, or in the RW if permParent is > READ
        /// child node needs to be in the RO if permChild is READ, or in the RW if permChild is > READ
        /// </summary>
        /// <param name="parent">the parent node</param>
        /// <param name="permParent">the permissions supposed for the parent</param>
        /// <param name="child">the child node</param>
        /// <param name="permChild">the permissions supposed for the child</param>
        public void ValidateLockList(IPersistedData parent, Perm permParent, IPersistedData child, Perm permChild)
        {
            if (!DoLockingValidation)
            {
                return;
            }

            if (parent != null)
            {
                int parentlevel = parent.Node.GetLevel();
                if (permParent == Perm.READ)
                {
                    if (AddAndLockRo(parent.Node, parentlevel))
                    {
                        throw new InvalidOperationException();
                    }
                }
                else if (permParent != Perm.NONE)
                {
                    bool isEphemeral = false;

                    if (child == null || child.IsEphemeral)
                    {
                        isEphemeral = true;
                    }

                    if (AddAndLockRw(parent.Node, permParent, parentlevel, isEphemeral))
                    {
                        throw new InvalidOperationException();
                    }
                }
            }

            if (child != null)
            {
                int level;
                if (parent == null)
                {
                    level = child.Node.GetLevel();
                }
                else
                {
                    level = parent.Node.GetLevel() + 1;
                }
                if (permChild == Perm.READ)
                {
                    if (AddAndLockRo(child.Node, level))
                    {
                        throw new InvalidOperationException();
                    }
                }
                else if (permChild != Perm.NONE)
                {
                    if (AddAndLockRw(child.Node, permChild, level, child.IsEphemeral))
                    {
                        throw new InvalidOperationException();
                    }
                }
                Node n = child.Node;
                while (n != null)
                {
                    n = n.Parent;
                    if (n == null)
                    {
                        break;
                    }

                    if (AddAndLockRo(n, n.GetLevel()))
                    {
                        throw new InvalidOperationException();
                    }
                }
            }
        }

        public IMutableStat SnapStatIfNeeded(IPersistedData data)
        {
            if (this._isLockDown)
            {
                return null;
            }

            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (data.Stat is FirstStat)
            {
                return new FirstStat(data.Stat);
            }

            return new MutableStat(data.Stat);
        }

        public void AppendSetAcl(IPersistedData data, long txtime, IReadOnlyList<Acl> prevAcl, IMutableStat prevStat)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            if (prevStat == null)
            {
                throw new ArgumentNullException("prevStat");
            }

            data.AppendSetAcl(ref this._chg);

            if (this._chg != null)
            {
                this._chg.SetTime(txtime);
            }

            if (this._isLockDown)
            {
                return;
            }

            this.RunOnAbort(() =>
            {
                data.Acl = prevAcl;
                data.Stat = prevStat;
            });
        }

        public void AppendSetData(IPersistedData data, long txtime, byte[] prevData, IMutableStat prevStat)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            if (prevStat == null)
            {
                throw new ArgumentNullException("prevStat");
            }

            data.AppendSetData(ref this._chg);
            if (this._chg != null)
            {
                this._chg.SetTime(txtime);
            }

            if (this._isLockDown)
            {
                return;
            }

            this.RunOnAbort(() =>
            {
                data.Data = prevData;
                data.Stat = prevStat;
            });
        }

        /// <summary>
        /// Appends a poison pill for the given path.
        /// </summary>
        /// <param name="data">The PD to poison.</param>
        /// <param name="spec">the Poison pill specification</param>
        /// <param name="txTime">The tx time.</param>
        public void AppendPoison(IPersistedData data, string spec, long txTime)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            data.AppendPoison(spec, ref this._chg);
            if (this._chg != null)
            {
                this._chg.SetTime(txTime);
            }
        }

        /// <summary>
        /// adds an action to execute upon commit
        /// </summary>
        /// <param name="act"></param>
        public void RunOnCommit(Action act)
        {
            if (this._onCommit == null)
            {
                this._onCommit = new List<Action>();
            }
            this._onCommit.Add(act);
        }

        /// <summary>
        /// adds an action to execute upon abort
        /// </summary>
        /// <param name="act"></param>
        public void RunOnAbort(Action act)
        {
            if (this._isLockDown)
            {
                throw new InvalidOperationException("this locklist is not abortable");
            }

            if (this._onAbort == null)
            {
                this._onAbort = new LinkedList<Action>();
            }

            // CAREFUL: we always insert first, so iteration will be in reverse order to the insertion.
            this._onAbort.AddFirst(act);
        }
    }

}
