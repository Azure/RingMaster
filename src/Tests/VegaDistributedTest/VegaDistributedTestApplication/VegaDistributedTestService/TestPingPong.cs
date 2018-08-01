// <copyright file="TestPingPong.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.DistributedTest
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Vega.Test.Helpers;
    using DistTestCommonProto;

    /// <summary>
    /// Pings the service to measure the overhead of basic request processing path without any read/write operation
    /// </summary>
    /// <seealso cref="Microsoft.Vega.DistributedTest.ITestJob" />
    internal class TestPingPong : TestBase
    {
        /// <summary>
        /// Runs the test.
        /// </summary>
        /// <param name="jobState">State of the job.</param>
        /// <param name="cancellation">The cancellation.</param>
        /// <returns>
        /// async task
        /// </returns>
        protected override Task RunTest(JobState jobState, CancellationToken cancellation)
        {
            jobState.Started = true;
            jobState.Status = "start pingpong test";

            var rate = this.TestFlowAsync(
                "Ping-Pong Test",
                OperationType.PingPong,
                this.PingPongThread,
                jobState,
                this.TestCaseSeconds)
                .GetAwaiter().GetResult();

            this.Log($"Ping-Pong test rate: {rate:G4} /sec");
            jobState.Passed = rate > 0;

            return Task.FromResult(0);
        }

        /// <summary>
        /// Work load for ping pong test
        /// </summary>
        /// <param name="client">RingMasterClient object</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="threadId">Thread sequence number</param>
        /// <returns>Async task</returns>
        private async Task PingPongThread(IRingMasterRequestHandler client, CancellationToken token, int threadId)
        {
            var clock = Stopwatch.StartNew();
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var startTime = clock.Elapsed;
                    var tasks = Enumerable.Range(0, this.AsyncTaskCount)
                        .Select(task => client.Exists(string.Empty, null, true)
                                .ContinueWith(t =>
                                {
                                    var duration = clock.Elapsed - startTime;
                                    MdmHelper.LogOperationDuration((long)duration.TotalMilliseconds, OperationType.PingPong);
                                }))
                        .ToArray();

                    await Task.WhenAll(tasks);
                    this.IncrementTotalDataCount(tasks.Length);
                }
                catch (Exception ex)
                {
                    this.IncrementTotalFailures();
                    this.Log($"Failed to call Batch: {ex.Message}");
                }
            }
        }
    }
}
