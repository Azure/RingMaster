// <copyright file="PoolOf.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;

    /// <summary>
    /// this is a lookaside list of disposable of type T.
    /// It allows to keep a pool of T objects and consume them temporarily.
    /// This reduces the GC churn when it is expensive to dispose objects otherwise cheap to reuse.
    /// Usage:
    ///    // declare the pool. It will cache no more than 100 ManualResetEvents, but will always return one Event when asked for (may create new ones as needed)
    ///    PoolOf&lt;ManualResetEvent&gt; p = new Pool&lt;ManualResetEvent&gt;(100, ()=>{return new ManualResetEvent(false);}, (t)=>{t.Reset();});
    ///    ...
    ///    // Obtain or create one ManualResetEvent from the pool
    ///    ManualResetEvent e = p.GetOne();
    ///    /// ... use e...
    ///    e.Set();
    ///    // ... use e...
    ///    e.WaitOne();
    ///    // once no other reference of e is around,
    ///    p.ReturnOne(ref e);
    ///    // now e is null as a proof that cannot be used anymore
    /// </summary>
    /// <typeparam name="T">the disposable class to build the pool for</typeparam>
    public class PoolOf<T> : IDisposable
        where T : IDisposable
    {
        private readonly Queue<T> pool = new Queue<T>();
        private readonly ReaderWriterLockSlim poolLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private readonly Func<T> createOne;
        private readonly Action<T> reset;
        private SemaphoreSlim semaphore;
        private int numPrepopulate;
        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Initializes a new instance of the <see cref="PoolOf{T}"/> class.
        /// </summary>
        /// <param name="maxCachedElements">maxCachedElements number of cached Ts</param>
        /// <param name="createOne">callback for creation of a new T</param>
        /// <param name="reset">callback to reset a returned T</param>
        /// <param name="blockOnCacheEmpty">if true, the GetOne call will block if the pool is empty, otherwise (default) a new T will be returned</param>
        /// <param name="numPrepopulate">the number of Ts to be pre-populated in the queue (default is 0)</param>
        public PoolOf(
            int maxCachedElements,
            Func<T> createOne,
            Action<T> reset,
            bool blockOnCacheEmpty = false,
            int numPrepopulate = 0)
        {
            this.MaxCachedElements = maxCachedElements;
            this.createOne = createOne;
            this.reset = reset;

            this.SetMaxPoolSize(maxCachedElements, blockOnCacheEmpty, numPrepopulate);
        }

        /// <summary>
        /// Gets the maximum number of cached items
        /// </summary>
        public int MaxCachedElements { get; private set; }

        /// <summary>
        /// retrieve one T from the pool (reset may be invoked at this point), or create a new one
        /// </summary>
        /// <param name="timeoutInMillis">The timeout in milliseconds</param>
        /// <returns>the T ready to be used</returns>
        public T GetOne(int timeoutInMillis = Timeout.Infinite)
        {
            if (this.semaphore != null)
            {
                if (!this.semaphore.Wait(timeoutInMillis))
                {
                    return default(T);
                }
            }

            this.poolLock.EnterWriteLock();

            try
            {
                if (this.numPrepopulate > 0)
                {
                    this.PrepopulateIfNeeded();
                }

                if (this.pool.Count > 0)
                {
                    return this.pool.Dequeue();
                }
            }
            finally
            {
                this.poolLock.ExitWriteLock();
            }

            try
            {
                T res = this.createOne();

                if (res == null)
                {
                    throw new ArgumentException("created pool objects cannot return null");
                }

                return res;
            }
            catch (Exception)
            {
                this.semaphore?.Release();
                throw;
            }
        }

        /// <summary>
        /// returns a T to the pool (reset may be invoked at this point)
        /// there should be no alive reference to t other than the one passed to this method.
        /// is isDisposed == true, just reports to the pool t has been disposed. This means the pool will account for it and eventually create a replacement for it.
        /// </summary>
        /// <param name="t">the T to be returned. null at exit</param>
        /// <param name="isDisposed">if true, the object has already been disposed, and the pool cannot reuse it.</param>
        public void ReturnOne(ref T t, bool isDisposed = false)
        {
            T ourT = t;
            if (t == null)
            {
                throw new ArgumentNullException(nameof(t));
            }

            t = default(T);

            if (this.reset != null && !isDisposed)
            {
                this.reset(ourT);
            }

            this.poolLock.EnterWriteLock();

            try
            {
                if (this.pool.Count < this.MaxCachedElements)
                {
                    if (!isDisposed)
                    {
                        this.pool.Enqueue(ourT);
                    }

                    this.semaphore?.Release();
                    return;
                }
            }
            finally
            {
                this.poolLock.ExitWriteLock();
            }

            if (!isDisposed)
            {
                ourT.Dispose();
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// This code added to correctly implement the disposable pattern.
        /// </remarks>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// sets the max number of elements handed over before blocking
        /// </summary>
        /// <param name="maxCachedElements">The max number of cached elements</param>
        /// <param name="blockOnCacheEmpty">Whether to block when the cache is empty</param>
        /// <param name="numPrepopulate">Number of elements to pre-populate</param>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Semaphore is stored in field")]
        internal void SetMaxPoolSize(int maxCachedElements, bool blockOnCacheEmpty, int numPrepopulate = 0)
        {
            if (maxCachedElements < 0)
            {
                throw new ArgumentException("maxCachedElements cannot be <0");
            }

            if (numPrepopulate < 0)
            {
                throw new ArgumentException("numPrepopulate cannot be <0");
            }

            if (numPrepopulate > maxCachedElements)
            {
                throw new ArgumentException("numPrepopulate cannot be > maxCachedElements");
            }

            this.poolLock.EnterWriteLock();
            try
            {
                if (this.pool.Count != 0)
                {
                    throw new InvalidOperationException("cannot change the pool configuration after use.");
                }

                this.MaxCachedElements = maxCachedElements;

                this.semaphore =
                    blockOnCacheEmpty
                    ? new SemaphoreSlim((int)maxCachedElements, (int)maxCachedElements)
                    : null;
            }
            finally
            {
                this.poolLock.ExitWriteLock();
            }

            this.numPrepopulate = numPrepopulate;
        }

        /// <summary>
        /// Disposing this object
        /// </summary>
        /// <param name="disposing">If disposing from managed code or native code</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.poolLock.EnterWriteLock();

                    try
                    {
                        foreach (T t in this.pool)
                        {
                            t.Dispose();
                        }

                        this.pool.Clear();
                    }
                    finally
                    {
                        this.poolLock.ExitWriteLock();
                    }
                }

                this.disposedValue = true;
            }
        }

        private void PrepopulateIfNeeded()
        {
            if (this.numPrepopulate == 0)
            {
                return;
            }

            if (this.numPrepopulate > this.MaxCachedElements)
            {
                this.numPrepopulate = (int)this.MaxCachedElements;
            }

            List<T> list = new List<T>();
            while (this.numPrepopulate > 0)
            {
                T res = this.createOne();
                list.Add(res);
                this.numPrepopulate--;
            }

            foreach (T el in list)
            {
                this.reset?.Invoke(el);
                this.pool.Enqueue(el);
            }
        }
    }
}
