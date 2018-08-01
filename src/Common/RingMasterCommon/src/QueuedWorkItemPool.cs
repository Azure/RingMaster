// <copyright file="QueuedWorkItemPool.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;

    /// <summary>
    /// A simple FIFO work item scheduling pool for running a lot of <see cref="Action"/> in dedicated threads. The
    /// intention is to isolate them from normal work items in the .NET thread pool and async tasks.
    /// </summary>
    public sealed class QueuedWorkItemPool
    {
        /// <summary>
        /// Work items to be invoked in order
        /// </summary>
        private readonly ConcurrentQueue<Action> workItems = new ConcurrentQueue<Action>();

        /// <summary>
        /// List of available threads to run the work item.
        /// </summary>
        private readonly ConcurrentStack<ThreadWorker> workers = new ConcurrentStack<ThreadWorker>();

        /// <summary>
        /// Gets the default instance of the class
        /// </summary>
        public static QueuedWorkItemPool Default { get; } = new QueuedWorkItemPool();

        /// <summary>
        /// Initializes the <see cref="QueuedWorkItemPool"/> class and starts all worker threads
        /// </summary>
        /// <param name="threadCount">Number of worker threads</param>
        /// <param name="cancellation">Cancellation token for the worker threads</param>
        public void Initialize(int threadCount, CancellationToken cancellation)
        {
            if (threadCount <= 0)
            {
                throw new ArgumentException("threadCount must be greater than 0", nameof(threadCount));
            }

            for (int i = 0; i < threadCount; i++)
            {
                this.workers.Push(new ThreadWorker(this, cancellation));
            }
        }

        /// <summary>
        /// Queues the specified work item for future execution
        /// </summary>
        /// <param name="workitem">work item to be queued</param>
        public void Queue(Action workitem)
        {
            this.workItems.Enqueue(workitem);
            this.TryScheduleNextWorkItem();
        }

        /// <summary>
        /// Returns the worker to the pool (stack) and tries to schedule the next work item
        /// </summary>
        /// <param name="worker">Worker to be marked as free</param>
        private void FreeThreadWorker(ThreadWorker worker)
        {
            this.workers.Push(worker);
            this.TryScheduleNextWorkItem();
        }

        /// <summary>
        /// Tries to schedule the next available work item on the next available worker
        /// </summary>
        private void TryScheduleNextWorkItem()
        {
            if (this.workers.TryPop(out var worker))
            {
                // worker is available, now check if there is any work item
                if (this.workItems.TryDequeue(out var workItem))
                {
                    worker.WorkItem = workItem;
                    worker.Ready.Set();
                }
                else
                {
                    // return the worker and let it go
                    this.workers.Push(worker);
                }
            }
        }

        /// <summary>
        /// Worker thread for invoking work items being scheduled
        /// </summary>
        private sealed class ThreadWorker
        {
            private readonly QueuedWorkItemPool pool;
            private readonly CancellationToken cancellation;
            private readonly Thread thread;

            /// <summary>
            /// Initializes a new instance of the <see cref="ThreadWorker"/> class and starts the thread
            /// </summary>
            /// <param name="pool">Pool object which this worker is associated with</param>
            /// <param name="cancellation">Cancellation token to stop the thread</param>
            public ThreadWorker(QueuedWorkItemPool pool, CancellationToken cancellation)
            {
                this.pool = pool;
                this.cancellation = cancellation;
                this.thread = new Thread(this.ThreadFunc);
                this.thread.IsBackground = true;
                this.thread.Start();
            }

            /// <summary>
            /// Gets or sets the work item to be invoked
            /// </summary>
            public Action WorkItem { get; set; }

            /// <summary>
            /// Gets the event to signal the thread to invoke the work item
            /// </summary>
            public AutoResetEvent Ready { get; } = new AutoResetEvent(false);

            private void ThreadFunc()
            {
                while (!this.cancellation.IsCancellationRequested)
                {
                    if (!this.Ready.WaitOne(1000))
                    {
                        continue;
                    }

                    try
                    {
                        this.WorkItem?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceInformation($"Work item in ThreadWorker failed: {ex.Message}");
                    }

                    // Only run the work item once. Dereference it after invocation is completed.
                    this.WorkItem = null;

                    // Trigger the next one.
                    this.pool.FreeThreadWorker(this);
                }
            }
        }
    }
}
