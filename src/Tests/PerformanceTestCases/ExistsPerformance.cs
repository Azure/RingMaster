// <copyright file="ExistsPerformance.cs" company="Microsoft">
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

    public class ExistsPerformance : IDisposable
    {
        private readonly CancellationToken cancellationToken;
        private readonly IInstrumentation instrumentation;
        private readonly SemaphoreSlim semaphore;
        private readonly RandomGenerator random = new RandomGenerator();
        private readonly List<string> nodeList = new List<string>();

        public ExistsPerformance(IInstrumentation instrumentation, int maxConcurrentRequests, CancellationToken cancellationToken)
        {
            this.instrumentation = instrumentation;
            this.semaphore = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
            this.cancellationToken = cancellationToken;
        }

        public interface IInstrumentation
        {
            void NodeLoaded(int nodeCount);

            void BatchProcessed(TimeSpan latency, int batchLength, int successCount, int failureCount);

            void BatchFailed(int batchLength);
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
                Trace.TraceInformation($"LoadNode {this.nodeList.Count}: {currentNode}");
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

        public void QueueBatches(IRingMasterRequestHandler ringMaster, int batchLength)
        {
            if (ringMaster == null)
            {
                throw new ArgumentNullException(nameof(ringMaster));
            }

            ulong batchId = 0;
            var random = new RandomGenerator();
            Trace.TraceInformation($"Queue Exists Batches");

            while (!this.cancellationToken.IsCancellationRequested)
            {
                var operations = new Op[batchLength];
                for (int i = 0; i < batchLength; i++)
                {
                    var index = random.GetRandomInt(0, this.nodeList.Count);
                    var nodePath = this.nodeList[index];

                    operations[i] = Op.Check(nodePath, -1);
                }

                var batchRequest = new RequestBatch(operations, completeSynchronously: false, uid: batchId++);
                this.semaphore.Wait();
                var timer = Stopwatch.StartNew();

                ringMaster.Request(batchRequest).ContinueWith(responseTask =>
                {
                    try
                    {
                        this.semaphore.Release();
                        timer.Stop();
                        int successCount = 0;
                        int failureCount = 0;
                        RequestResponse response = responseTask.Result;
                        if (response.ResultCode == (int)RingMasterException.Code.Ok)
                        {
                            var results = (IReadOnlyList<OpResult>)response.Content;
                            if (results != null)
                            {
                                foreach (var result in results)
                                {
                                    if (result.ErrCode == RingMasterException.Code.Ok)
                                    {
                                        successCount++;
                                    }
                                    else
                                    {
                                        failureCount++;
                                    }
                                }
                            }

                            this.instrumentation?.BatchProcessed(timer.Elapsed, batchRequest.Requests.Count, successCount, failureCount);
                        }
                        else
                        {
                            this.instrumentation?.BatchFailed(batchRequest.Requests.Count);
                        }
                    }
                    catch (Exception)
                    {
                        this.instrumentation?.BatchFailed(batchRequest.Requests.Count);
                    }
                });
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
                this.semaphore.Dispose();
            }
        }
    }
}
