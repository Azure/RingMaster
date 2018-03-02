// <copyright file="ProducerConsumer.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;

    public sealed class ProducerConsumer<T> : IDisposable
    {
        public delegate void RunDelegate(T obj);

        private readonly RunDelegate runFunction;
        private readonly int maxThreads;
        private readonly SemaphoreSlim queueFullSemaphore;
        private readonly Queue<T> pendingRequests = new Queue<T>();
        private ReaderWriterLockSlim pendingRequestsLock = new ReaderWriterLockSlim();
        private int numStarted;

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Semaphore is stored in field")]
        public ProducerConsumer(RunDelegate run, int maxThreads, int maxQueueLength = int.MaxValue)
        {
            this.runFunction = run;
            this.maxThreads = maxThreads;
            this.MaxQueueLength = maxQueueLength;
            this.queueFullSemaphore = maxQueueLength != int.MaxValue
                ? new SemaphoreSlim(maxQueueLength, maxQueueLength)
                : null;
        }

        public int MaxQueueLength { get; }

        public uint MaxSpins { get; set; } = 50;

        public void EnqueueBatch(IList<T> calls)
        {
            if (calls == null)
            {
                throw new ArgumentNullException(nameof(calls));
            }

            // enqueue and spin a thread if needed
            bool launch = false;

            this.pendingRequestsLock.EnterWriteLock();
            try
            {
                if (this.queueFullSemaphore != null)
                {
                    for (int i = 0; i < calls.Count; i++)
                    {
                        this.pendingRequestsLock.ExitWriteLock();
                        this.queueFullSemaphore.Wait();
                        this.pendingRequestsLock.EnterWriteLock();
                    }
                }

                foreach (T call in calls)
                {
                    this.pendingRequests.Enqueue(call);
                }

                if (this.numStarted < this.maxThreads)
                {
                    this.numStarted++;
                    launch = true;
                }
            }
            finally
            {
                this.pendingRequestsLock.ExitWriteLock();
            }

            if (launch)
            {
                this.LaunchConsumer();
            }
        }

        public void Enqueue(T call)
        {
            // enqueue and spin a thread if needed
            bool launch = false;

            this.pendingRequestsLock.EnterWriteLock();
            try
            {
                if (this.queueFullSemaphore != null)
                {
                    this.pendingRequestsLock.ExitWriteLock();
                    this.queueFullSemaphore.Wait();
                    this.pendingRequestsLock.EnterWriteLock();
                }

                this.pendingRequests.Enqueue(call);

                if (this.numStarted < this.maxThreads)
                {
                    this.numStarted++;
                    launch = true;
                }
            }
            finally
            {
                this.pendingRequestsLock.ExitWriteLock();
            }

            if (launch)
            {
                this.LaunchConsumer();
            }
        }

        private void LaunchConsumer()
        {
            RingMasterThreadPool.Instance.QueueUserWorkItem(this.DoConsumer);
        }

        private void DoConsumer(object ign)
        {
            this.pendingRequestsLock.EnterWriteLock();
            while (true)
            {
                ulong spins = 0;
                while (this.pendingRequests.Count == 0 && spins < this.MaxSpins)
                {
                    this.pendingRequestsLock.ExitWriteLock();
                    Thread.Yield();
                    this.pendingRequestsLock.EnterWriteLock();
                    spins++;
                }

                if (this.pendingRequests.Count == 0)
                {
                    this.pendingRequestsLock.ExitWriteLock();
                    Interlocked.Decrement(ref this.numStarted);
                    return;
                }

                T r = this.pendingRequests.Dequeue();

                this.queueFullSemaphore?.Release();

                this.pendingRequestsLock.ExitWriteLock();

                this.runFunction(r);

                this.pendingRequestsLock.EnterWriteLock();
            }
        }

        public void Clear()
        {
            this.pendingRequestsLock.EnterWriteLock();
            try
            {
                this.pendingRequests.Clear();
            }
            finally
            {
                this.pendingRequestsLock.ExitWriteLock();
            }
        }

        public bool IsEmpty => this.pendingRequests.Count == 0;

        public int Inflight => this.numStarted;

        public void Dispose()
        {
            this.pendingRequestsLock?.Dispose();
            this.pendingRequestsLock = null;
        }
    }
}