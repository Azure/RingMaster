// <copyright file="LRUCache.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// LRU cache
    /// </summary>
    /// <typeparam name="TKey">Type of cache key</typeparam>
    /// <typeparam name="TValue">Type of cache value</typeparam>
    public sealed class LruCache<TKey, TValue> : IDisposable
    {
        /// <summary>
        /// The dictionary of keys that are declared to be inflight
        /// </summary>
        private readonly Dictionary<TKey, EventAndCount> inflight = new Dictionary<TKey, EventAndCount>();

        /// <summary>
        /// The maximum capacity of the cache
        /// </summary>
        private int maxCapacity;

        /// <summary>
        /// The number of elements currently present in the cache
        /// </summary>
        private int count;

        /// <summary>
        /// The dictionary of positions in the linked list, indexed by key
        /// </summary>
        private Dictionary<TKey, LinkedListNode<CacheItem>> positionsByKey = null;

        /// <summary>
        /// The RW lock for accessing this cache
        /// </summary>
        private ReaderWriterLockSlim isCacheWriting = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// The linked list containing the elements, sorted by less recently utilized first
        /// </summary>
        private LinkedList<CacheItem> lruList = null;

        /// <summary>
        /// If true, we will need to dispose the items as the are ejected from the cache.
        /// </summary>
        private bool disposeItems = false;

        /// <summary>
        /// The maximum milliseconds "unaccessed" to allow an item to be in the middle of the lrulist
        /// </summary>
        private uint maxMillisecondsToAllowInMiddleOfList;

        /// <summary>
        /// The maximum milliseconds "unaccesed" to allow an element in the top of the lruList.
        /// </summary>
        private uint maxMillisecondsToAllowInListTop;

        /// <summary>
        /// The number of elements to attempt move in bulk if we need to move them in the lruList
        /// </summary>
        private uint numberOfElementsToAttemptMoveInBulk;

        /// <summary>
        /// The maximum percentage capacity under which we will not try to resort the LRU elements.
        /// </summary>
        private double maxCapacityPercentageForSorting;

        /// <summary>
        /// The perennial keys, those that are only removed from the cache if "Clear" is invoked.
        /// </summary>
        private HashSet<TKey> perennialKeys = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="LruCache{TKey,TValue}"/> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        /// <param name="maxMillisecondsToAllowInMiddleOfList">The maximum milliseconds to allow an "unaccessed object" to stay in middle of list.</param>
        /// <param name="maxMillisecondsToAllowInListTop">The maximum milliseconds to allow an "unaccessed object" to stay in list top.</param>
        /// <param name="numberOfElementsToAttemptMoveInBulk">The number of elements to attempt move in bulk from top if we need to move objects.</param>
        /// <param name="maxCapacityPercentageForSorting">The maximum capacity percentage under which we will not bother sorting.</param>
        public LruCache(int capacity, uint maxMillisecondsToAllowInMiddleOfList, uint maxMillisecondsToAllowInListTop, uint numberOfElementsToAttemptMoveInBulk, double maxCapacityPercentageForSorting)
        {
            this.Initialize(capacity, maxMillisecondsToAllowInMiddleOfList, maxMillisecondsToAllowInListTop, numberOfElementsToAttemptMoveInBulk, maxCapacityPercentageForSorting);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LruCache{TKey,TValue}"/> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        /// <param name="pureLru">Whether to initialize as a pure LRU cache.</param>
        public LruCache(int capacity, bool pureLru = false)
        {
            if (!pureLru)
            {
                this.Initialize(capacity, 2000, 15000, 100, 0.8);
            }
            else
            {
                this.Initialize(capacity, 0, 0, 0, 0);
            }
        }

        /// <summary>
        /// Gets the current count.
        /// </summary>
        /// <value>The current count.</value>
        public int Count => this.count;

        /// <summary>
        /// Gets the maximum capacity.
        /// </summary>
        /// <value>The current count.</value>
        public int MaxCapacity => this.maxCapacity;

        /// <summary>
        /// Gets or sets the maximum percentage capacity (in the range [0.0 .. 1.0]) under which we will not try to resort the LRU elements.
        /// </summary>
        public double MaxCapacityPercentageForSorting
        {
            get
            {
                return this.maxCapacityPercentageForSorting;
            }

            set
            {
                if (value < 0 || value > 1)
                {
                    throw new ArgumentException("MaxCapaciotyPercentageForSorting needs to be in [0.0 .. 1.0]");
                }

                this.maxCapacityPercentageForSorting = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of elements to attempt move in bulk if we need to move them in the lruList
        /// </summary>
        /// <value>The number of elements to attempt move in bulk.</value>
        public uint NumberOfElementsToAttemptMoveInBulk
        {
            get
            {
                return this.numberOfElementsToAttemptMoveInBulk;
            }

            set
            {
                this.numberOfElementsToAttemptMoveInBulk = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum milliseconds "unaccesed" to allow an element in the top of the lruList.
        /// </summary>
        /// <value>The maximum milliseconds to allow in list top.</value>
        public uint MaxMillisecondsToAllowInListTop
        {
            get
            {
                return this.maxMillisecondsToAllowInListTop;
            }

            set
            {
                this.maxMillisecondsToAllowInListTop = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum milliseconds "unaccessed" to allow an item to be in the middle of the lrulist
        /// </summary>
        /// <value>The maximum milliseconds to allow in middle of list.</value>
        public uint MaxMillisecondsToAllowInMiddleOfList
        {
            get
            {
                return this.maxMillisecondsToAllowInMiddleOfList;
            }

            set
            {
                this.maxMillisecondsToAllowInMiddleOfList = value;
            }
        }

        /// <summary>
        /// Adds a perennial key
        /// </summary>
        /// <param name="key">Key to be added</param>
        public void AddPerennial(TKey key)
        {
            this.isCacheWriting.EnterWriteLock();
            try
            {
                if (this.perennialKeys == null)
                {
                    this.perennialKeys = new HashSet<TKey>();
                }

                this.perennialKeys.Add(key);
            }
            finally
            {
                this.isCacheWriting.ExitWriteLock();
            }
        }

        /// <summary>
        /// Clears all perennial keys
        /// </summary>
        public void ClearPerennials()
        {
            this.isCacheWriting.EnterWriteLock();
            try
            {
                this.perennialKeys = null;
            }
            finally
            {
                this.isCacheWriting.ExitWriteLock();
            }
        }

        /// <summary>
        /// Tries to get the value for the given key, and updates the lru list to have this element last in the lru list.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="val">The value.</param>
        /// <returns><c>true</c> if the value was found, <c>false</c> otherwise.</returns>
        public bool TryGetValue(TKey key, out TValue val)
        {
            this.isCacheWriting.EnterReadLock();
            bool moveNeeded = false;
            bool removeNeeded = false;
            LinkedListNode<CacheItem> node = null;

            try
            {
                if (this.positionsByKey.TryGetValue(key, out node))
                {
                    // we found the element, so we need to move it to the back of the LRU list
                    val = node.Value.Value;

                    int millisSincePreviousAccess = node.Value.SetTime();

                    if (millisSincePreviousAccess > this.maxMillisecondsToAllowInMiddleOfList &&
                        this.count > this.maxCapacity * this.maxCapacityPercentageForSorting)
                    {
                        moveNeeded = true;
                    }

                    return true;
                }

                val = default(TValue);

                removeNeeded = this.count >= this.maxCapacity;

                return false;
            }
            finally
            {
                this.isCacheWriting.ExitReadLock();

                // it is okay if another thread injects itself between the above release and the following acquire,
                // since in the worst case, such race would move the node a little further in the LRU list.
                if (removeNeeded || moveNeeded)
                {
                    this.isCacheWriting.EnterWriteLock();
                    try
                    {
                        // because we may have lazy reduction of the capacity, we check here if we can remove something now.
                        if (this.count >= this.maxCapacity)
                        {
                            this.RemoveFirstUnsafe();
                        }

                        if (moveNeeded)
                        {
                            this.MoveNodeUnsafe(node);
                        }
                    }
                    finally
                    {
                        this.isCacheWriting.ExitWriteLock();
                    }
                }
            }
        }

        /// <summary>
        /// Finds out if the key is present in the dictionary, but it doesn't affect the LRU content or access lists.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if the value was found, <c>false</c> otherwise.</returns>
        public bool Contains(TKey key)
        {
            this.isCacheWriting.EnterReadLock();

            try
            {
                return this.positionsByKey.ContainsKey(key);
            }
            finally
            {
                this.isCacheWriting.ExitReadLock();
            }
        }

        /// <summary>
        /// Adds the specified element to the list, under the given key. It inserts the element last in the lru list.
        /// </summary>
        /// <param name="key">The key of the added item.</param>
        /// <param name="val">The value assicoated to the key.</param>
        /// <exception cref="System.ArgumentException">if the key already exists in the cache</exception>
        public void Add(TKey key, TValue val)
        {
            this.isCacheWriting.EnterWriteLock();
            try
            {
                if (this.count >= this.maxCapacity)
                {
                    this.RemoveFirstUnsafe();
                }

                CacheItem cacheItem = new CacheItem(key, val);
                cacheItem.SetTime();
                LinkedListNode<CacheItem> node = new LinkedListNode<CacheItem>(cacheItem);

                // IMPORTANT: first add to the dictionary, and only then add to the list,
                // so if the key was already present, we don't leave behind garbage in the list
                this.positionsByKey.Add(key, node);
                this.lruList.AddLast(node);
                this.count++;

                // sanity checks
                System.Diagnostics.Debug.Assert(this.count == this.lruList.Count, "Sanity check");
                System.Diagnostics.Debug.Assert(this.count == this.positionsByKey.Count, "Sanity check");
            }
            finally
            {
                this.isCacheWriting.ExitWriteLock();
            }
        }

        /// <summary>
        /// Declares the key is going to be computed.
        /// It blocks the caller if there is another thread computing that key. When that other thread finishes,
        /// this thread is released and returned false. the caller is expected to get the item from the cache
        /// then (and caller must assume the object may be removed -unlikely but possible- by the time the caller tries to retrieve it).
        /// If the method returns true, the caller to invoke "Complete" once it adds the element to the cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if true, the key doesn't have a value and is not being computed yet<c>false</c> otherwise.</returns>
        public bool StartInflight(TKey key)
        {
            EventAndCount ev = null;

            lock (this.inflight)
            {
                if (!this.inflight.TryGetValue(key, out ev))
                {
                    this.inflight.Add(key, null);
                }
                else
                {
                    if (ev == null)
                    {
                        ev = new EventAndCount();
                        this.inflight[key] = ev;
                    }
                }
            }

            if (ev == null)
            {
                return true;
            }

            ev.WaitAndReturnIfLast();

            return false;
        }

        /// <summary>
        /// Completes the inflight operation to the specified key.
        /// This method needs to be invoked right after the Add or TryAdd call if the caller invoked "Inflight".
        /// </summary>
        /// <param name="key">The key just added.</param>
        /// <exception cref="System.InvalidOperationException">Complete was invoked without a corresponding Inflight call</exception>
        public void CompleteInflight(TKey key)
        {
            lock (this.inflight)
            {
                EventAndCount ev = null;
                if (!this.inflight.TryGetValue(key, out ev))
                {
                    throw new InvalidOperationException("Complete was invoked without a corresponding Inflight call");
                }

                this.inflight.Remove(key);

                // if anybody was waiting on this, now we tell them.
                ev?.Set();
            }
        }

        /// <summary>
        /// Tries to add an element to the dictionary.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="val">The value.</param>
        /// <returns><c>true</c> if the object was added (and then val doesn't change), <c>false</c> if the dictionary already had an object with that key (and then val changes to be that exisgint object).</returns>
        public bool TryAdd(TKey key, ref TValue val)
        {
            this.isCacheWriting.EnterWriteLock();
            try
            {
                if (this.count >= this.maxCapacity)
                {
                    this.RemoveFirstUnsafe();
                }

                CacheItem cacheItem = new CacheItem(key, val);
                cacheItem.SetTime();
                LinkedListNode<CacheItem> node = new LinkedListNode<CacheItem>(cacheItem);

                // IMPORTANT: first add to the dictionary, and only then add to the list,
                // so if the key was already present, we don't leave behind garbage in the list
                try
                {
                    this.positionsByKey.Add(key, node);
                }
                catch (ArgumentException)
                {
                    if (!this.positionsByKey.TryGetValue(key, out node))
                    {
                        throw;
                    }

                    // if the element was already in the dictionary (and that is why we got the exception
                    // we don't add it but instead we tell the caller what is the value that exists, and return false instead
                    val = node.Value.Value;
                    return false;
                }

                this.lruList.AddLast(node);
                this.count++;

                // return true since we added the new object
                return true;
            }
            finally
            {
                // sanity checks
                System.Diagnostics.Debug.Assert(this.count == this.lruList.Count, "Sanity check");
                System.Diagnostics.Debug.Assert(this.count == this.positionsByKey.Count, "Sanity check");

                this.isCacheWriting.ExitWriteLock();
            }
        }

        /// <summary>
        /// Clears the cache contents
        /// </summary>
        public void Clear()
        {
            this.isCacheWriting.EnterWriteLock();
            try
            {
                this.positionsByKey = new Dictionary<TKey, LinkedListNode<CacheItem>>();

                if (this.disposeItems)
                {
                    foreach (CacheItem item in this.lruList)
                    {
                        ((IDisposable)item.Value).Dispose();
                    }
                }

                this.lruList = new LinkedList<CacheItem>();
                this.count = 0;
            }
            finally
            {
                this.isCacheWriting.ExitWriteLock();
            }
        }

        /// <summary>
        /// Changes the maximum capacity of the cache.
        /// </summary>
        /// <param name="capacity">The new maximum capacity.</param>
        /// <param name="cleanupNow">if set to <c>true</c> the function will also cleanup the excess capacity before returning. If set to false, the cleanup will happen lazily as new elements are added or retrieved.</param>
        public void ChangeCapacity(int capacity, bool cleanupNow)
        {
            this.isCacheWriting.EnterWriteLock();

            try
            {
                this.maxCapacity = capacity;

                if (cleanupNow)
                {
                    while (this.count >= this.maxCapacity)
                    {
                        this.RemoveFirstUnsafe();
                    }
                }
            }
            finally
            {
                this.isCacheWriting.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes the given element from the list, and from the dictionary.
        /// </summary>
        /// <param name="key">The key for the entry to remove</param>
        /// <returns>Whether key exists</returns>
        public bool Remove(TKey key)
        {
            this.isCacheWriting.EnterWriteLock();
            try
            {
                LinkedListNode<CacheItem> node;

                // IMPORTANT: first add to the dictionary, and only then add to the list,
                // so if the key was already present, we don't leave behind garbage in the list
                this.positionsByKey.TryGetValue(key, out node);

                if (node == null)
                {
                    return false;
                }

                // IMPORTANT: although not strictly necessary here, we assume everywhere in the class
                // that all element in the dictionary is present in the list,
                // so we first remove from the list, and then from the dictionary, if that second remove fails, the basic assumption
                // had been violated. Even with that assumption violated, though, it is safe to proceed because the LRU item was just
                // not indexed.
                if (!this.positionsByKey.Remove(node.Value.Key))
                {
                    System.Diagnostics.Debug.Fail("the dictionary doesn't contain the element!");
                }

                this.lruList.Remove(node.Value);

                this.count--;

                // sanity checks
                System.Diagnostics.Debug.Assert(this.count == this.lruList.Count, "Sanity check");
                System.Diagnostics.Debug.Assert(this.count == this.positionsByKey.Count, "Sanity check");
                return true;
            }
            finally
            {
                this.isCacheWriting.ExitWriteLock();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.isCacheWriting?.Dispose();
            this.isCacheWriting = null;
        }

        /// <summary>
        /// Initializes the LRUCache instance.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        /// <param name="maxMillisecondsToAllowInMiddleOfList">The maximum milliseconds to allow an "unaccessed object" to stay in middle of list.</param>
        /// <param name="maxMillisecondsToAllowInListTop">The maximum milliseconds to allow an "unaccessed object" to stay in list top.</param>
        /// <param name="numberOfElementsToAttemptMoveInBulk">The number of elements to attempt move in bulk from top if we need to move objects.</param>
        /// <param name="maxCapacityPercentageForSorting">The maximum capacity percentage under which we will not bother sorting.</param>
        private void Initialize(int capacity, uint maxMillisecondsToAllowInMiddleOfList, uint maxMillisecondsToAllowInListTop, uint numberOfElementsToAttemptMoveInBulk, double maxCapacityPercentageForSorting)
        {
            if (this.lruList != null)
            {
                throw new InvalidOperationException("Initialize can only be invoked once.");
            }

            this.maxCapacity = capacity;
            this.count = 0;
            this.maxMillisecondsToAllowInMiddleOfList = maxMillisecondsToAllowInMiddleOfList;
            this.maxMillisecondsToAllowInListTop = maxMillisecondsToAllowInListTop;
            this.numberOfElementsToAttemptMoveInBulk = numberOfElementsToAttemptMoveInBulk;
            this.MaxCapacityPercentageForSorting = maxCapacityPercentageForSorting;
            this.positionsByKey = new Dictionary<TKey, LinkedListNode<CacheItem>>();
            this.lruList = new LinkedList<CacheItem>();
            this.disposeItems = typeof(IDisposable).IsAssignableFrom(typeof(TValue));
        }

        /// <summary>
        /// Moves the node if needed to the end of the lru list.
        /// Potentially, it might also move some nodes from the top of the list, if they have been accessed recently enough.
        /// NOTE: This method is not thread safe
        /// </summary>
        /// <param name="node">The node.</param>
        private void MoveNodeUnsafe(LinkedListNode<CacheItem> node)
        {
            if (node.List != this.lruList)
            {
                // the element has been removed from the list and there is no need to move it anymore.
                return;
            }

            this.lruList.Remove(node);
            this.lruList.AddLast(node);

            // if we have to move something, we will see if we need to move any of the first hundred elements in the list
            if (this.lruList.Count > 0)
            {
                uint num = (uint)Math.Min(this.lruList.Count - 1, this.numberOfElementsToAttemptMoveInBulk);

                LinkedListNode<CacheItem> current = this.lruList.First;

                for (int i = 0; i < num; i++)
                {
                    LinkedListNode<CacheItem> next = current.Next;

                    // if the "top" element was accessed recently enough, move it to the back
                    if (current.Value.EllapsedMillisSinceLastAccess <= this.maxMillisecondsToAllowInListTop)
                    {
                        this.lruList.Remove(current);
                        this.lruList.AddLast(current);
                    }

                    current = next;
                }
            }
        }

        /// <summary>
        /// Removes the first element from the list, and from the dictionary.
        /// NOTE: This method is not thread safe
        /// </summary>
        private void RemoveFirstUnsafe()
        {
            // find the first element
            LinkedListNode<CacheItem> node = this.lruList.First;

            // skip the entries that are perennial, and push them to the back
            while (node != null && this.perennialKeys != null && this.perennialKeys.Contains(node.Value.Key))
            {
                LinkedListNode<CacheItem> next = node.Next;

                node.Value.SetTime();
                this.lruList.Remove(node);
                this.lruList.AddLast(node);

                node = next;
            }

            if (node == null)
            {
                return;
            }

            // IMPORTANT: although not strictly necessary here, we assume everywhere in the class
            // that all element in the dictionary is present in the list,
            // so we first remove from the list, and then from the dictionary, if that second remove fails, the basic assumption
            // had been violated. Even with that assumption violated, though, it is safe to proceed because the LRU item was just
            // not indexed.
            if (!this.positionsByKey.Remove(node.Value.Key))
            {
                System.Diagnostics.Debug.Fail("the dictionary doesn't contain the element!");
            }

            if (this.disposeItems)
            {
                IDisposable item = (IDisposable)node.Value.Value;
                item?.Dispose();
            }

            this.lruList.RemoveFirst();

            this.count--;

            // sanity checks
            System.Diagnostics.Debug.Assert(this.count == this.lruList.Count, "Sanity check");
            System.Diagnostics.Debug.Assert(this.count == this.positionsByKey.Count, "Sanity check");
        }

        /// <summary>
        /// Class EventAndCount abstracts an event that is auto-disposed when the last thread waiting on it gets awaken
        /// </summary>
        private class EventAndCount
        {
            private ManualResetEvent ev;
            private volatile int waitCount;
            private SpinLock mayBeReleasing;

            /// <summary>
            /// Initializes a new instance of the <see cref="EventAndCount"/> class.
            /// </summary>
            public EventAndCount()
            {
                this.waitCount = 0;
                this.ev = ManualResetEventPool.InstancePool.GetOne();
                this.mayBeReleasing = default(SpinLock);
            }

            /// <summary>
            /// Sets this instance.
            /// </summary>
            public void Set()
            {
                ManualResetEventPool.InstancePool.Set(this.ev);
            }

            /// <summary>
            /// waits for the event to be set and returns the event if this is the last thread waiting for it.
            /// </summary>
            public void WaitAndReturnIfLast()
            {
                // the idea here is that two threads can be in this routine concurrently.
                // Thread A may be starting the wait (after Interlocked.Increment)
                // Thread B may be finishing the wait (after WaitOne)
                // The goal is to make sure B doesn't ever release if A is about to wait

                // 1. lock the releasing Critical Section
                bool taken = false;
                do
                {
                    this.mayBeReleasing.Enter(ref taken);
                }
                while (!taken);

                // 2. increase the counter of waiting threads
                this.waitCount++;

                // 3. remember the event we have
                ManualResetEvent thisEv = this.ev;

                // 4. unlock the C.S.
                this.mayBeReleasing.Exit();

                // 5. wait, if there is a valid event
                if (thisEv == null)
                {
                    return;
                }

                thisEv.WaitOne();

                // 6. lock the C.S.
                taken = false;
                do
                {
                    this.mayBeReleasing.Enter(ref taken);
                }
                while (!taken);

                // 7. decrease the counter of wait threads, and if none left, release the event
                this.waitCount--;

                if (this.waitCount == 0)
                {
                    ManualResetEventPool.InstancePool.ReturnOne(ref this.ev);
                }

                // 8. unlock the C.S.
                this.mayBeReleasing.Exit();
            }
        }

        /// <summary>
        /// Class LRUCacheItem.
        /// </summary>
        private class CacheItem
        {
            private static readonly long StartTimeInTicks = DateTime.UtcNow.Ticks;

            /// <summary>
            /// the last time this object was accessed,
            /// in milliseconds since the process start up
            /// </summary>
            private int lastAccessInMilisSinceProcessStartUp;

            /// <summary>
            /// Initializes a new instance of the <see cref="CacheItem"/> class.
            /// </summary>
            /// <param name="k">The key of the element.</param>
            /// <param name="v">The value for the elemet.</param>
            public CacheItem(TKey k, TValue v)
            {
                this.Key = k;
                this.Value = v;
            }

            /// <summary>
            /// Gets the key of the item
            /// </summary>
            internal TKey Key { get; }

            /// <summary>
            /// Gets the value of the item
            /// </summary>
            internal TValue Value { get; }

            internal int EllapsedMillisSinceLastAccess
            {
                get
                {
                    int nowInMillis = (int)((DateTime.UtcNow.Ticks - StartTimeInTicks) / TimeSpan.TicksPerMillisecond);
                    return nowInMillis - this.lastAccessInMilisSinceProcessStartUp;
                }
            }

            /// <summary>
            /// Sets the access time for this object.
            /// </summary>
            /// <returns>the time elapsed since the previous access, in milliseconds</returns>
            internal int SetTime()
            {
                int nowInMillis = (int)((DateTime.UtcNow.Ticks - StartTimeInTicks) / TimeSpan.TicksPerMillisecond);
                int lastTime = Interlocked.Exchange(ref this.lastAccessInMilisSinceProcessStartUp, nowInMillis);

                return nowInMillis - lastTime;
            }
        }
    }
}