// <copyright file="TestSetDataAndUserMetadata.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.DistributedTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Vega.DistTestCommonProto;
    using Microsoft.Vega.Test.Helpers;

    /// <summary>
    /// Test set user metadata
    /// </summary>
    /// <seealso cref="Microsoft.Vega.DistributedTest.TestBase" />
    internal sealed class TestSetDataAndUserMetadata : TestBase
    {
        /// <inheritdoc/>
        protected override Task RunTest(JobState jobState, CancellationToken cancellation)
        {
            jobState.Started = true;

            Task.Run(() => this.TraverseTree(new List<string>() { this.RootNodeName }, cancellation));

            jobState.Status = "start setting user metadata";
            var rate = this.TestFlowAsync(
                "Set user metadata perf test",
                OperationType.SetDataAndUserMetadata,
                this.SetDataAndUserMetadataThread,
                jobState,
                this.TestCaseSeconds * 2)
                .GetAwaiter().GetResult();

            this.Log($"Node set user metadata rate: {rate:G4} /sec");
            VegaDistTestEventSource.Log.OperationThroughput(OperationType.SetDataAndUserMetadata, rate);

            jobState.Passed = rate > 0;
            return Task.FromResult(0);
        }

        /// <summary>
        /// Work load for testing set user metadata method
        /// </summary>
        /// <param name="client">RingMasterClient object</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="threadId">Thread sequence number</param>
        /// <returns>Async task</returns>
        private Task SetDataAndUserMetadataThread(IRingMasterRequestHandler client, CancellationToken token, int threadId)
        {
            int taskCount = 0;
            var rnd = new Random();
            var clock = Stopwatch.StartNew();

            while (!token.IsCancellationRequested)
            {
                while (this.QueuedNodes.TryDequeue(out string path) && !token.IsCancellationRequested)
                {
                    var data = Helpers.MakeRandomData(rnd, rnd.Next(this.MinDataSize, this.MaxDataSize));

                    SpinWait.SpinUntil(() => taskCount < this.AsyncTaskCount || token.IsCancellationRequested);
                    var startTime = clock.Elapsed;
                    var task = client.SetDataAndUserMetadata(path, data, -1, data, -1)
                            .ContinueWith(
                            t =>
                            {
                                Interlocked.Decrement(ref taskCount);

                                if (t.Exception != null)
                                {
                                    this.IncrementTotalFailures();
                                    this.Log($"Failed to set user metadata on {path}: {t.Exception.Message}");
                                }
                                else
                                {
                                    this.AddTotalDataSize(data.Length);
                                    this.IncrementTotalDataCount();

                                    var duration = clock.Elapsed - startTime;
                                    MdmHelper.LogOperationDuration((long)duration.TotalMilliseconds, OperationType.SetDataAndUserMetadata);
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
