// <copyright file="EnumerationStressService.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.EnumerationStressService
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Fabric;
    using System.Fabric.Description;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Instrumentation;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Performance;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.ServiceFabric;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport;
    using Microsoft.ServiceFabric.Services.Runtime;

    /// <summary>
    /// Service used to stress the RingMaster by enumerating random subsets of children of a given node.
    /// </summary>
    public class EnumerationStressService : StatelessService
    {
        private static readonly Uri RingMasterServiceUri = new Uri("fabric:/RingMaster/RingMasterService");

        public EnumerationStressService(StatelessServiceContext context, IMetricsFactory metricsFactory)
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
                ConfigurationSection enumerationPerformanceTestConfiguration = this.Context.CodePackageActivationContext.GetConfigurationSection("EnumerationPerformanceTest");
                string connectionString = enumerationPerformanceTestConfiguration.GetStringValue("TargetConnectionString");
                ulong timeStreamId = enumerationPerformanceTestConfiguration.GetUInt64Value("TimeStream");

                while (!cancellationToken.IsCancellationRequested)
                {
                    using (var ringMaster = ConnectToRingMaster(connectionString, cancellationToken))
                    using (var timeStream = ringMaster.OpenTimeStream(timeStreamId))
                    {
                        await Task.Run(() => this.GetChildrenPerformanceTest(timeStream, enumerationPerformanceTestConfiguration, cancellationToken));
                    }

                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                EnumerationStressServiceEventSource.Log.RunAsyncFailed(ex.ToString());
                throw;
            }
            finally
            {
                EnumerationStressServiceEventSource.Log.Terminated((long)uptime.Elapsed.TotalSeconds);
            }
        }

        private static IRingMasterRequestHandler ConnectToRingMaster(string connectionString, CancellationToken cancellationToken)
        {
            var configuration = new RingMasterClient.Configuration();
            return new RingMasterClient(connectionString, configuration, null, cancellationToken);
        }

        private void GetChildrenPerformanceTest(IRingMasterRequestHandler ringMaster, ConfigurationSection config, CancellationToken cancellationToken)
        {
            string testPath = config.GetStringValue("TestPath");
            int maxChildren = config.GetIntValue("MaxChildren");
            int maxConcurrentRequests = config.GetIntValue("MaxConcurrentRequests");

            var instrumentation = new GetChildrenPerformanceInstrumentation(this.MetricsFactory);
            var getChildrenPerformanceTest = new GetChildrenPerformance(instrumentation, maxConcurrentRequests, cancellationToken);

            EnumerationStressServiceEventSource.Log.GetChildrenPerformanceTestStarted(testPath, maxChildren, maxConcurrentRequests);
            getChildrenPerformanceTest.QueueRequests(ringMaster, testPath, maxChildren);
        }

        private sealed class GetChildrenPerformanceInstrumentation : GetChildrenPerformance.IInstrumentation
        {
            private readonly IMetric0D getChildrenFailure;
            private readonly IMetric0D getChildrenSuccess;
            private readonly IMetric0D getChildrenResultCount;
            private readonly IMetric0D getChildrenLatencyMs;

            public GetChildrenPerformanceInstrumentation(IMetricsFactory metricsFactory)
            {
                this.getChildrenFailure = metricsFactory.Create0D("getChildrenFailure");
                this.getChildrenSuccess = metricsFactory.Create0D("getChildrenSuccess");
                this.getChildrenResultCount = metricsFactory.Create0D("getChildrenResultCount");
                this.getChildrenLatencyMs = metricsFactory.Create0D("getChildrenLatencyMs");
            }

            public void GetChildrenFailed(string nodePath)
            {
                this.getChildrenFailure.LogValue(1);
            }

            public void GetChildrenSucceeded(string nodePath, int childrenCount, TimeSpan elapsed)
            {
                this.getChildrenSuccess.LogValue(1);
                this.getChildrenResultCount.LogValue(childrenCount);
                this.getChildrenLatencyMs.LogValue((long)elapsed.TotalMilliseconds);
            }
        }
    }
}
