// <copyright file="LayeredLockCollection.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using HelperTypes;

    /// <summary>
    /// Collects of locks in the in-memory tree for processing a single request (simple or compound) from client.
    /// </summary>
    /// <remarks>
    /// Intended calling sequence is:
    /// - Constructor.
    /// - AddLock, AddLock, ...
    /// - Acquire.
    /// - Release.
    /// </remarks>
    public sealed class LayeredLockCollection
    {
        /// <summary>
        /// Value to represent reader locks during bitwise operations
        /// </summary>
        private const byte ReaderLockValue = 0x00;

        /// <summary>
        /// Value to represent reader-writer locks during bitwise operations. This is intended to be greater
        /// numerically than the value of reader lock.
        /// </summary>
        private const byte WriterLockValue = 0x01;

        /// <summary>
        /// Eventually we will enforce no adding new locks after acquisition of all existing ones
        /// </summary>
        private static readonly bool NoAddingAfterLockAcquisition = false;

        /// <summary>
        /// At the moment locks may be released again after they are released. Second time release is no op.
        /// Later we may enforce the state transition.
        /// </summary>
        private static readonly bool NoReleasingAfterLockRelease = false;

        /// <summary>
        /// Sorted list of nodes to be (or being) locked in corresponding level of the multi-level lock pool.
        /// </summary>
        /// <remarks>
        /// Value of 0 means reader lock is required, 1 means reader-writer lock is required.
        /// </remarks>
        private readonly SortedList<Node, byte>[] nodeToBeLocked;

        /// <summary>
        /// List of locks which have been acquired, where the value indicating if reader-writer lock is acquired (1), or read-only (0).
        /// </summary>
        private readonly Stack<KeyValuePair<ILockObject, byte>> nodeHaveBeenLocked;

        /// <summary>
        /// Measurement of lock acquisition duration
        /// </summary>
        private readonly Stopwatch clock = Stopwatch.StartNew();

        /// <summary>
        /// If any lock in <see cref="nodeToBeLocked"/> is reader-writer lock
        /// </summary>
        private bool hasWriterLock = false;

        /// <summary>
        /// Enforce the state transition to not add locks after acquisition or release
        /// </summary>
        private LockStage lockStage = LockStage.AddingLock;

        /// <summary>
        /// Initializes a new instance of the <see cref="LayeredLockCollection"/> class
        /// </summary>
        public LayeredLockCollection()
        {
            this.nodeToBeLocked = Enumerable.Range(0, Node.LockStrategy.LockLevelCount)
                .Select(n => new SortedList<Node, byte>(ByNodeNameAndId.Instance))
                .ToArray();
            this.nodeHaveBeenLocked = new Stack<KeyValuePair<ILockObject, byte>>();
        }

        /// <summary>
        /// Different state of lock collections
        /// </summary>
        private enum LockStage
        {
            /// <summary>
            /// Can add more locks or acquire lock
            /// </summary>
            AddingLock,

            /// <summary>
            /// Can release lock after locks are collected
            /// </summary>
            Locked,

            /// <summary>
            /// No more operation is permitted after lock is released
            /// </summary>
            Released,
        }

        /// <summary>
        /// Gets or sets the max time to acquire a lock
        /// </summary>
        public static TimeSpan MaxAcquireLockTime { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            var toBeLockedString = string.Join(
                ";",
                this.nodeToBeLocked.Select(
                    level => string.Join(",", level.Select(x => string.Concat(x.Key.Name, ":", x.Value)))));

            return string.Format(
                "{0} {1} Locked:{2} ToLock:{3}",
                this.lockStage.ToString(),
                this.hasWriterLock ? "RW" : "RO",
                string.Join(", ", this.nodeHaveBeenLocked.Select(x => x.Value)),
                toBeLockedString);
        }

        /// <summary>
        /// Adds a node to pending lock list to indicate the specified node at a certain lock requires reader or
        /// reader-writer lock
        /// </summary>
        /// <param name="n">Node object</param>
        /// <param name="level">Level of the node counted from the root</param>
        /// <param name="writeRequired">true if writer lock required, false if reader lock required</param>
        public void AddLock(Node n, int level, bool writeRequired)
        {
            Debug.Assert(n != null, "node should not be null");
            Debug.Assert(level >= 0, "Level must be zero or positive integer");

            if (NoAddingAfterLockAcquisition && this.lockStage != LockStage.AddingLock)
            {
                throw new InvalidOperationException($"Cannot call {nameof(this.AddLock)} in stage {this.lockStage}");
            }

            level = level < this.nodeToBeLocked.Length
                ? level
                : this.nodeToBeLocked.Length - 1;

            var list = this.nodeToBeLocked[level];
            var lockValue = writeRequired ? WriterLockValue : ReaderLockValue;

            if (!list.ContainsKey(n))
            {
                list.Add(n, lockValue);
            }
            else
            {
                list[n] |= lockValue;
            }

            this.hasWriterLock |= writeRequired;
        }

        /// <summary>
        /// Acquires all the locks in a top-down, left-right order.
        /// </summary>
        /// <param name="cancelled">Cancellation token to cancel the long-running lock acquisition</param>
        public void Acquire(ref bool cancelled)
        {
            if (this.lockStage != LockStage.AddingLock)
            {
                throw new InvalidOperationException($"Cannot call {nameof(this.Acquire)} in stage {this.lockStage}");
            }

            if (this.hasWriterLock)
            {
                this.RemoveRedundantLocks();
            }

            // Mark it before potential cancellation
            this.lockStage = LockStage.Locked;

            // For O(1) lock collision check. Collision can only happen within a level, not between different levels.
            // Index refers to the index of the lock object in the following locks list.
            var lockSet = new Dictionary<ILockObject, int>();

            // Ordered collection of each lock to be acquired
            var locks = new List<KeyValuePair<ILockObject, byte>>();

            // For logging and debugging
            var sb = new StringBuilder();
            for (int level = 0; level < this.nodeToBeLocked.Length && !cancelled; level++)
            {
                locks.Clear();

                var idx = 0; // lock index for debugging
                foreach (var nodeLock in this.nodeToBeLocked[level])
                {
                    var lockRequired = nodeLock.Value;
                    var lockObj = nodeLock.Key.GetLockObject(level, lockRequired == WriterLockValue);

                    // In read free case, no lock is required.
                    if (lockObj == null)
                    {
                        continue;
                    }

                    // Lock collision found. No operation if current one is read because previous one is at least read.
                    int existingLockObjIndex;
                    if (lockSet.TryGetValue(lockObj, out existingLockObjIndex))
                    {
                        // Ignore the collision if the reader is acquired.
                        if (lockRequired == WriterLockValue &&
                            locks[existingLockObjIndex].Value == ReaderLockValue)
                        {
                            locks[existingLockObjIndex] = new KeyValuePair<ILockObject, byte>(lockObj, WriterLockValue);

                            sb.AppendLine(string.Join(",", level.ToString(), nodeLock.Key.Name, nodeLock.Key.BuildPath(), lockRequired.ToString(), "C"));
                        }
                    }
                    else
                    {
                        // No collision
                        lockSet.Add(lockObj, idx);
                        idx++;

                        locks.Add(new KeyValuePair<ILockObject, byte>(lockObj, lockRequired));

                        sb.AppendLine(string.Join(",", level.ToString(), nodeLock.Key.Name, nodeLock.Key.BuildPath(), lockRequired.ToString()));
                    }
                }

                idx = 0;
                foreach (var lockOp in locks)
                {
                    if (cancelled)
                    {
                        break;
                    }

                    var startTime = this.clock.Elapsed;
                    var succeeded = lockOp.Value == ReaderLockValue
                        ? lockOp.Key.AcquireReaderLock(MaxAcquireLockTime)
                        : lockOp.Key.AcquireWriterLock(MaxAcquireLockTime);

                    RingMasterServerInstrumentation.Instance.OnAcquireLock(true, succeeded, level, this.clock.Elapsed - startTime);

                    if (!succeeded)
                    {
                        throw new RetriableOperationException(
                            $"Lock acquisition timed out after {MaxAcquireLockTime.TotalMilliseconds}: level {level} index {idx} in {sb}");
                    }

                    this.nodeHaveBeenLocked.Push(lockOp);
                    idx++;
                }
            }

            RingMasterEventSource.Log.LockCollectionAcquired(Thread.CurrentThread.ManagedThreadId, sb.ToString());
        }

        /// <summary>
        /// Releases all the locks in the reverse order of acquisition.
        /// </summary>
        public void Release()
        {
            if (this.nodeHaveBeenLocked.Count == 0)
            {
                // Allow the lock to be released again... Will enforce the state transition later.
                return;
            }
            else if (NoReleasingAfterLockRelease && this.lockStage == LockStage.Released)
            {
                throw new InvalidOperationException($"Cannot call {nameof(this.Release)} in stage Released");
            }
            else if (this.lockStage == LockStage.Locked)
            {
                // Immediately mark it as released
                this.lockStage = LockStage.Released;

                while (this.nodeHaveBeenLocked.Count > 0)
                {
                    var lockPair = this.nodeHaveBeenLocked.Pop();
                    if (lockPair.Value == 0)
                    {
                        lockPair.Key.ReleaseReaderLock();
                    }
                    else
                    {
                        lockPair.Key.ReleaseWriterLock();
                    }
                }

                RingMasterEventSource.Log.LockCollectionReleased(Thread.CurrentThread.ManagedThreadId);
            }

            // Do nothing if locks are collected but not acquired.
        }

        /// <summary>
        /// Remove redundancy locks under parent nodes that require reader-writer lock
        /// </summary>
        private void RemoveRedundantLocks()
        {
            if (this.nodeToBeLocked.All(level => level.Count < 2))
            {
                // Single list
                bool foundWriterLock = false;
                for (int n = 0; n < this.nodeToBeLocked.Length; n++)
                {
                    if (foundWriterLock)
                    {
                        this.nodeToBeLocked[n].Clear();
                    }
                    else if (this.nodeToBeLocked[n].First().Value > ReaderLockValue)
                    {
                        foundWriterLock = true;
                    }
                }
            }
            else
            {
                var nodesToRemove = new HashSet<ulong>();
                foreach (var nodeList in this.nodeToBeLocked)
                {
                    // If the current level is empty, further level must also be empty.
                    if (nodeList.Count == 0)
                    {
                        break;
                    }

                    // All children of removed parent nodes (decending from a node requires writer lock) should be
                    // removed, if they are listed in this level.
                    nodesToRemove.IntersectWith(nodeList.Keys.Select(n => n.Persisted.Id));

                    // Convert ID to node object, since we cannot remove element during enumeration
                    var nodes = nodeList.Where(n => nodesToRemove.Contains(n.Key.Persisted.Id));

                    foreach (var nodeKvp in nodes)
                    {
                        nodeList.Remove(nodeKvp.Key);
                    }

                    // Find out all new nodes that require writer locks at this level
                    foreach (var nodeKvp in nodeList)
                    {
                        if (nodeKvp.Value > ReaderLockValue)
                        {
                            nodesToRemove.Add(nodeKvp.Key.Persisted.Id);
                        }
                    }

                    // Childrens of removed nodes and childrens of nodes that require writer locks
                    nodesToRemove = new HashSet<ulong>(nodesToRemove
                        .OfType<CompleteNode>()
                        .SelectMany(node => node.ChildrenNodes)
                        .Select(node => node.Id));
                }
            }
        }

        /// <summary>
        /// Compares two nodes by their persisted name and then ID (if names are identical)
        /// </summary>
        private sealed class ByNodeNameAndId : IComparer<Node>
        {
            /// <summary>
            /// Singleton instance of the <see cref="ByNodeNameAndId"/> class
            /// </summary>
            private static readonly ByNodeNameAndId ThisInstance = new ByNodeNameAndId();

            /// <summary>
            /// Initializes a new instance of the <see cref="ByNodeNameAndId"/> class.
            /// Prevents the class from being instantiated by non-singleton use
            /// </summary>
            private ByNodeNameAndId()
            {
            }

            /// <summary>
            /// Gets the singleton instance of the <see cref="ByNodeNameAndId"/> class
            /// </summary>
            public static ByNodeNameAndId Instance
            {
                get
                {
                    return ThisInstance;
                }
            }

            /// <summary>
            /// Compares two nodes and returns a value indicating whether one node is less than, equal to, or greater
            /// than the other node
            /// </summary>
            /// <param name="x">The first node to compare</param>
            /// <param name="y">The second node to compare</param>
            /// <returns>A signed integer to indicate the relative values of x and y</returns>
            public int Compare(Node x, Node y)
            {
                if (x == null)
                {
                    throw new ArgumentNullException(nameof(x));
                }

                if (y == null)
                {
                    throw new ArgumentNullException(nameof(y));
                }

                var px = x.Persisted;
                var py = y.Persisted;

                var nameCompareResult = string.CompareOrdinal(px.Name, py.Name);
                return nameCompareResult != 0
                    ? nameCompareResult
                    : px.Id.CompareTo(py.Id);
            }
        }
    }
}
