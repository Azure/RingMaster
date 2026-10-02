// <copyright file="RecyclableMemoryBufferPool.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using Microsoft.IO;

    /// <summary>
    /// Recyclable memory buffer pool.
    /// </summary>
    public static class RecyclableMemoryBufferPool
    {
        private static RecyclableMemoryStreamManager memoryStreamManager;

        /// <summary>
        /// Gets the recyclable memory stream manager instance.
        /// </summary>
        /// <value>
        /// The instance.
        /// </value>
        public static RecyclableMemoryStreamManager Instance
        {
            get
            {
                // provide default initialization if it was not explicitly initialized
                if (memoryStreamManager == null)
                {
                    memoryStreamManager = new RecyclableMemoryStreamManager();
                }

                return memoryStreamManager;
            }
        }

        /// <summary>
        /// Initializes the recyclable memory stream manager.
        /// </summary>
        /// <param name="blockSize">Block size of the recyclable memory streams.</param>
        /// <param name="largeBufferMultiple">The large buffer pool will contain multiples of this size.</param>
        /// <param name="maximumBufferSize">The maximum buffer size to pool. Allocations larger than this size will not be pooled.</param>
        public static void Initialize(int blockSize, int largeBufferMultiple, int maximumBufferSize)
        {
            memoryStreamManager = new RecyclableMemoryStreamManager(blockSize, largeBufferMultiple, maximumBufferSize);
        }
    }
}
