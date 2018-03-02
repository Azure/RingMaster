// <copyright file="DeletePerformance.cs" company="Microsoft">
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

    public class DeletePerformance : IDisposable
    {
        private readonly List<string> nodeList = new List<string>();
        private readonly CancellationToken cancellationToken;
        private readonly IInstrumentation instrumentation;
        private readonly SemaphoreSlim semaphore;

        public DeletePerformance(IInstrumentation instrumentation, int maxConcurrentRequests, CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;
            this.instrumentation = instrumentation;
            this.semaphore = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
        }

        public interface IInstrumentation
        {
            void NodeLoaded(int nodeCount);

            void NodeQueuedForDelete(int nodeCount);

            void DeleteMultiSucceeded(int successCount, TimeSpan latency);

            void DeleteMultiFailed(int failureCount);
        }

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

            while ((nodes.Count > 0) && (this.nodeList.Count < maxNodes) && !this.cancellationToken.IsCancellationRequested)
            {
                string currentNode = nodes.Dequeue();
                this.instrumentation?.NodeLoaded(this.nodeList.Count);
                this.nodeList.Add(currentNode);

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

            this.nodeList.Reverse();
            Trace.TraceInformation($"LoadNodes Completed: {this.nodeList.Count} nodes loaded");
        }

        /// <summary>
        /// Queue Delete requests.
        /// </summary>
        /// <param name="ringMaster">RingMaster client</param>
        /// <param name="batchLength">Number of deletes per multi</param>
        public void QueueDeletes(IRingMasterRequestHandler ringMaster, int batchLength)
        {
            if (ringMaster == null)
            {
                throw new ArgumentNullException(nameof(ringMaster));
            }

            string[] nodePaths = this.nodeList.ToArray();
            Trace.TraceInformation($"DeletePerformance.QueueDeletes: nodePathsLength={nodePaths.Length}, batchLength={batchLength}");

            using (var operationsCompletedEvent = new CountdownEvent(1))
            {
                int nodeCount = 0;
                var operations = new List<Op>();
                while (!this.cancellationToken.IsCancellationRequested && nodeCount < nodePaths.Length)
                {
                    string nodePath = nodePaths[nodeCount];
                    operations.Add(Op.Delete(nodePath, version: -1, recursive: true));
                    nodeCount++;
                    this.instrumentation?.NodeQueuedForDelete(nodeCount);
                    if (operations.Count >= batchLength)
                    {
                        this.IssueMultiRequest(ringMaster, operations, operationsCompletedEvent);
                        operations.Clear();
                    }
                }

                if (operations.Count > 0)
                {
                    this.IssueMultiRequest(ringMaster, operations, operationsCompletedEvent);
                }

                operationsCompletedEvent.Signal();
                operationsCompletedEvent.Wait(this.cancellationToken);
            }
        }

        /// <summary>
        /// Recursively delete nodes under the given path
        /// </summary>
        /// <param name="ringMaster">RingMaster client</param>
        /// <param name="rootPath">Root path to the nodes</param>
        public void CascadeDelete(IRingMasterRequestHandler ringMaster, string rootPath)
        {
            if (ringMaster == null)
            {
                throw new ArgumentNullException(nameof(ringMaster));
            }

            using (var operationsCompletedEvent = new CountdownEvent(1))
            {
                this.instrumentation?.NodeQueuedForDelete(this.nodeList.Count);
                var operations = new Op[1];
                operations[0] = Op.Delete(rootPath, version: -1, deletemode: DeleteMode.FastDelete | DeleteMode.CascadeDelete);
                this.IssueMultiRequest(ringMaster, operations, operationsCompletedEvent);

                operationsCompletedEvent.Signal();
                operationsCompletedEvent.Wait();
            }
        }

        /// <summary>
        /// Schedule delete for nodes under the given path
        /// </summary>
        /// <param name="ringMaster">RingMaster client</param>
        /// <param name="rootPath">Root path to the nodes</param>
        public void ScheduledDelete(IRingMasterRequestHandler ringMaster, string rootPath)
        {
            if (ringMaster == null)
            {
                throw new ArgumentNullException(nameof(ringMaster));
            }

            using (var operationsCompletedEvent = new CountdownEvent(1))
            {
                this.instrumentation?.NodeQueuedForDelete(this.nodeList.Count);
                string scheduleName = Guid.NewGuid().ToString();
                string stagingLocation = $"/$ScheduledDelete/{scheduleName}";
                var operations = new List<Op>();
                operations.Add(Op.Move(rootPath, -1, stagingLocation, MoveMode.AllowPathCreationFlag));

                var scheduledOperations = new Op[1];
                scheduledOperations[0] = Op.Delete(stagingLocation, -1, DeleteMode.FastDelete | DeleteMode.CascadeDelete);
                operations.Add(Op.Run(new RequestMulti(scheduledOperations, completeSynchronously: true, scheduledName: scheduleName)));

                var timer = Stopwatch.StartNew();
                Trace.TraceInformation($"DeletePerformance.ScheduledDelete: rootPath={rootPath}, stagingLocation={stagingLocation}, scheduleName={scheduleName}");
                this.IssueMultiRequest(ringMaster, operations, operationsCompletedEvent);

                operationsCompletedEvent.Signal();
                operationsCompletedEvent.Wait();
                timer.Stop();
                Trace.TraceInformation($"DeletePerformance.ScheduledDelete: scheduleName={scheduleName}, elapsedMilliseconds={timer.ElapsedMilliseconds}");
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void IssueMultiRequest(IRingMasterRequestHandler ringMaster, IReadOnlyList<Op> operations, CountdownEvent operationsCompletedEvent)
        {
            var multiRequest = new RequestMulti(operations, completeSynchronously: true, uid: 0);
            this.semaphore.Wait();
            var timer = Stopwatch.StartNew();
            int operationsCount = operations.Count;

            operationsCompletedEvent.AddCount(operationsCount);
            ringMaster.Request(multiRequest).ContinueWith(responseTask =>
            {
                this.semaphore.Release();
                timer.Stop();
                try
                {
                    RequestResponse response = responseTask.Result;
                    if (response.ResultCode == (int)RingMasterException.Code.Ok)
                    {
                        this.instrumentation?.DeleteMultiSucceeded(operationsCount, timer.Elapsed);
                    }
                    else
                    {
                        this.instrumentation?.DeleteMultiFailed(operationsCount);
                    }
                }
                catch (Exception)
                {
                    this.instrumentation?.DeleteMultiFailed(operationsCount);
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
