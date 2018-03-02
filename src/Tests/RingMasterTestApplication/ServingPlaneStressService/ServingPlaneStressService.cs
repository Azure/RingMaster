// <copyright file="ServingPlaneStressService.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ServingPlaneStressService
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Fabric;
    using System.Fabric.Description;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Instrumentation;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Performance;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.ServiceFabric;
    using Microsoft.ServiceFabric.Services.Runtime;

    /// <summary>
    /// Service used to stress the Serving Plane of RingMaster.
    /// </summary>
    public class ServingPlaneStressService : StatelessService
    {
        private static readonly Uri RingMasterServiceUri = new Uri("fabric:/RingMaster/RingMasterService");

        public ServingPlaneStressService(StatelessServiceContext context, IMetricsFactory metricsFactory)
            : base(context)
        {
            this.MetricsFactory = metricsFactory;
        }

        public IMetricsFactory MetricsFactory { get; private set; }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var uptime = Stopwatch.StartNew();
            try
            {
                ConfigurationSection getDataPerformanceTestConfiguration = this.Context.CodePackageActivationContext.GetConfigurationSection("GetDataPerformanceTest");
                string connectionString = getDataPerformanceTestConfiguration.GetStringValue("TargetConnectionString");
                ulong timeStreamId = getDataPerformanceTestConfiguration.GetUInt64Value("TimeStream");

                while (!cancellationToken.IsCancellationRequested)
                {
                    using (var ringMaster = ConnectToRingMaster(connectionString, cancellationToken))
                    using (var timeStream = ringMaster.OpenTimeStream(timeStreamId))
                    {
                        await this.GetDataPerformanceTest(timeStream, getDataPerformanceTestConfiguration, cancellationToken);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                ServingPlaneStressServiceEventSource.Log.RunAsyncFailed(ex.ToString());
                throw;
            }
            finally
            {
                ServingPlaneStressServiceEventSource.Log.Terminated((long)uptime.Elapsed.TotalSeconds);
            }
        }

        private static IRingMasterRequestHandler ConnectToRingMaster(string connectionString, CancellationToken cancellationToken)
        {
            var configuration = new RingMasterClient.Configuration();
            return new RingMasterClient(connectionString, configuration, null, cancellationToken);
        }

        private async Task GetDataPerformanceTest(IRingMasterRequestHandler ringMaster, ConfigurationSection config, CancellationToken cancellationToken)
        {
            try
            {
                string testRootPath = config.GetStringValue("TestPath");
                int maxConcurrentGetDataBatches = config.GetIntValue("MaxConcurrentBatches");
                int batchLength = config.GetIntValue("BatchLength");
                int maxNodes = config.GetIntValue("MaxNodesToLoad");

                var instrumentation = new GetDataPerformanceInstrumentation(this.MetricsFactory);
                var getDataPerformanceTest = new GetDataPerformance(instrumentation, maxConcurrentGetDataBatches, cancellationToken);
                await getDataPerformanceTest.LoadNodes(ringMaster, testRootPath, maxNodes);

                ringMaster.Timeout = 100;
                ServingPlaneStressServiceEventSource.Log.GetDataPerformanceTestStarted(testRootPath, maxNodes, batchLength);
                await Task.Run(() => getDataPerformanceTest.QueueBatches(ringMaster, batchLength));

                ServingPlaneStressServiceEventSource.Log.GetDataPerformanceTestCompleted();
            }
            catch (Exception ex)
            {
                ServingPlaneStressServiceEventSource.Log.GetDataPerformanceTestFailed(ex.ToString());
            }
        }

        private sealed class GetDataPerformanceInstrumentation : GetDataPerformance.IInstrumentation
        {
            private readonly IMetric0D getDataSuccess;
            private readonly IMetric0D getDataFailure;
            private readonly IMetric0D getDataLatency;
            private readonly IMetric0D getDataNodesLoaded;

            public GetDataPerformanceInstrumentation(IMetricsFactory metricsFactory)
            {
                this.getDataSuccess = metricsFactory.Create0D("getDataSuccess");
                this.getDataFailure = metricsFactory.Create0D("getDataFailure");
                this.getDataLatency = metricsFactory.Create0D("getDataLatencyMs");
                this.getDataNodesLoaded = metricsFactory.Create0D("getDataNodesLoaded");
            }

            public void BatchFailed(int batchLength)
            {
                this.getDataFailure.LogValue(batchLength);
            }

            public void BatchProcessed(TimeSpan latency, int batchLength, int successCount, int failureCount)
            {
                this.getDataSuccess.LogValue(successCount);
                if (failureCount > 0)
                {
                    this.getDataFailure.LogValue(failureCount);
                }

                this.getDataLatency.LogValue((long)latency.TotalMilliseconds);
            }

            public void NodeLoaded(int nodeCount)
            {
                this.getDataNodesLoaded.LogValue(nodeCount);
            }
        }
    }
}
