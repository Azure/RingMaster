// <copyright file="MultiLevelLockPool.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;

    /// <summary>
    /// Multi-level lock pool
    /// </summary>
    public class MultiLevelLockPool : MultiLevelPool<Node, ILockObject>, IDisposable
    {
        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiLevelLockPool"/> class.
        /// </summary>
        /// <param name="sizesPerLevel">Number of lock object at each level</param>
        /// <param name="lastLevelIsOverflow">Whether the last level is for overflow or not</param>
        public MultiLevelLockPool(int[] sizesPerLevel, bool lastLevelIsOverflow)
            : base(NewLock, sizesPerLevel, lastLevelIsOverflow)
        {
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes this object
        /// </summary>
        /// <param name="disposing">whether to dispose from managed code or native code</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    if (this.Levels != null)
                    {
                        // dispose managed state (managed objects) and null large fields
                        for (int i = 0; i < this.Levels.Length; i++)
                        {
                            for (int j = 0; j < this.Levels[i].Pool.Length; j++)
                            {
                                this.Levels[i].Pool[j].Dispose();
                            }

                            this.Levels[i].Pool = null;
                        }

                        this.Levels = null;
                    }
                }

                this.disposedValue = true;
            }
        }

        private static ILockObject NewLock()
        {
            return new LockObject();
        }
    }
}
