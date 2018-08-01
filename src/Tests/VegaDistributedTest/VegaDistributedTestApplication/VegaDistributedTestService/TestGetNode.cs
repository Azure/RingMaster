// <copyright file="TestGetNode.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.DistributedTest
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using DistTestCommonProto;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Vega.Test.Helpers;

    /// <summary>
    /// The get node test.
    /// </summary>
    /// <seealso cref="Microsoft.Vega.DistributedTest.TestBase" />
    internal sealed class TestGetNode : TestBase
    {
        /// <inheritdoc/>
        protected override Task RunTest(JobState jobState, CancellationToken cancellation)
        {
            jobState.Started = true;
            Task.Run(() => this.TraverseTree(new List<string>() { this.RootNodeName }, cancellation));

            jobState.Status = "start getting nodes";
            var rate = this.TestFlowAsync(
                "get data node perf test",
                OperationType.Get,
                this.GetNodeThread,
                jobState,
                this.TestCaseSeconds)
                .GetAwaiter().GetResult();

            this.Log($"Node get rate: {rate:G4} /sec");

            jobState.Passed = rate > 0;

            return Task.FromResult(0);
        }

        /// <summary>
        /// Work load for testing GetNode method
        /// </summary>
        /// <param name="client">RingMasterClient object</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="threadId">Thread sequence number</param>
        /// <returns>Async task</returns>
        private Task GetNodeThread(IRingMasterRequestHandler client, CancellationToken token, int threadId)
        {
            int taskCount = 0;
            var clock = Stopwatch.StartNew();

            while (!token.IsCancellationRequested)
            {
                while (this.QueuedNodes.TryDequeue(out string path) && !token.IsCancellationRequested)
                {
                    SpinWait.SpinUntil(() => taskCount < this.AsyncTaskCount || token.IsCancellationRequested);
                    var startTime = clock.Elapsed;
                    var task = client.GetData(path, null)
                        .ContinueWith(
                            t =>
                            {
                                Interlocked.Decrement(ref taskCount);

                                if (t.Exception != null)
                                {
                                    this.IncrementTotalFailures();
                                    this.Log($"Failed to get {path}: {t.Exception.Message}");
                                }
                                else
                                {
                                    var data = t.Result;
                                    this.AddTotalDataSize(data.Length);
                                    this.IncrementTotalDataCount();

                                    var duration = clock.Elapsed - startTime;
                                    MdmHelper.LogOperationDuration((long)duration.TotalMilliseconds, OperationType.Get);
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
