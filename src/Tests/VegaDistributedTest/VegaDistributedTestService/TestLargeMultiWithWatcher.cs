// <copyright file="TestLargeMultiWithWatcher.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.DistributedTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Fabric;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Vega.DistTestCommonProto;
    using Microsoft.Vega.Test.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Test scenario that (1). 45 bulk watchers on one vnet from different session
    /// (2). one session have ~3k bulkwatchers across different vnets.
    /// (3). One client send large multi requests sequentially (e.g. with ~10k write requests on the same vnet).
    /// (4). Other clients send read requests concurrently.
    /// The test assumes the tree is already created (The TestCreateNode test has run)
    /// </summary>
    /// <seealso cref="Microsoft.Vega.DistributedTest.TestBase" />
    internal class TestLargeMultiWithWatcher : TestBase
    {
        private const int SendMultiInstanceId = 0;

        private const int GetNonMultiPathThreadId = 2;

        private int requestCountInMulti;

        private List<double> multiResponseTimeMs = new List<double>();

        private string targetVnetPath;

        private List<string> otherVnets = new List<string>();

        private List<Op> multiRequest;

        private List<string> nonMultiPath;

        private Random rnd = new Random();

        /// <inheritdoc/>
        public override async Task Initialize(Dictionary<string, string> parameters, StatelessServiceContext serviceContext, TestNodeContext testNodeContext)
        {
            await base.Initialize(parameters, serviceContext, testNodeContext);
            this.nonMultiPath = new List<string>();
            this.requestCountInMulti = int.Parse(parameters["RequestCountInMulti"]);

            this.targetVnetPath = await this.FindTargetVnet();
            this.multiRequest = await this.GetMultiRequest(this.targetVnetPath);
            Assert.IsTrue(this.multiRequest.Count == this.requestCountInMulti);
        }

        /// <inheritdoc/>
        public override Dictionary<string, double[]> GetJobMetrics()
        {
            var result = base.GetJobMetrics();
            result.Add("MultiResponseTimeMs", this.multiResponseTimeMs.ToArray());

            return result;
        }

        /// <inheritdoc/>
        protected override async Task RunTest(JobState jobState, CancellationToken cancellation)
        {
            jobState.Started = true;

            try
            {
                if (this.TestNodeContext.TestNodeId == SendMultiInstanceId)
                {
                    await this.InstallBulkWatcher(WatchedEvent.WatchedEventType.NodeDataChanged, jobState, cancellation);

                    var clock = Stopwatch.StartNew();
                    long multiCount = 0;
                    var stop = false;
                    var showStatus = Task.Run(async () =>
                    {
                        while (!cancellation.IsCancellationRequested && !stop)
                        {
                            await Task.Delay(2000);
                            Helper.LogAndSetJobStatus(this.Log, jobState, $"Send multi request count: {Interlocked.Read(ref multiCount)}");
                        }
                    });

                    while (clock.Elapsed.TotalSeconds < this.TestCaseSeconds && !cancellation.IsCancellationRequested)
                    {
                        try
                        {
                            var multiStart = clock.Elapsed;
                            var multiResponse = await this.HelperClient.Multi(this.multiRequest);
                            this.Log($"Multi process time: {(clock.Elapsed - multiStart).TotalSeconds}");
                            for (int i = 0; i <= 100; i++)
                            {
                                Assert.AreEqual(RingMasterException.Code.Ok, multiResponse[i].ErrCode);
                            }

                            Interlocked.Increment(ref multiCount);
                        }
                        catch (Exception e)
                        {
                            this.Log($"Exception in Multi: {e.ToString()}");
                        }
                    }

                    stop = true;
                    await showStatus;
                    var multiRate = multiCount / clock.Elapsed.TotalSeconds;
                    Helper.LogAndSetJobStatus(this.Log, jobState, $"Multi rate: {multiRate:G4} multi per second");
                }
                else if (this.TestNodeContext.TestNodeId <= GetNonMultiPathThreadId && this.TestNodeContext.TestNodeId > SendMultiInstanceId)
                {
                    var rate = await this.TestFlowAsync(
                       "get non multi path in Multi perf test",
                       Test.Helpers.OperationType.Create,
                       this.GetNonMultiPathThread,
                       jobState,
                       this.TestCaseSeconds);
                }
                else
                {
                    var rate = await this.TestFlowAsync(
                        "Get request in Multi perf test",
                        Test.Helpers.OperationType.Get,
                        this.GetRequestThread,
                        jobState,
                        this.TestCaseSeconds);
                }
            }
            catch (Exception ex)
            {
                this.Log("Exception running test: " + ex.ToString());
                jobState.Status = ex.ToString();
                jobState.Passed = false;
                jobState.Completed = true;
                return;
            }

            jobState.Passed = true;
            jobState.Completed = true;
        }

        private Task GetNonMultiPathThread(IRingMasterRequestHandler client, CancellationToken token, int threadId)
        {
            int taskCount = 0;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    SpinWait.SpinUntil(() => taskCount < this.AsyncTaskCount || token.IsCancellationRequested);

                    var getData = client.GetData(this.nonMultiPath[this.rnd.Next(this.nonMultiPath.Count)], null)
                        .ContinueWith(t => { HandleTaskReturn(t.Exception); });

                    var getAcl = client.GetACL(this.nonMultiPath[this.rnd.Next(this.nonMultiPath.Count)], null)
                        .ContinueWith(t => { HandleTaskReturn(t.Exception); });

                    Interlocked.Add(ref taskCount, 2);
                }

                SpinWait.SpinUntil(() => taskCount <= 0);
            }
            catch (Exception ex)
            {
                this.Log(ex.ToString());
            }

            return Task.FromResult(0);

            void HandleTaskReturn(Exception exception)
            {
                Interlocked.Decrement(ref taskCount);

                if (exception != null)
                {
                    this.Log("Exception from Get request: " + exception.ToString());
                    this.IncrementTotalFailures();
                }
                else
                {
                    this.IncrementTotalDataCount();
                }
            }
        }

        private Task GetRequestThread(IRingMasterRequestHandler client, CancellationToken token, int threadId)
        {
            int taskCount = 0;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    SpinWait.SpinUntil(() => taskCount < this.AsyncTaskCount || token.IsCancellationRequested);

                    // var getSubtree = client.GetSubtree(this.targetVnetPath, $">:{MaxChildrenCount}:")
                       // .ContinueWith(t => { HandleTaskReturn(t.Exception); });
                    var getData = client.GetData(this.multiRequest[this.rnd.Next(this.multiRequest.Count)].Path, null)
                        .ContinueWith(t => { HandleTaskReturn(t.Exception); });

                    var getAcl = client.GetACL(this.multiRequest[this.rnd.Next(this.multiRequest.Count)].Path, null)
                        .ContinueWith(t => { HandleTaskReturn(t.Exception); });

                    // var get = client.GetChildren(this.targetVnetPath, null, $">:{MaxChildrenCount}:")
                       // .ContinueWith(t => { HandleTaskReturn(t.Exception); });
                    Interlocked.Add(ref taskCount, 2);
                }

                SpinWait.SpinUntil(() => taskCount <= 0);
            }
            catch (Exception ex)
            {
                this.Log(ex.ToString());
            }

            return Task.FromResult(0);

            void HandleTaskReturn(Exception exception)
            {
                Interlocked.Decrement(ref taskCount);

                if (exception != null)
                {
                    this.Log("Exception from Get request: " + exception.ToString());
                    this.IncrementTotalFailures();
                }
                else
                {
                    this.IncrementTotalDataCount();
                }
            }
        }

        private async Task<string> FindTargetVnet()
        {
            var allVnets = await this.HelperClient.GetChildren($"/{this.RootNodeName}/Instance{SendMultiInstanceId}", null, $">:{MaxChildrenCount * 4}:");
            foreach (var vnet in allVnets)
            {
                var path = $"/{this.RootNodeName}/Instance{SendMultiInstanceId}/{vnet}/mappings/v4ca";
                var stat = (await this.HelperClient.GetData(path, RequestGetData.GetDataOptions.None, null)).Stat;

                if (stat.NumChildren >= this.requestCountInMulti && this.otherVnets.Count > 3)
                {
                    this.Log($"Found target vnet: {path}, number of children: {stat.NumChildren}. Other Vnets count: {this.otherVnets.Count}");
                    return path;
                }
                else
                {
                    this.otherVnets.Add(path);
                }
            }

            throw new Exception("No suitable vnet found. please create more data");
        }

        private async Task<List<Op>> GetMultiRequest(string parent)
        {
            var ops = new List<Op>();
            var startFrom = string.Empty;

            var data = Helpers.MakeRandomData(this.rnd, this.rnd.Next(this.MinDataSize, this.MaxDataSize));

            while (true)
            {
                bool stop = false;
                var children = await this.HelperClient.GetChildren(parent, null, $">:{MaxChildrenCount}:{startFrom}");
                foreach (var child in children)
                {
                    ops.Add(Op.SetData($"{parent}/{child}", data, -1));
                    if (ops.Count >= this.requestCountInMulti)
                    {
                        stop = true;
                        startFrom = child;
                        break;
                    }

                    startFrom = child;
                }

                if (children.Count < MaxChildrenCount || stop)
                {
                    break;
                }
            }

            while (true)
            {
                var stop = false;
                var children = await this.HelperClient.GetChildren(parent, null, $">:{MaxChildrenCount}:{startFrom}");
                foreach (var child in children)
                {
                    this.nonMultiPath.Add($"{parent}/{child}");
                    if (this.nonMultiPath.Count >= 10000)
                    {
                        stop = true;
                        break;
                    }

                    startFrom = child;
                }

                if (children.Count < MaxChildrenCount || stop)
                {
                    break;
                }
            }

            return ops;
        }
    }
}
