// <copyright file="ILockObject.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// abstraction for a lock
    /// </summary>
    public interface ILockObject : IDisposable
    {
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
}
