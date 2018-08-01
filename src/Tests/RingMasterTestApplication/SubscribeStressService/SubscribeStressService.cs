// <copyright file="SubscribeStressService.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.SubscribeStressService
{
    using System;
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
    using Microsoft.Vega.Test.Helpers;

    /// <summary>
    /// Service used to stress the RingMaster by installing lots of watchers.
    /// </summary>
    public class SubscribeStressService : StatelessService
    {
        private static readonly Uri RingMasterServiceUri = new Uri("fabric:/RingMaster/RingMasterService");

        public SubscribeStressService(StatelessServiceContext context, IMetricsFactory metricsFactory)
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
                ConfigurationSection subscribePerformanceTestConfiguration = this.Context.CodePackageActivationContext.GetConfigurationSection("SubscribePerformanceTest");
                string connectionString = subscribePerformanceTestConfiguration.GetStringValue("TargetConnectionString");
                connectionString = Helpers.GetServerAddressIfNotProvided(connectionString);

                ulong timeStreamId = subscribePerformanceTestConfiguration.GetUInt64Value("TimeStream");

                while (!cancellationToken.IsCancellationRequested)
                {
                    using (var ringMaster = new RetriableRingMasterClient(s => Helpers.CreateRingMasterTimeStreamRequestHandler(s, cancellationToken, timeStreamId), connectionString))
                    {
                        await this.SubscribePerformanceTest(ringMaster, subscribePerformanceTestConfiguration, cancellationToken);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                SubscribeStressServiceEventSource.Log.RunAsyncFailed(ex.ToString());
                throw;
            }
            finally
            {
                SubscribeStressServiceEventSource.Log.Terminated((long)uptime.Elapsed.TotalSeconds);
            }
        }

        private static IRingMasterRequestHandler ConnectToRingMaster(string connectionString, CancellationToken cancellationToken)
        {
            var configuration = new RingMasterClient.Configuration();
            return new RingMasterClient(connectionString, configuration, null, cancellationToken);
        }

        /// <summary>
        /// Measures the performance of Watchers.
        /// </summary>
        /// <param name="ringMaster">RingMaster client</param>
        /// <param name="config">Configuration section</param>
        /// <param name="cancellationToken">Token that must be observed for cancellation signal</param>
        /// <returns>Task that tracks execution of this test</returns>
        private async Task SubscribePerformanceTest(IRingMasterRequestHandler ringMaster, ConfigurationSection config, CancellationToken cancellationToken)
        {
            string testPath = config.GetStringValue("TestPath");
            int maxNodes = config.GetIntValue("MaxNodesToLoad");
            int maxGetChildrenEnumerationCount = config.GetIntValue("MaxChildrenEnumerationCount");
            int maxConcurrentWatchers = config.GetIntValue("MaxConcurrentWatchers");
            int maxConcurrency = 16;

            SubscribeStressServiceEventSource.Log.WatcherPerformanceTestStarted(testPath, maxNodes, maxConcurrentWatchers);
            var instrumentation = new WatcherPerformanceInstrumentation(this.MetricsFactory);
            var watcherPerformanceTest = new BulkWatcherPerformance(instrumentation, maxConcurrency, cancellationToken);

            await watcherPerformanceTest.LoadNodes(ringMaster, testPath, maxNodes, maxGetChildrenEnumerationCount);

            await Task.Run(() => watcherPerformanceTest.SetWatchers(ringMaster));
        }

        private sealed class WatcherPerformanceInstrumentation : BulkWatcherPerformance.IInstrumentation
        {
            private readonly IMetric0D nodesLoaded;
            private readonly IMetric0D setWatcherFailed;
            private readonly IMetric0D setWatcherSucceeded;
            private readonly IMetric0D setWatcherLatencyMs;
            private readonly IMetric1D watcherNotified;

            public WatcherPerformanceInstrumentation(IMetricsFactory metricsFactory)
            {
                this.nodesLoaded = metricsFactory.Create0D("watcherPerformanceTestNodesLoaded");
                this.setWatcherFailed = metricsFactory.Create0D("setWatcherFailed");
                this.setWatcherSucceeded = metricsFactory.Create0D("setWatcherSucceded");
                this.setWatcherLatencyMs = metricsFactory.Create0D("setWatcherLatencyMs");
                this.watcherNotified = metricsFactory.Create1D("watcherNotified", "notificationType");
            }

            public void NodeLoaded(int nodeCount)
            {
                this.nodesLoaded.LogValue(nodeCount);
            }

            public void SetWatcherFailed()
            {
                this.setWatcherFailed.LogValue(1);
            }

            public void SetWatcherSucceeded(TimeSpan latency)
            {
                this.setWatcherSucceeded.LogValue(1);
                this.setWatcherLatencyMs.LogValue((long)latency.TotalMilliseconds);
            }

            public void WatcherNotified(string notificationType, TimeSpan watchDuration)
            {
                this.watcherNotified.LogValue(1, notificationType);
            }
        }
    }
}
