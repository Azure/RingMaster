// <copyright file="SetDataPerformance.cs" company="Microsoft">
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

    public class SetDataPerformance : IDisposable
    {
        private readonly List<string> nodeList = new List<string>();
        private readonly CancellationToken cancellationToken;
        private readonly IInstrumentation instrumentation;
        private readonly SemaphoreSlim semaphore;

        public SetDataPerformance(IInstrumentation instrumentation, int maxConcurrentRequests, CancellationToken cancellationToken)
        {
            this.instrumentation = instrumentation;
            this.semaphore = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
            this.cancellationToken = cancellationToken;
        }

        public interface IInstrumentation
        {
            void NodeLoaded(int nodeCount);

            void SetDataMultiSucceeded(int successCount, TimeSpan latency);

            void SetDataMultiFailed(int failureCount);
        }

        /// <summary>
        /// Gets or sets the minimum size of data that will be associated with each node.
        /// </summary>
        public int MinDataSizePerNode { get; set; } = 0;

        /// <summary>
        /// Gets or sets the maximum size of data that will be associated with each node.
        /// </summary>
        public int MaxDataSizePerNode { get; set; } = 512;

        /// <summary>
        /// Load the nodes that will be used for this test.
        /// </summary>
        /// <param name="ringMaster">RingMaster client</param>
        /// <param name="rootPath">Root path to the nodes</param>
        /// <param name="maxNodes">Maximum number of nodes to load</param>
        /// <param name="maxGetChildrenEnumerationCount">Maximum number of children to enumerate per get children request</param>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
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

        /// <summary>
        /// Queue SetData requests.
        /// </summary>
        /// <param name="ringMaster">RingMaster client</param>
        /// <param name="batchLength">Number create requests per multi</param>
        /// <param name="maxOperations">Maximum number of operations</param>
        public void QueueRequests(IRingMasterRequestHandler ringMaster, int batchLength, long maxOperations = long.MaxValue)
        {
            if (ringMaster == null)
            {
                throw new ArgumentNullException(nameof(ringMaster));
            }

            string[] nodePaths = this.nodeList.ToArray();
            Trace.TraceInformation($"SetDataPerformance.QueueRequests: nodePathsLength={nodePaths.Length}, batchLength={batchLength}");

            var random = new RandomGenerator();
            var nodeData = new byte[this.MaxDataSizePerNode];

            using (var operationsCompletedEvent = new CountdownEvent(1))
            {
                var operations = new List<Op>();
                while (!this.cancellationToken.IsCancellationRequested && maxOperations > 0)
                {
                    var index = random.GetRandomInt(0, nodePaths.Length);
                    var nodePath = nodePaths[index];
                    this.instrumentation?.NodeLoaded(nodePaths.Length);

                    if (batchLength == 0)
                    {
                        this.IssueRequest(ringMaster, nodePath, nodeData, operationsCompletedEvent);
                    }
                    else
                    {
                        operations.Add(Op.SetData(nodePath, nodeData, version: -1));
                        if (operations.Count >= batchLength)
                        {
                            this.IssueMultiRequest(ringMaster, operations, operationsCompletedEvent);
                            operations.Clear();
                        }
                    }

                    maxOperations--;
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

        private void IssueRequest(IRingMasterRequestHandler ringMaster, string nodePath, byte[] nodeData, CountdownEvent operationsCompletedEvent)
        {
            var setDataRequest = new RequestSetData(nodePath, nodeData, -1);
            this.semaphore.Wait();

            operationsCompletedEvent.AddCount();
            var timer = Stopwatch.StartNew();
            ringMaster.Request(setDataRequest).ContinueWith(responseTask =>
            {
                try
                {
                    this.semaphore.Release();
                    timer.Stop();

                    RequestResponse response = responseTask.Result;
                    if (response.ResultCode == (int)RingMasterException.Code.Ok)
                    {
                        this.instrumentation?.SetDataMultiSucceeded(1, timer.Elapsed);
                    }
                    else
                    {
                        this.instrumentation?.SetDataMultiFailed(1);
                    }
                }
                catch (Exception)
                {
                    this.instrumentation?.SetDataMultiFailed(1);
                }
                finally
                {
                    operationsCompletedEvent.Signal();
                }
            });
        }

        private void IssueMultiRequest(IRingMasterRequestHandler ringMaster, IReadOnlyList<Op> operations, CountdownEvent operationsCompletedEvent)
        {
            var multiRequest = new RequestMulti(operations, completeSynchronously: false, uid: 0);
            this.semaphore.Wait();

            int operationsCount = operations.Count;
            operationsCompletedEvent.AddCount(operationsCount);
            var timer = Stopwatch.StartNew();
            ringMaster.Request(multiRequest).ContinueWith(responseTask =>
            {
                try
                {
                    this.semaphore.Release();
                    timer.Stop();

                    RequestResponse response = responseTask.Result;
                    if (response.ResultCode == (int)RingMasterException.Code.Ok)
                    {
                        var results = (IReadOnlyList<OpResult>)response.Content;
                        this.instrumentation?.SetDataMultiSucceeded(results.Count, timer.Elapsed);
                    }
                    else
                    {
                        this.instrumentation?.SetDataMultiFailed(operationsCount);
                    }
                }
                catch (Exception)
                {
                    this.instrumentation?.SetDataMultiFailed(operationsCount);
                }
                finally
                {
                    operationsCompletedEvent.Signal(operationsCount);
                }
            });
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
