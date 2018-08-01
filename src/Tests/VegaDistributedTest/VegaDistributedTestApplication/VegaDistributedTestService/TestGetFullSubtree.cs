// <copyright file="TestGetFullSubtree.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.DistributedTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Vega.Test.Helpers;
    using DistTestCommonProto;

    /// <summary>
    /// the get full subtree test
    /// </summary>
    /// <seealso cref="Microsoft.Vega.DistributedTest.TestBase" />
    internal sealed class TestGetFullSubtree : TestBase
    {
        /// <inheritdoc/>
        protected override Task RunTest(JobState jobState, CancellationToken cancellation)
        {
            jobState.Started = true;

            Task.Run(async () =>
            {
                var startFrom = string.Empty;
                while (!cancellation.IsCancellationRequested)
                {
                    var children = await this.HelperClient.GetChildren($"/{this.RootNodeName}/Instance{this.ServiceContext.ReplicaOrInstanceId}", null, $">:{MaxChildrenCount}:{startFrom}");
                    foreach (var child in children)
                    {
                        this.QueuedNodes.Enqueue($"/{this.RootNodeName}/Instance{this.ServiceContext.ReplicaOrInstanceId}/{child}/mappings/v4ca");
                        startFrom = child;
                    }

                    if (children.Count < MaxChildrenCount)
                    {
                        break;
                    }
                }
            });

            jobState.Status = "starting get full subtree";
            var rate = this.TestFlowAsync(
                "Get full sub-tree perf test",
                OperationType.GetFullSubtree,
                this.GetFullSubtreeThread,
                jobState,
                this.TestCaseSeconds)
                .GetAwaiter().GetResult();

            this.Log($"get full subtree rate: {rate:G4} /sec");

            jobState.Passed = rate > 0;
            return Task.FromResult(0);
        }

        /// <summary>
        /// Work load for getting full sub-tree
        /// </summary>
        /// <param name="client">RingMasterClient object</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="threadId">Thread sequence number</param>
        /// <returns>Async task</returns>
        private Task GetFullSubtreeThread(IRingMasterRequestHandler client, CancellationToken token, int threadId)
        {
            int taskCount = 0;
            var clock = Stopwatch.StartNew();

            while (!token.IsCancellationRequested)
            {
                while (this.QueuedNodes.TryDequeue(out string path) && !token.IsCancellationRequested)
                {
                    SpinWait.SpinUntil(() => taskCount < this.AsyncTaskCount || token.IsCancellationRequested);
                    var startTime = clock.Elapsed;
                    var task = client.GetFullSubtree(path, true)
                        .ContinueWith(
                            t =>
                            {
                                Interlocked.Decrement(ref taskCount);

                                if (t.Exception != null)
                                {
                                    this.IncrementTotalFailures();
                                    this.Log($"Failed to get full subtree on path {path}: {t.Exception.Message}");
                                }
                                else
                                {
                                    var children = t.Result.Children;
                                    this.AddTotalDataSize(children.Sum(c => c.Data.Length));
                                    this.IncrementTotalDataCount(children.Count);

                                    var duration = (clock.Elapsed - startTime).TotalMilliseconds;
                                    MdmHelper.LogOperationDuration((long)duration, OperationType.GetFullSubtree);
                                }
                            });

                    Interlocked.Increment(ref taskCount);

                    this.QueuedNodes.Enqueue(path);
                }
            }

            SpinWait.SpinUntil(() => taskCount == 0);
            return Task.FromResult(0);
        }
    }
}
