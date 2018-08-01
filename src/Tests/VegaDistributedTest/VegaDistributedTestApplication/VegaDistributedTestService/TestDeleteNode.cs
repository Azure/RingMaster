// <copyright file="TestDeleteNode.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.DistributedTest
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Vega.Test.Helpers;
    using DistTestCommonProto;

    /// <summary>
    /// The delete node test
    /// </summary>
    /// <seealso cref="Microsoft.Vega.DistributedTest.TestBase" />
    internal sealed class TestDeleteNode : TestBase
    {
        /// <inheritdoc/>
        protected override Task RunTest(JobState jobState, CancellationToken cancellation)
        {
            jobState.Started = true;

            if (this.WatcherCountPerNode > 0)
            {
                this.InstallBulkWatcher(WatchedEvent.WatchedEventType.NodeChildrenChanged, jobState, cancellation).GetAwaiter().GetResult();
            }

            Task.Run(() => this.TraverseTree(new List<string>() { this.RootNodeName, $"{this.RootNodeName}_Batch", $"{this.RootNodeName}_Multi" }, cancellation));

            jobState.Status = "start deleting nodes";
            var rate = this.TestFlowAsync(
                "Delete data node perf test",
                OperationType.Delete,
                this.DeleteNodeThread,
                jobState,
                this.TestCaseSeconds * 4)
                .GetAwaiter().GetResult();

            this.Log($"Node delete rate: {rate:G4} /sec");

            if (this.WatcherCountPerNode > 0 && !this.WaitForWatchers().Result)
            {
                jobState.Status = "did not receive all watched events";
                jobState.Passed = false;
                return Task.FromResult(0);
            }

            jobState.Passed = rate > 0;
            return Task.FromResult(0);
        }

        /// <summary>
        /// Work load for testing Create method
        /// </summary>
        /// <param name="client">RingMasterClient object</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="threadId">Thread sequence number</param>
        /// <returns>Async task</returns>
        private Task DeleteNodeThread(IRingMasterRequestHandler client, CancellationToken token, int threadId)
        {
            int taskCount = 0;
            var clock = Stopwatch.StartNew();

            while (!token.IsCancellationRequested)
            {
                while (this.QueuedNodes.TryDequeue(out string path) && !token.IsCancellationRequested)
                {
                    SpinWait.SpinUntil(() => taskCount < this.AsyncTaskCount || token.IsCancellationRequested);
                    var startTime = clock.Elapsed;
                    var task = client.Delete(path, -1, DeleteMode.None)
                            .ContinueWith(
                                t =>
                                {
                                    Interlocked.Decrement(ref taskCount);
                                    if (!t.Result)
                                    {
                                        this.Log($"Failed to delete {path}.");
                                        if (t.Exception != null)
                                        {
                                            this.Log($"Exception: {t.Exception.Message}");
                                            this.IncrementTotalFailures();
                                        }
                                    }
                                    else
                                    {
                                        this.IncrementTotalDataCount();

                                        var duration = clock.Elapsed - startTime;
                                        MdmHelper.LogOperationDuration((long)duration.TotalMilliseconds, OperationType.Delete);
                                    }
                                });

                    Interlocked.Increment(ref taskCount);
                }
            }

            SpinWait.SpinUntil(() => taskCount == 0);

            return Task.FromResult(0);
        }
    }
}
