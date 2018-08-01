// <copyright file="TestLnmVnetPublishingScenario.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.DistributedTest
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Vega.Test.Helpers;
    using DistTestCommonProto;

    /// <summary>
    /// Test the NSM/LNM VNET Publishing
    /// </summary>
    /// <seealso cref="Microsoft.Vega.DistributedTest.TestBase" />
    internal sealed class TestLnmVnetPublishingScenario : TestBase
    {
        /// <inheritdoc/>
        protected override Task RunTest(JobState jobState, CancellationToken cancellation)
        {
            jobState.Started = true;
            jobState.Status = "start Lnm Vnet publishing scenario";

            var rate = this.TestFlowAsync(
                "TestLnmVnetPublishingScenario",
                OperationType.LnmVnetPublishingScenario,
                this.LnmPublishingThread,
                jobState,
                this.TestCaseSeconds * 2)
                .GetAwaiter().GetResult();

            jobState.Passed = rate > 0;
            return Task.FromResult(0);
        }

        /// <summary>
        /// Creates a VNET ID spanning across multiple cluster, which is mimicked by thread
        /// </summary>
        /// <param name="threadId">Thread sequence number</param>
        /// <returns>A random VNET ID in string</returns>
        private static string CreateSpanningVnetId(int threadId)
        {
            return string.Concat(DateTime.UtcNow.ToString("HHmmss"), threadId);
        }

        private async Task LnmPublishingThread(IRingMasterRequestHandler client, CancellationToken token, int threadId)
        {
            var rnd = new Random();

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var vnet = $"/Instance{this.ServiceContext.ReplicaOrInstanceId}/vnets/{CreateSpanningVnetId(threadId)}";
                    var stat = await client.Exists(vnet, null, true);
                    var ops = new List<Op>();

                    if (stat == null)
                    {
                        ops.Add(Op.Create($"{vnet}/mappings/v4ca", null, null, CreateMode.PersistentAllowPathCreation));
                        ops.Add(Op.Create($"{vnet}/lnms/thread-{threadId}", null, null, CreateMode.PersistentAllowPathCreation));

                        await client.Multi(ops, true);
                        ops.Clear();

                        this.IncrementTotalDataCount(2);
                    }

                    var mappingCount = rnd.Next(1, 1024 * 8);
                    for (int i = 0; i < mappingCount; i++)
                    {
                        ops.Add(Op.Create($"{vnet}/mappings/v4ca/{i}", null, null, CreateMode.PersistentAllowPathCreation));
                    }

                    this.IncrementTotalDataCount(ops.Count);
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    await client.Multi(ops, true);
                    ops.Clear();

                    for (int i = 0; i < mappingCount; i++)
                    {
                        var data = new byte[rnd.Next(this.MinDataSize, this.MaxDataSize)];
                        ops.Add(Op.SetData($"{vnet}/mappings/v4ca/{i}", data, -1));
                        this.AddTotalDataSize(data.Length);
                    }

                    this.IncrementTotalDataCount(mappingCount);
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    await client.Multi(ops, true);
                    ops.Clear();
                }
                catch (Exception ex)
                {
                    this.IncrementTotalFailures();

                    // Ignore and keep going
                    this.Log($"FAIL in {threadId}: {ex.Message}");
                }
            }
        }
    }
}
