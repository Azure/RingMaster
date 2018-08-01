// <copyright file="RingMasterRequestExecutor.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// Provides an arena with dedicated threads where RingMaster requests are executed.
    /// </summary>
    public sealed class RingMasterRequestExecutor : IRingMasterRequestExecutor, IDisposable
    {
        private static readonly Action<RequestResponse, Exception> NoAction = (response, ex) => { };

        private readonly CancellationToken parentCancellationToken;
        private readonly RingMasterBackendCore backend;
        private readonly Configuration configuration;
        private readonly IInstrumentation instrumentation;

        private CancellationTokenSource cancellationSource;
        private BlockingCollection<PendingRequest> requestQueue = new BlockingCollection<PendingRequest>();
        private List<Thread> requestProcessingThreads = new List<Thread>();
        private long lastAssignedSequenceNumber = 0;
        private int activeExecutions = 0;
        private int availableThreads = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterRequestExecutor"/> class.
        /// </summary>
        /// <param name="backend">RingMaster backend</param>
        /// <param name="configuration">Configuration settings</param>
        /// <param name="instrumentation">Instrumentation consumer</param>
        /// <param name="cancellationToken">Token to be observed for cancellation signal</param>
        public RingMasterRequestExecutor(RingMasterBackendCore backend, Configuration configuration, IInstrumentation instrumentation, CancellationToken cancellationToken)
        {
            this.backend = backend ?? throw new ArgumentNullException(nameof(backend));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.instrumentation = instrumentation;
            this.parentCancellationToken = cancellationToken;
        }

        /// <summary>
        /// Instrumentation signals provided by <see cref="RingMasterRequestExecutor"/>.
        /// </summary>
        public interface IInstrumentation
        {
            /// <summary>
            /// A request was scheduled for execution.
            /// </summary>
            /// <param name="queueLength">Current length of the queue</param>
            /// <param name="queueCapacity">Capacity of the queue</param>
            void OnExecutionScheduled(int queueLength, int queueCapacity);

            /// <summary>
            /// An request was not added to the queue because the queue was full.
            /// </summary>
            /// <param name="queueLength">Current length of the queue</param>
            /// <param name="queueCapacity">Capacity of the queue</param>
            void OnQueueOverflow(int queueLength, int queueCapacity);

            /// <summary>
            /// Execution of a request started.
            /// </summary>
            /// <param name="currentlyActiveCount">Number of request currently being executed</param>
            /// <param name="availableThreadCount">Number of threads available</param>
            /// <param name="elapsedInQueue">Amount of time that the request spent waiting in the queue</param>
            void OnExecutionStarted(int currentlyActiveCount, int availableThreadCount, TimeSpan elapsedInQueue);

            /// <summary>
            /// Execution of an operation was completed.
            /// </summary>
            /// <param name="elapsed">Time taken to complete the operation</param>
            /// <param name="currentlyActiveCount">Number of request currently being executed</param>
            void OnExecutionCompleted(TimeSpan elapsed, int currentlyActiveCount);

            /// <summary>
            /// Execution of an operation was cancelled.
            /// </summary>
            void OnExecutionCancelled();

            /// <summary>
            /// A request was not executed because it timed out while in the queue.
            /// </summary>
            /// <param name="elapsed">Time spent by the request in the queue</param>
            void OnExecutionTimedout(TimeSpan elapsed);

            /// <summary>
            /// Execution of a request failed.
            /// </summary>
            void OnExecutionFailed();
        }

        /// <summary>
        /// Start the executor.
        /// </summary>
        /// <param name="threadCount">Number of threads</param>
        public void Start(int threadCount)
        {
            RingMasterEventSource.Log.Executor_Start(threadCount);

            if (this.requestProcessingThreads.Count > 0)
            {
                throw new InvalidOperationException("RingMasterRequestExecutor has already started");
            }

            this.cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(this.parentCancellationToken);

            for (int i = 0; i < threadCount; i++)
            {
                this.requestProcessingThreads.Add(new Thread(this.ProcessPendingRequests));
            }

            foreach (var thread in this.requestProcessingThreads)
            {
                thread.Start();
            }
        }

        /// <summary>
        /// Processes a session initialization request.
        /// </summary>
        /// <param name="call">The initialization request</param>
        /// <param name="session">The session to be initialized</param>
        /// <returns>Response for the initialization request</returns>
        public RequestResponse ProcessSessionInitialization(RequestCall call, ClientSession session)
        {
            return this.backend.ProcessSessionInitialization(call, session);
        }

        /// <summary>
        /// Process the given request.
        /// </summary>
        /// <param name="request">Request that must be processed</param>
        /// <param name="session">Client session associtated with the request</param>
        /// <param name="onCompletion">Action that must be invoked when the request is completed</param>
        public void ProcessMessage(
            IRingMasterBackendRequest request,
            ClientSession session,
            Action<RequestResponse, Exception> onCompletion)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            PendingRequest pendingRequest = new PendingRequest
            {
                SequenceNumber = Interlocked.Increment(ref this.lastAssignedSequenceNumber),
                Lifetime = Stopwatch.StartNew(),
                Request = request,
                Session = session,
                OnCompletion = onCompletion ?? NoAction,
            };

            if (this.requestQueue.TryAdd(pendingRequest))
            {
                RingMasterEventSource.Log.Executor_RequestQueued(pendingRequest.SequenceNumber, session.SessionId, request.Uid);
                this.instrumentation?.OnExecutionScheduled(this.requestQueue.Count, this.requestQueue.BoundedCapacity);
            }
            else
            {
                RingMasterEventSource.Log.Executor_RequestQueueOverflow(pendingRequest.SequenceNumber, session.SessionId, request.Uid);
                pendingRequest.OnCompletion(
                    new RequestResponse
                    {
                        ResponsePath = request.Path,
                        ResultCode = (int)RingMasterException.Code.ServerOperationTimeout,
                    },
                    null);
                this.instrumentation?.OnQueueOverflow(this.requestQueue.Count, this.requestQueue.BoundedCapacity);
            }
        }

        /// <summary>
        /// Stop the executor.
        /// </summary>
        public void Stop()
        {
            // This will be called twice. Check to avoid the null reference in the second stop.
            if (this.requestProcessingThreads != null &&
                this.cancellationSource != null &&
                !this.cancellationSource.IsCancellationRequested)
            {
                RingMasterEventSource.Log.Executor_Stopping(this.requestProcessingThreads.Count);
                this.cancellationSource.Cancel();
                this.requestQueue.CompleteAdding();
                foreach (var thread in this.requestProcessingThreads)
                {
                    thread.Join();
                }

                this.requestProcessingThreads.Clear();
                this.requestQueue.Dispose();

                this.cancellationSource = null;
                this.requestQueue = null;
                this.requestProcessingThreads = null;
            }

            RingMasterEventSource.Log.Executor_Stopped();
        }

        /// <summary>
        /// Dispose this instance.
        /// </summary>
        public void Dispose()
        {
            this.Stop();
        }

        private void ProcessPendingRequests()
        {
            RingMasterEventSource.Log.Executor_ProcessPendingRequestsThreadStarted(Thread.CurrentThread.ManagedThreadId);
            try
            {
                int threadCount = Interlocked.Increment(ref this.availableThreads);
                foreach (PendingRequest pendingRequest in this.requestQueue.GetConsumingEnumerable(this.cancellationSource.Token))
                {
                    RequestResponse response = new RequestResponse { ResponsePath = pendingRequest.Request.Path };
                    ulong sessionId = pendingRequest.Session.SessionId;
                    ulong requestId = pendingRequest.Request.Uid;

                    var timer = Stopwatch.StartNew();
                    int currentlyActiveExecutions = Interlocked.Increment(ref this.activeExecutions);
                    try
                    {
                        this.instrumentation?.OnExecutionStarted(currentlyActiveExecutions, threadCount, pendingRequest.Lifetime.Elapsed);

                        if (this.cancellationSource.IsCancellationRequested)
                        {
                            RingMasterEventSource.Log.Executor_ProcessRequestCancelled(pendingRequest.SequenceNumber);
                            response.ResultCode = (int)RingMasterException.Code.OperationCancelled;
                            pendingRequest.OnCompletion(response, null);
                            this.instrumentation?.OnExecutionCancelled();
                        }
                        else if (pendingRequest.Lifetime.Elapsed > this.configuration.DefaultRequestTimeout)
                        {
                            RingMasterEventSource.Log.Executor_ProcessRequestTimedout(pendingRequest.SequenceNumber);
                            response.ResultCode = (int)RingMasterException.Code.ServerOperationTimeout;
                            pendingRequest.OnCompletion(response, null);
                            this.instrumentation?.OnExecutionTimedout(pendingRequest.Lifetime.Elapsed);
                        }
                        else
                        {
                            this.backend.ProcessMessage(pendingRequest.Request, pendingRequest.Session, pendingRequest.OnCompletion);
                            RingMasterEventSource.Log.Executor_ProcessRequestCompleted(pendingRequest.SequenceNumber, timer.ElapsedMilliseconds);
                        }
                    }
                    catch (Exception ex)
                    {
                        RingMasterEventSource.Log.Executor_ProcessRequestFailed(pendingRequest.SequenceNumber, ex.ToString());
                        this.instrumentation?.OnExecutionFailed();
                    }
                    finally
                    {
                        currentlyActiveExecutions = Interlocked.Decrement(ref this.activeExecutions);
                        this.instrumentation?.OnExecutionCompleted(timer.Elapsed, currentlyActiveExecutions);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Cancellation token is trigged. Ignore this and log the thread stop event in finally clause.
            }
            catch (Exception ex)
            {
                RingMasterEventSource.Log.Executor_ProcessPendingRequestsThreadFailed(Thread.CurrentThread.ManagedThreadId, ex.ToString());
            }
            finally
            {
                Interlocked.Decrement(ref this.availableThreads);
                RingMasterEventSource.Log.Executor_ProcessPendingRequestsThreadStopped(Thread.CurrentThread.ManagedThreadId);
            }
        }

        private struct PendingRequest
        {
            public long SequenceNumber;
            public Stopwatch Lifetime;
            public IRingMasterBackendRequest Request;
            public ClientSession Session;
            public Action<RequestResponse, Exception> OnCompletion;
        }

        /// <summary>
        /// Configuration of the request executor
        /// </summary>
        public sealed class Configuration
        {
            /// <summary>
            /// Gets or sets the maximum number of requests that can be waiting in the queue.
            /// </summary>
            public int QueueLength { get; set; } = 100;

            /// <summary>
            /// Gets or sets the default request timeout value.
            /// </summary>
            public TimeSpan DefaultRequestTimeout { get; set; } = TimeSpan.FromMilliseconds(2500);
        }
    }
}
