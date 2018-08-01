// <copyright file="LockObject.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System;
    using System.Threading;

    /// <summary>
    /// Reader-writer lock
    /// </summary>
    public class LockObject : ILockObject
    {
        private readonly ReaderWriterLockSlim readerWriterLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private bool disposed = false;

        /// <inheritdoc />
        public bool AcquireReaderLock(TimeSpan timeout)
        {
            return this.readerWriterLock.TryEnterReadLock(timeout);
        }

        /// <inheritdoc />
        public bool AcquireWriterLock(TimeSpan timeout)
        {
            return this.readerWriterLock.TryEnterWriteLock(timeout);
        }

        /// <inheritdoc />
        public void ReleaseReaderLock()
        {
            this.readerWriterLock.ExitReadLock();
        }

        /// <inheritdoc />
        public void ReleaseWriterLock()
        {
            this.readerWriterLock.ExitWriteLock();
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
        /// <param name="disposing">If disposing from managed code or native code</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                this.readerWriterLock.Dispose();
                this.disposed = true;
            }
        }
    }
}
