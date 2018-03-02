// <copyright file="CreatePerformance.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Performance
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    public class CreatePerformance : IDisposable
    {
        /// <summary>
        /// Maximum valid code point that can be used in a string.
        /// </summary>
        private const int MaxCodePoint = 0x10FFFF;

        private readonly CancellationToken cancellationToken;
        private readonly IInstrumentation instrumentation;
        private readonly SemaphoreSlim semaphore;

        private bool isDisposed = false;

        public CreatePerformance(IInstrumentation instrumentation, int maxConcurrentRequests, CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;
            this.instrumentation = instrumentation;
            this.semaphore = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
        }

        public interface IInstrumentation
        {
            void NodeQueuedForCreate(int nodeCount);

            void CreateMultiSucceeded(int successCount, TimeSpan elapsed);

            void CreateMultiFailed(int failureCount);
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
        /// Gets or sets the minimum number of codepoints in a node name.
        /// </summary>
        public int MinNodeNameLength { get; set; } = 2;

        /// <summary>
        /// Gets or sets the maximum number of codepoints in a node name.
        /// </summary>
        public int MaxNodeNameLength { get; set; } = 8;

        /// <summary>
        /// Gets or sets the minimum number of children that will be created per node.
        /// </summary>
        public int MinChildrenCountPerNode { get; set; } = 8;

        /// <summary>
        /// Gets or sets the maximum number of children that will be created per node.
        /// </summary>
        public int MaxChildrenCountPerNode { get; set; } = 32;

        /// <summary>
        /// Gets or sets the maximum allowed codepoint in randomly generated names.
        /// </summary>
        public int MaxAllowedCodePoint { get; set; } = MaxCodePoint;

        /// <summary>
        /// Create a hierarchy of nodes under the given path.
        /// </summary>
        /// <param name="ringMaster">RingMaster client</param>
        /// <param name="rootPath">Path where the hierarchy must be created</param>
        /// <param name="batchLength">Number create requests per multi</param>
        /// <param name="maxNodes">Number of nodes to create</param>
        public void CreateHierarchy(IRingMasterRequestHandler ringMaster, string rootPath, int batchLength, int maxNodes)
        {
            int nodesCreated = 0;
            int numNodesToCreate = maxNodes;
            var paths = new Queue<string>();

            paths.Enqueue(rootPath);

            using (var operationsCompletedEvent = new CountdownEvent(1))
            {
                var random = new RandomGenerator();
                var operations = new List<Op>();
                while (!this.cancellationToken.IsCancellationRequested && numNodesToCreate > 0)
                {
                    string currentNode = paths.Dequeue();
                    int numChildrenToCreate = random.GetRandomInt(this.MinChildrenCountPerNode, this.MaxChildrenCountPerNode);
                    numChildrenToCreate = Math.Min(numChildrenToCreate, numNodesToCreate);
                    numNodesToCreate -= numChildrenToCreate;

                    for (int i = 0; i < numChildrenToCreate; i++)
                    {
                        string childName = random.GetRandomName(this.MinNodeNameLength, this.MaxNodeNameLength, this.MaxAllowedCodePoint);
                        string childPath = (currentNode == "/") ? $"/{childName}" : $"{currentNode}/{childName}";
                        byte[] childData = random.GetRandomData(this.MinDataSizePerNode, this.MaxDataSizePerNode);

                        paths.Enqueue(childPath);
                        operations.Add(Op.Create(childPath, childData, null, CreateMode.Persistent | CreateMode.AllowPathCreationFlag));
                        nodesCreated++;
                        this.instrumentation?.NodeQueuedForCreate(nodesCreated);
                        if (operations.Count >= batchLength)
                        {
                            this.IssueMultiRequest(ringMaster, operations, operationsCompletedEvent);
                            operations.Clear();
                        }

                        if (this.cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }
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
        /// Create the specified number of nodes nodes directly under the given path.
        /// </summary>
        /// <param name="ringMaster">RingMaster client</param>
        /// <param name="rootPath">Path where the hierarchy must be created</param>
        /// <param name="batchLength">Number create requests per multi</param>
        /// <param name="maxNodes">Number of nodes to create</param>
        public void CreateFlat(IRingMasterRequestHandler ringMaster, string rootPath, int batchLength, int maxNodes)
        {
            var random = new RandomGenerator();
            int nodesCreated = 0;
            using (var operationsCompletedEvent = new CountdownEvent(1))
            {
                var operations = new List<Op>();
                while (!this.cancellationToken.IsCancellationRequested && nodesCreated < maxNodes)
                {
                    string childName = random.GetRandomName(this.MinNodeNameLength, this.MaxNodeNameLength);
                    string childPath = (rootPath == "/") ? $"/{childName}" : $"{rootPath}/{childName}";
                    byte[] childData = random.GetRandomData(this.MinDataSizePerNode, this.MaxDataSizePerNode);

                    operations.Add(Op.Create(childPath, childData, null, CreateMode.Persistent | CreateMode.AllowPathCreationFlag));
                    nodesCreated++;
                    this.instrumentation?.NodeQueuedForCreate(nodesCreated);
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
        /// Dispose this instance.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void IssueMultiRequest(IRingMasterRequestHandler ringMaster, IReadOnlyList<Op> operations, CountdownEvent operationsCompletedEvent)
        {
            var multiRequest = new RequestMulti(operations, completeSynchronously: true, uid: 0);
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
                        this.instrumentation?.CreateMultiSucceeded(results.Count, timer.Elapsed);
                    }
                    else
                    {
                        this.instrumentation?.CreateMultiFailed(operationsCount);
                    }
                }
                catch (Exception)
                {
                    this.instrumentation?.CreateMultiFailed(operationsCount);
                }
                finally
                {
                    operationsCompletedEvent.Signal(operationsCount);
                }
            });
        }

        private void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    this.semaphore.Dispose();
                }

                this.isDisposed = true;
            }
        }
    }
}
