// <copyright file="SemaphoreRepository.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;

    /// <summary>
    /// Class SemaphoreRepository will abstract a repository of semaphores under a name.
    /// </summary>
    public sealed class SemaphoreRepository : IDisposable
    {
        /// <summary>
        /// The dictionary of locktokens per Guid
        /// </summary>
        private readonly Dictionary<Guid, LockToken> tokens = new Dictionary<Guid, LockToken>();

        /// <summary>
        /// The rwlock to use on the dictionary
        /// </summary>
        private ReaderWriterLockSlim rw = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        /// <summary>
        /// Prevents a default instance of the <see cref="SemaphoreRepository"/> class from being created.
        /// </summary>
        private SemaphoreRepository()
        {
        }

        /// <summary>
        /// Interface ILockToken defines externally a token to a semaphore managed by SemaphoreRepository
        /// </summary>
        public interface ILockToken
        {
            /// <summary>
            /// Gets the identifier of the semaphore
            /// </summary>
            Guid Id { get; }
        }

        /// <summary>
        /// Gets the singleton for the class
        /// </summary>
        public static SemaphoreRepository Instance { get; } = new SemaphoreRepository();

        /// <inheritdoc />
        public void Dispose()
        {
            if (this.rw != null)
            {
                this.rw.Dispose();
            }

            this.rw = null;
        }

        /// <summary>
        /// locates or creates a semaphore for the given Id, and tries to acquire it up to the given timeout
        /// </summary>
        /// <param name="id">The identifier for the semaphore to acquire.</param>
        /// <param name="timeoutInMillis">The timeout in milliseconds.</param>
        /// <param name="releaseToken">The release token if acquired.</param>
        /// <returns><c>true</c> if true acquisition happened, <c>false</c> otherwise.</returns>
        internal bool FindAndTryAcquire(Guid id, int timeoutInMillis, out ILockToken releaseToken)
        {
            this.rw.EnterWriteLock();
            LockToken ev;

            if (!this.tokens.TryGetValue(id, out ev))
            {
                ev = new LockToken { Id = id, Sem = new Semaphore(1, 1), NumWaiting = 0 };
                this.tokens[id] = ev;
            }

            Interlocked.Increment(ref ev.NumWaiting);
            this.rw.ExitWriteLock();

            bool acq = ev.Sem.WaitOne(timeoutInMillis);

            Interlocked.Decrement(ref ev.NumWaiting);

            releaseToken = acq ? ev : null;

            return acq;
        }

        /// <summary>
        /// Releases the specified token and potentially disposes it if no other thread is waiting on it.
        /// </summary>
        /// <param name="token">The token to be released.</param>
        internal void Release(ILockToken token)
        {
            LockToken lockToken = (LockToken)token;

            this.rw.EnterWriteLock();

            if (lockToken.NumWaiting == 0)
            {
                this.tokens.Remove(lockToken.Id);

                lockToken.Sem.Release();
                lockToken.Sem.Close();
                lockToken.Sem = null;
            }
            else
            {
                lockToken.Sem.Release();
            }

            this.rw.ExitWriteLock();
        }

        /// <summary>
        /// Class LockToken that is used to release the semaphore after acquisition
        /// </summary>
        private class LockToken : ILockToken
        {
            /// <summary>
            /// The number of other threads waiting for this semaphore
            /// </summary>
            [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Scope = "Member", Justification = "Atomic increment/decrement required")]
            public int NumWaiting;

            /// <summary>
            /// Gets or Sets the identifier of the semaphore
            /// </summary>
            public Guid Id { get; set; }

            /// <summary>
            /// Gets or sets the semaphore to be acquired
            /// </summary>
            public Semaphore Sem { get; set; }
        }
    }
}