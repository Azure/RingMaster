// <copyright file="BulkWatcherPerformance.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Performance
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    public class BulkWatcherPerformance : IDisposable
    {
        private readonly CancellationToken cancellationToken;
        private readonly IInstrumentation instrumentation;
        private readonly RandomGenerator random = new RandomGenerator();
        private readonly List<string> nodeList = new List<string>();
        private readonly SemaphoreSlim semaphore;

        public BulkWatcherPerformance(IInstrumentation instrumentation, int maxConcurrentRequests, CancellationToken cancellationToken)
        {
            this.instrumentation = instrumentation;
            this.cancellationToken = cancellationToken;
            this.semaphore = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
        }

        public interface IInstrumentation
        {
            void NodeLoaded(int nodeCount);

            void SetWatcherSucceeded(TimeSpan latency);

            void SetWatcherFailed();

            void WatcherNotified(string notificationType, TimeSpan watchDuration);
        }

        public async Task LoadNodes(IRingMasterRequestHandler ringMaster, string rootPath, int maxNodes, int maxGetChildrenEnumerationCount = 1000)
        {
            Trace.TraceInformation($"LoadNodes rootPath={rootPath}, maxNodes={maxNodes}, maxGetChildrenEnumerationCount={maxGetChildrenEnumerationCount}");
            var nodes = new Queue<string>();

            nodes.Enqueue(rootPath);

            while (!this.cancellationToken.IsCancellationRequested && (nodes.Count > 0) && (this.nodeList.Count < maxNodes))
            {
                string currentNode = nodes.Dequeue();

                this.nodeList.Add(currentNode);
                this.instrumentation?.NodeLoaded(this.nodeList.Count);

                try
                {
                    await ringMaster.ForEachChild(currentNode, maxGetChildrenEnumerationCount, child =>
                    {
                        string childFullPath = (currentNode == "/") ? $"/{child}" : $"{currentNode}/{child}";
                        nodes.Enqueue(childFullPath);
                    });
                }
                catch (RingMasterException ex)
                {
                    Trace.TraceError($"Failed to get children of node {currentNode}. Exception={ex}");
                }
            }

            Trace.TraceInformation($"LoadNodes Completed: {this.nodeList.Count} nodes loaded");
        }

        public void SetWatchers(IRingMasterRequestHandler ringMaster)
        {
            if (ringMaster == null)
            {
                throw new ArgumentNullException(nameof(ringMaster));
            }

            long lastAssignedUniqueId = 0;
            var random = new RandomGenerator();
            Trace.TraceInformation($"Set watchers");

            using (var operationsCompletedEvent = new CountdownEvent(1))
            {
                while (!this.cancellationToken.IsCancellationRequested)
                {
                    var index = random.GetRandomInt(0, this.nodeList.Count);
                    var nodePath = this.nodeList[index];
                    this.instrumentation?.NodeLoaded(this.nodeList.Count);

                    ulong id = (ulong)Interlocked.Increment(ref lastAssignedUniqueId);
                    this.semaphore.Wait();
                    Task _ = this.IssueRequest(ringMaster, id, nodePath, operationsCompletedEvent);
                }

                operationsCompletedEvent.Signal();
                operationsCompletedEvent.Wait(this.cancellationToken);
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private async Task IssueRequest(IRingMasterRequestHandler ringMaster, ulong id, string nodePath, CountdownEvent operationsCompletedEvent)
        {
            try
            {
                string bulkWatcherPath = $"bulkwatcher:{nodePath}";
                operationsCompletedEvent.AddCount();
                var timer = Stopwatch.StartNew();
                var watcher = new Watcher(id, this.instrumentation);

                var installBulkWatcherRequest = new RequestExists(bulkWatcherPath, watcher);

                RequestResponse response = await ringMaster.Request(installBulkWatcherRequest);

                timer.Stop();
                if (response.ResultCode == (int)RingMasterException.Code.Ok)
                {
                    this.instrumentation?.SetWatcherSucceeded(timer.Elapsed);
                }
                else
                {
                    this.instrumentation?.SetWatcherFailed();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"SetBulkWatcher failed id={id}, path={nodePath}, exception={ex.ToString()}");
                this.instrumentation?.SetWatcherFailed();
            }
            finally
            {
                this.semaphore.Release();
                operationsCompletedEvent.Signal();
            }
        }

        private void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                this.semaphore.Dispose();
            }
        }

        /// <summary>
        /// An implementation of the <see cref="IWatcher"/> interface that is used
        /// in the performance test.
        /// </summary>
        private sealed class Watcher : IWatcher
        {
            private readonly BulkWatcherPerformance.IInstrumentation instrumentation;
            private readonly Stopwatch lifetime;

            public Watcher(ulong id, BulkWatcherPerformance.IInstrumentation instrumentation)
            {
                this.Id = id;
                this.OneUse = false;
                this.instrumentation = instrumentation;
                this.lifetime = Stopwatch.StartNew();
            }

            public ulong Id { get; private set; }

            public bool OneUse { get; private set; }

            /// <summary>
            /// Process a watcher notification.
            /// </summary>
            /// <param name="watchedEvent">Information about the watcher notification</param>
            /// <remarks>
            /// Releases the watcher semaphore when a notification is received.  This will allow
            /// the test to set more watchers.
            /// </remarks>
            public void Process(WatchedEvent watchedEvent)
            {
                if (watchedEvent == null)
                {
                    throw new ArgumentNullException(nameof(watchedEvent));
                }

                this.lifetime.Stop();
                this.instrumentation?.WatcherNotified(Enum.GetName(typeof(WatchedEvent.WatchedEventType), watchedEvent.EventType), this.lifetime.Elapsed);
            }
        }
    }
}
