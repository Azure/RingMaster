// <copyright file="TestCreateNode.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
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
    /// The create node test.
    /// </summary>
    /// <seealso cref="Microsoft.Vega.DistributedTest.TestBase" />
    internal sealed class TestCreateNode : TestBase
    {
        /// <inheritdoc/>
        protected override Task RunTest(JobState jobState, CancellationToken cancellation)
        {
            jobState.Started = true;

            Random rnd = new Random();

            // number of large trees will be a random number between (20, 50)
            this.LargeTreeRoots = Enumerable.Range(0, rnd.Next(20, 50)).Select(x => Guid.NewGuid()).ToList();

            jobState.Status = "start creating nodes";
            var rate = this.TestFlowAsync(
                "Create data node perf test",
                OperationType.Create,
                this.CreateNodeThread,
                jobState,
                this.TestCaseSeconds * 6)
                .GetAwaiter().GetResult();

            this.Log($"Node create rate: {rate:G4} /sec");

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
        private Task CreateNodeThread(IRingMasterRequestHandler client, CancellationToken token, int threadId)
        {
            var rnd = new Random();
            int taskCount = 0;
            var clock = Stopwatch.StartNew();
            bool createSmallTree = true;

            var rootName = $"{this.RootNodeName}/Instance{this.ServiceContext.ReplicaOrInstanceId}";
            while (!token.IsCancellationRequested)
            {
                var dataSize = 0;
                var dataCount = 0;
                int numToCreate = 0;
                var subtree = string.Empty;

                if (createSmallTree)
                {
                    numToCreate = rnd.Next(0, 20);
                    subtree = $"/{rootName}/vnet{Guid.NewGuid()}/mappings/v4ca";
                }
                else
                {
                    // create big tree children;
                    numToCreate = this.LargeTreeRatio;
                    int idx = rnd.Next(this.LargeTreeRoots.Count);
                    subtree = $"/{rootName}/vnet{this.LargeTreeRoots[idx]}/mappings/v4ca";
                }

                // flip the flag so that the thread switches between creating small trees and large trees.
                createSmallTree = !createSmallTree;

                while (numToCreate-- > 0 && !token.IsCancellationRequested)
                {
                    SpinWait.SpinUntil(() => taskCount < this.AsyncTaskCount || token.IsCancellationRequested);

                    var path = $"{subtree}/{Guid.NewGuid()}";
                    var data = Helpers.MakeRandomData(rnd, rnd.Next(this.MinDataSize, this.MaxDataSize));

                    var startTime = clock.Elapsed;
                    var unused = client.Create(path, data, null, CreateMode.PersistentAllowPathCreation | CreateMode.SuccessEvenIfNodeExistsFlag)
                        .ContinueWith(t =>
                        {
                            Interlocked.Decrement(ref taskCount);

                            if (t.Exception != null)
                            {
                                this.Log($"Failed path: {path}");
                                this.IncrementTotalFailures();
                            }
                            else
                            {
                                this.AddTotalDataSize(data.Length);
                                this.IncrementTotalDataCount();

                                var duration = clock.Elapsed - startTime;
                                MdmHelper.LogOperationDuration((long)duration.TotalMilliseconds, OperationType.Create);
                            }
                        });

                    Interlocked.Increment(ref taskCount);
                    dataSize += data.Length;
                    dataCount++;
                }
            }

            SpinWait.SpinUntil(() => taskCount == 0);
            return Task.FromResult(0);
        }
    }
}
