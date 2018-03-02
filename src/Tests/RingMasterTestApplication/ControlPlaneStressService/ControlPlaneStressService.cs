// <copyright file="ControlPlaneStressService.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ControlPlaneStressService
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
    /// Service used to stress the Control Plane of RingMaster.
    /// </summary>
    public class ControlPlaneStressService : StatelessService
    {
        private static readonly Uri RingMasterServiceUri = new Uri("fabric:/RingMaster/RingMasterService");

        public ControlPlaneStressService(StatelessServiceContext context, IMetricsFactory metricsFactory)
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
                ConfigurationSection setDataPerformanceTestConfiguration = this.Context.CodePackageActivationContext.GetConfigurationSection("SetDataPerformanceTest");
                string connectionString = setDataPerformanceTestConfiguration.GetStringValue("TargetConnectionString");
                ulong timeStreamId = setDataPerformanceTestConfiguration.GetUInt64Value("TimeStream");

                while (!cancellationToken.IsCancellationRequested)
                {
                    using (var ringMaster = ConnectToRingMaster(connectionString, cancellationToken))
                    using (var timeStream = ringMaster.OpenTimeStream(timeStreamId))
                    {
                        await this.SetDataPerformanceTest(timeStream, setDataPerformanceTestConfiguration, cancellationToken);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                ControlPlaneStressServiceEventSource.Log.RunAsyncFailed(ex.ToString());
                throw;
            }
            finally
            {
                ControlPlaneStressServiceEventSource.Log.Terminated((long)uptime.Elapsed.TotalSeconds);
            }
        }

        private static IRingMasterRequestHandler ConnectToRingMaster(string connectionString, CancellationToken cancellationToken)
        {
            var configuration = new RingMasterClient.Configuration();
            return new RingMasterClient(connectionString, configuration, null, cancellationToken);
        }

        private async Task SetDataPerformanceTest(IRingMasterRequestHandler ringMasterClient, ConfigurationSection config, CancellationToken cancellationToken)
        {
            try
            {
                string testRootPath = config.GetStringValue("TestPath");
                int maxConcurrentSetDataBatches = config.GetIntValue("MaxConcurrentBatches");
                int maxNodes = config.GetIntValue("MaxNodesToLoad");
                int batchLength = config.GetIntValue("BatchLength");

                ControlPlaneStressServiceEventSource.Log.SetDataPerformanceTestStarted(testRootPath, maxNodes, batchLength);
                var instrumentation = new SetDataPerformanceInstrumentation(this.MetricsFactory);
                var setDataPerformanceTest = new SetDataPerformance(instrumentation, maxConcurrentSetDataBatches, cancellationToken);

                setDataPerformanceTest.MinDataSizePerNode = config.GetIntValue("MinDataSizePerNode");
                setDataPerformanceTest.MaxDataSizePerNode = config.GetIntValue("MaxDataSizePerNode");

                await setDataPerformanceTest.LoadNodes(ringMasterClient, testRootPath, maxNodes);
                await Task.Run(() => setDataPerformanceTest.QueueRequests(ringMasterClient, batchLength));

                ControlPlaneStressServiceEventSource.Log.SetDataPerformanceTestCompleted();
            }
            catch (Exception ex)
            {
                ControlPlaneStressServiceEventSource.Log.SetDataPerformanceTestFailed(ex.ToString());
            }
        }

        private sealed class SetDataPerformanceInstrumentation : SetDataPerformance.IInstrumentation
        {
            private readonly IMetric0D setDataSuccess;
            private readonly IMetric0D setDataFailure;
            private readonly IMetric0D setDataLatency;
            private readonly IMetric0D setDataNodesLoaded;

            public SetDataPerformanceInstrumentation(IMetricsFactory metricsFactory)
            {
                this.setDataSuccess = metricsFactory.Create0D("setDataSuccess");
                this.setDataFailure = metricsFactory.Create0D("setDataFailure");
                this.setDataLatency = metricsFactory.Create0D("setDataLatencyMs");
                this.setDataNodesLoaded = metricsFactory.Create0D("setDataNodesLoaded");
            }

            public void NodeLoaded(int nodeCount)
            {
                this.setDataNodesLoaded.LogValue(nodeCount);
            }

            public void SetDataMultiFailed(int failureCount)
            {
                this.setDataFailure.LogValue(failureCount);
            }

            public void SetDataMultiSucceeded(int successCount, TimeSpan latency)
            {
                this.setDataSuccess.LogValue(successCount);
                this.setDataLatency.LogValue((long)latency.TotalMilliseconds);
            }
        }
    }
}
