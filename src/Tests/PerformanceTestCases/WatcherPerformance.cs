// <copyright file="WatcherPerformance.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Performance
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    public class WatcherPerformance : IDisposable
    {
        private readonly CancellationToken cancellationToken;
        private readonly IInstrumentation instrumentation;
        private readonly RandomGenerator random = new RandomGenerator();
        private readonly List<string> nodeList = new List<string>();

        public WatcherPerformance(IInstrumentation instrumentation, CancellationToken cancellationToken)
        {
            this.instrumentation = instrumentation;
            this.cancellationToken = cancellationToken;
        }

        public interface IInstrumentation
        {
            void NodeLoaded(int nodeCount);

            void SetWatcherSucceeded(TimeSpan latency);

            void SetWatcherFailed();

            void WatcherNotified(TimeSpan watchDuration);
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

        public void SetWatchers(IRingMasterRequestHandler ringMaster, int maxWatchers)
        {
            if (ringMaster == null)
            {
                throw new ArgumentNullException(nameof(ringMaster));
            }

            // Semaphore that limits how many simultaneous active watchers are allowed.
            using (var watcherSemaphore = new SemaphoreSlim(maxWatchers, maxWatchers))
            {
                long lastAssignedUniqueId = 0;
                var random = new RandomGenerator();
                Trace.TraceInformation($"Set watchers");

                while (!this.cancellationToken.IsCancellationRequested)
                {
                    var index = random.GetRandomInt(0, this.nodeList.Count);
                    var nodePath = this.nodeList[index];

                    ulong id = (ulong)Interlocked.Increment(ref lastAssignedUniqueId);
                    var watcher = new Watcher(id, WatcherKind.OneUse, this.instrumentation, watcherSemaphore);

                    watcherSemaphore.Wait(this.cancellationToken);
                    var existsRequest = new RequestExists(nodePath, watcher, id);
                    var timer = Stopwatch.StartNew();

                    ringMaster.Request(existsRequest).ContinueWith(responseTask =>
                    {
                        try
                        {
                            timer.Stop();
                            RequestResponse response = responseTask.Result;
                            if (response.ResultCode == (int)RingMasterException.Code.Ok)
                            {
                                this.instrumentation?.SetWatcherSucceeded(timer.Elapsed);
                            }
                            else
                            {
                                this.instrumentation?.SetWatcherFailed();
                            }
                        }
                        catch (Exception)
                        {
                            this.instrumentation?.SetWatcherFailed();
                        }
                    });
                }
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
            }
        }

        /// <summary>
        /// An implementation of the <see cref="IWatcher"/> interface that is used
        /// in the performance test.
        /// </summary>
        private sealed class Watcher : IWatcher
        {
            private readonly WatcherPerformance.IInstrumentation instrumentation;
            private readonly SemaphoreSlim watcherSemaphore;
            private readonly Stopwatch lifetime;

            public Watcher(ulong id, WatcherKind kind, WatcherPerformance.IInstrumentation instrumentation, SemaphoreSlim watcherSemaphore)
            {
                this.Id = id;
                this.Kind = kind;
                this.instrumentation = instrumentation;
                this.watcherSemaphore = watcherSemaphore;
                this.lifetime = Stopwatch.StartNew();
            }

            public ulong Id { get; private set; }

            public bool OneUse => this.Kind.HasFlag(WatcherKind.OneUse);

            public WatcherKind Kind { get; private set; }

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
                this.watcherSemaphore.Release();
                this.lifetime.Stop();
                this.instrumentation?.WatcherNotified(this.lifetime.Elapsed);
            }
        }
    }
}
