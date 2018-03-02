// <copyright file="MultiLevelLockPool.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Class MultiLevelLockPool.
    /// it builds pools at multiple levels, with an optional overflow level.
    /// It has the ability to return for each level a pool object mapped to a hash of the given key object.
    /// A single mapped object may map to multiple key objects.
    /// </summary>
    /// <typeparam name="TKeyObject">The key type</typeparam>
    /// <typeparam name="TPoolObject">The pool type</typeparam>
    public class MultiLevelPool<TKeyObject, TPoolObject>
    {
        public class LevelPool
        {
            public TPoolObject[] Pool { get; set; }

            public LevelPool(Func<TPoolObject> constructor, int numLocks)
            {
                if (constructor == null)
                {
                    throw new ArgumentNullException(nameof(constructor));
                }

                if (numLocks <= 0)
                {
                    throw new ArgumentException("numLocks must be >0 " + numLocks);
                }

                this.Pool = new TPoolObject[numLocks];
                for (int i = 0; i < this.Pool.Length; i++)
                {
                    this.Pool[i] = constructor();
                }
            }

            public TPoolObject GetPoolElementForObject(TKeyObject o)
            {
                return this.Pool[o.GetHashCode() % this.Pool.Length];
            }
        }

        protected LevelPool[] Levels { get; set; }

        protected bool LastLevelIsOverflow { get; }

        public MultiLevelPool(Func<TPoolObject> constructor, int[] sizesPerLevel, bool lastLevelIsOverflow)
        {
            if (sizesPerLevel == null)
            {
                throw new ArgumentNullException(nameof(sizesPerLevel));
            }

            this.Levels = new LevelPool[sizesPerLevel.Length];
            for (int i = 0; i < sizesPerLevel.Length; i++)
            {
                this.Levels[i] = new LevelPool(constructor, sizesPerLevel[i]);
            }

            this.LastLevelIsOverflow = lastLevelIsOverflow;
        }

        public TPoolObject GetPoolElementFor(int level, TKeyObject o)
        {
            if (level >= this.Levels.Length)
            {
                if (this.LastLevelIsOverflow)
                {
                    level = this.Levels.Length - 1;
                }
                else
                {
                    throw new KeyNotFoundException("level " + level + " is too high for this pool, which doesn't have overflow");
                }
            }

            return this.Levels[level].GetPoolElementForObject(o);
        }
    }

    /// <summary>
    /// abstraction for a lock
    /// </summary>
    public interface ILockObject : IDisposable
    {
        /// <summary>
        /// gets the value indicating if the current thread owns the lock for RO
        /// </summary>
        bool IsReaderLockHeld { get; }

        /// <summary>
        /// gets the value indicating if the current thread owns the lock for RW
        /// </summary>
        bool IsWriterLockHeld { get; }

        /// <summary>
        /// acquires the lock for RO (within the given timeout)
        /// </summary>
        /// <param name="timeout">timeout to block at most</param>
        /// <returns>true if the lock was acquired, false otherwise</returns>
        bool AcquireReaderLock(TimeSpan timeout);

        /// <summary>
        /// acquires the lock for RW (within the given timeout)
        /// </summary>
        /// <param name="timeout">timeout to block at most</param>
        /// <returns>true if the lock was acquired, false otherwise</returns>
        bool AcquireWriterLock(TimeSpan timeout);

        /// <summary>
        /// release the lock IF it was held for RO
        /// </summary>
        void ReleaseReaderLock();

        /// <summary>
        /// release the lock IF it was held for RW
        /// </summary>
        void ReleaseWriterLock();
    }

    public class LockObject : ILockObject
    {
        private ReaderWriterLock rwL = new ReaderWriterLock();
        private LockCookie upgradecookie;
        private bool downgrade = false;

        public bool IsReaderLockHeld => this.rwL.IsReaderLockHeld;

        public bool IsWriterLockHeld => this.rwL.IsWriterLockHeld;

        public bool AcquireReaderLock(TimeSpan timeout)
        {
            try
            {
                this.rwL.AcquireReaderLock(timeout);
                return true;
            }
            catch (ApplicationException)
            {
                if (timeout == Timeout.InfiniteTimeSpan)
                {
                    throw;
                }

                return false;
            }
        }

        public bool AcquireWriterLock(TimeSpan timeout)
        {
            try
            {
                if (this.rwL.IsReaderLockHeld)
                {
                    this.upgradecookie = this.rwL.UpgradeToWriterLock(timeout);
                    this.downgrade = true;
                }
                else
                {
                    this.rwL.AcquireWriterLock(timeout);
                }

                return true;
            }
            catch (ApplicationException)
            {
                if (timeout == Timeout.InfiniteTimeSpan)
                {
                    throw;
                }

                return false;
            }
        }

        public void ReleaseReaderLock()
        {
            if (!this.IsReaderLockHeld)
            {
                return;
            }

            this.rwL.ReleaseReaderLock();
        }

        public void ReleaseWriterLock()
        {
            if (!this.IsWriterLockHeld)
            {
                return;
            }

            if (this.downgrade)
            {
                this.downgrade = false;
                this.rwL.DowngradeFromWriterLock(ref this.upgradecookie);
            }
            else
            {
                this.rwL.ReleaseWriterLock();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            this.rwL = null;
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}