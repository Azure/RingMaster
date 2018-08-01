// <copyright file="MultiLevelPool.cs" company="Microsoft Corporation">
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
        /// <summary>
        /// Initializes a new instance of the <see cref="MultiLevelPool{TKeyObject, TPoolObject}"/> class.
        /// </summary>
        /// <param name="constructor">Constructor of the pool object</param>
        /// <param name="sizesPerLevel">sizes per level</param>
        /// <param name="lastLevelIsOverflow">Whether the last level is for overflow</param>
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

        /// <summary>
        /// Gets or sets the pools at every level
        /// </summary>
        protected LevelPool[] Levels { get; set; }

        /// <summary>
        /// Gets a value indicating whether the last level is for overflow, i.e. all further levels
        /// </summary>
        protected bool LastLevelIsOverflow { get; }

        /// <summary>
        /// Gets an element at the specified level for the given key
        /// </summary>
        /// <param name="level">Level to look for</param>
        /// <param name="o">Key of the element</param>
        /// <returns>Element retrieved</returns>
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

        /// <summary>
        /// Pool of object at one level
        /// </summary>
        public class LevelPool
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="LevelPool"/> class.
            /// </summary>
            /// <param name="constructor">Constructor of pool object</param>
            /// <param name="numLocks">Number of objects</param>
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

            /// <summary>
            /// Gets or sets the pool of objects
            /// </summary>
            public TPoolObject[] Pool { get; set; }

            /// <summary>
            /// Gets the pool object for the given key
            /// </summary>
            /// <param name="o">Key object</param>
            /// <returns>Pool object retrieved</returns>
            public TPoolObject GetPoolElementForObject(TKeyObject o)
            {
                return this.Pool[o.GetHashCode() % this.Pool.Length];
            }
        }
    }
}