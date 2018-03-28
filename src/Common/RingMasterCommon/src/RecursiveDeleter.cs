// <copyright file="RecursiveDeleter.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using RingMaster.Data;
    using RingMaster.Requests;

    /// <summary>
    /// RecursiveDeleter manages the state of a recursive delete operation.
    /// </summary>
    public sealed class RecursiveDeleter
    {
        /// <summary>
        /// Instrumentation consumer.
        /// </summary>
        private readonly IInstrumentation instrumentation;

        /// <summary>
        /// Number of nodes deleted by this deleter.
        /// </summary>
        private int deletedCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecursiveDeleter"/> class.
        /// </summary>
        /// <param name="instrumentation">Instrumentation object to get the internal state notification</param>
        /// <param name="maxChildrenEnumerationCount">maximum number of children that the RecursiveDeleter will enumerate at one time</param>
        public RecursiveDeleter(IInstrumentation instrumentation, int maxChildrenEnumerationCount = 1000)
        {
            this.instrumentation = instrumentation;
            this.MaxChildrenEnumerationCount = maxChildrenEnumerationCount;
        }

        /// <summary>
        /// Instrumentation interface
        /// </summary>
        public interface IInstrumentation
        {
            /// <summary>
            /// Report that delete has been queued for a node.
            /// </summary>
            /// <param name="nodesDeleted">Number of nodes deleted so far</param>
            /// <param name="nodePath">Path to the node</param>
            void DeleteQueued(int nodesDeleted, string nodePath);

            /// <summary>
            /// Report that a multi of delete operations has been successfully applied.
            /// </summary>
            /// <param name="nodesDeleted">Number of nodes deleted so far</param>
            /// <param name="operationsCount">Number of operations in the multi</param>
            /// <param name="latency">Time taken by the multi</param>
            void DeleteMultiSucceeded(int nodesDeleted, int operationsCount, TimeSpan latency);

            /// <summary>
            /// Report that a multi of delete operations has failed.
            /// </summary>
            /// <param name="nodesDeleted">Number of nodes deleted so far</param>
            /// <param name="operationsCount">Number of operations in the multi</param>
            /// <param name="latency">Time taken by the multi</param>
            void DeleteMultiFailed(int nodesDeleted, int operationsCount, TimeSpan latency);

            /// <summary>
            /// Report that a recursive delete operation succeeded.
            /// </summary>
            /// <param name="nodesDeleted">Number of nodes deleted</param>
            /// <param name="latency">Time taken by the recursive delete</param>
            void RecursiveDeleteSucceeded(int nodesDeleted, TimeSpan latency);

            /// <summary>
            /// Report that a recursive delete operation failed.
            /// </summary>
            /// <param name="nodesDeleted">Number of nodes successfully deleted</param>
            /// <param name="latency">Time taken by the recursive delete</param>
            void RecursiveDeleteFailed(int nodesDeleted, TimeSpan latency);
        }

        /// <summary>
        /// Gets or sets the maximum number of deletes that will be batched together.
        /// </summary>
        public int MaxDeleteBatchLength { get; set; } = 50;

        /// <summary>
        /// Gets or sets the maximum number of children that the RecursiveDeleter will enumerate at one time.
        /// </summary>
        public int MaxChildrenEnumerationCount { get; set; }

        /// <summary>
        /// Recursively deletes all the nodes under the given path.
        /// </summary>
        /// <param name="ringMaster">Interface to RingMaster</param>
        /// <param name="path">Path to recursively delete</param>
        /// <param name="cancellationToken">Token to be observed for cancellation signal</param>
        /// <returns>A <see cref="Task"/> that resolves to the number of nodes deleted</returns>
        public async Task<int> Delete(IRingMasterRequestHandler ringMaster, string path, CancellationToken cancellationToken)
        {
            var recursiveDeleteTimer = Stopwatch.StartNew();
            var pendingNodes = new Stack<NodeState>();
            var deleteOperations = new List<Op>();

            this.deletedCount = 0;

            try
            {
                pendingNodes.Push(new NodeState(path));

                while (pendingNodes.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var currentNode = pendingNodes.Pop();

                    if (!currentNode.AllChildrenProcessed)
                    {
                        IReadOnlyList<string> children = await ringMaster.GetChildren(
                            currentNode.Path,
                            watcher: null,
                            retrievalCondition: string.Format(">:{0}:{1}", this.MaxChildrenEnumerationCount, currentNode.StartingChildName));

                        pendingNodes.Push(new NodeState
                        {
                            Path = currentNode.Path,
                            StartingChildName = children.Count > 0 ? children[children.Count - 1] : string.Empty,
                            AllChildrenProcessed = children.Count < this.MaxChildrenEnumerationCount,
                        });

                        foreach (var child in children)
                        {
                            string childFullPath = (currentNode.Path == "/") ? $"/{child}" : $"{currentNode.Path}/{child}";
                            pendingNodes.Push(new NodeState(childFullPath));
                        }
                    }
                    else
                    {
                        this.instrumentation?.DeleteQueued(this.deletedCount, currentNode.Path);
                        deleteOperations.Add(Op.Delete(currentNode.Path, version: -1));
                    }

                    if (deleteOperations.Count >= this.MaxDeleteBatchLength)
                    {
                        await this.DeleteMulti(ringMaster, deleteOperations);
                        deleteOperations.Clear();
                    }
                }

                if (deleteOperations.Count > 0)
                {
                    await this.DeleteMulti(ringMaster, deleteOperations);
                }

                this.instrumentation?.RecursiveDeleteSucceeded(this.deletedCount, recursiveDeleteTimer.Elapsed);
                return this.deletedCount;
            }
            catch
            {
                this.instrumentation?.RecursiveDeleteFailed(this.deletedCount, recursiveDeleteTimer.Elapsed);
                throw;
            }
        }

        /// <summary>
        /// Apply a set of delete operations as a multi.
        /// </summary>
        /// <param name="ringMaster">Interface to RingMaster</param>
        /// <param name="deleteOperations">Delete operations to apply as a batch</param>
        /// <returns>Async task to indicate the completion of the request</returns>
        private async Task DeleteMulti(IRingMasterRequestHandler ringMaster, IReadOnlyList<Op> deleteOperations)
        {
            var timer = Stopwatch.StartNew();
            var multiRequest = new RequestMulti(deleteOperations, completeSynchronously: true);
            var response = await ringMaster.Request(multiRequest);

            if (response.ResultCode == (int)RingMasterException.Code.Ok)
            {
                this.deletedCount += deleteOperations.Count;
                this.instrumentation?.DeleteMultiSucceeded(this.deletedCount, deleteOperations.Count, timer.Elapsed);
            }
            else
            {
                this.instrumentation?.DeleteMultiFailed(this.deletedCount, deleteOperations.Count, timer.Elapsed);
                throw RingMasterException.GetException(response);
            }
        }

        /// <summary>
        /// State of a node in the pending nodes stack.
        /// </summary>
        private struct NodeState
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="NodeState"/> struct.
            /// </summary>
            /// <param name="path">Path of the node</param>
            public NodeState(string path)
            {
                this.Path = path;
                this.StartingChildName = string.Empty;
                this.AllChildrenProcessed = false;
            }

            /// <summary>
            /// Gets or sets the full path to the node.
            /// </summary>
            public string Path { get; set; }

            /// <summary>
            /// Gets or sets the starting child name for the next children enumeration.
            /// </summary>
            public string StartingChildName { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether all children of this node were enumerated.
            /// </summary>
            public bool AllChildrenProcessed { get; set; }
        }
    }
}
