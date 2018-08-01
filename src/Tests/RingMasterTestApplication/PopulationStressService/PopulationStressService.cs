// <copyright file="PopulationStressService.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.PopulationStressService
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
    /// Service used to stress the RingMaster by creating and deleting deep hierarchy of nodes.
    /// </summary>
    public class PopulationStressService : StatelessService
    {
        private static readonly Uri RingMasterServiceUri = new Uri("fabric:/RingMaster/RingMasterService");

        public PopulationStressService(StatelessServiceContext context, IMetricsFactory metricsFactory)
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
                ConfigurationSection populationPerformanceTestConfiguration = this.Context.CodePackageActivationContext.GetConfigurationSection("PopulationPerformanceTest");
                string connectionString = populationPerformanceTestConfiguration.GetStringValue("TargetConnectionString");
                connectionString = Helpers.GetServerAddressIfNotProvided(connectionString);

                ulong timeStreamId = populationPerformanceTestConfiguration.GetUInt64Value("TimeStream");

                while (!cancellationToken.IsCancellationRequested)
                {
                    using (var ringMaster = new RetriableRingMasterClient(s => Helpers.CreateRingMasterTimeStreamRequestHandler(s, cancellationToken, timeStreamId), connectionString))
                    {
                        await this.CreateAndDeleteHierarchyTest(ringMaster, populationPerformanceTestConfiguration, cancellationToken);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                PopulationStressServiceEventSource.Log.RunAsyncFailed(ex.ToString());
                throw;
            }
            finally
            {
                PopulationStressServiceEventSource.Log.Terminated((long)uptime.Elapsed.TotalSeconds);
            }
        }

        private static IRingMasterRequestHandler ConnectToRingMaster(string connectionString, CancellationToken cancellationToken)
        {
            var configuration = new RingMasterClient.Configuration();
            return new RingMasterClient(connectionString, configuration, null, cancellationToken);
        }

        private async Task CreateAndDeleteHierarchyTest(IRingMasterRequestHandler ringMaster, ConfigurationSection config, CancellationToken cancellationToken)
        {
            try
            {
                string testPath = config.GetStringValue("CreateAndDeleteHierarchy.TestPath");
                int maxNodes = config.GetIntValue("CreateAndDeleteHierarchy.MaxNodes");
                bool useScheduledDelete = config.GetBoolValue("CreateAndDeleteHierarchy.UseScheduledDelete");

                await ringMaster.Create(testPath, null, null, CreateMode.PersistentAllowPathCreation, throwIfNodeExists: false);

                while (!cancellationToken.IsCancellationRequested)
                {
                    var createInstrumentation = new CreatePerformanceInstrumentation(this.MetricsFactory);
                    int maxConcurrentCreateBatches = config.GetIntValue("Create.MaxConcurrentBatches");
                    int createBatchLength = config.GetIntValue("Create.BatchLength");

                    var createPerformanceTest = new CreatePerformance(createInstrumentation, maxConcurrentCreateBatches, cancellationToken);
                    createPerformanceTest.MinChildrenCountPerNode = config.GetIntValue("Create.MinChildrenCountPerNode");
                    createPerformanceTest.MaxChildrenCountPerNode = config.GetIntValue("Create.MaxChildrenCountPerNode");
                    createPerformanceTest.MinDataSizePerNode = config.GetIntValue("Create.MinDataSizePerNode");
                    createPerformanceTest.MaxDataSizePerNode = config.GetIntValue("Create.MaxDataSizePerNode");
                    createPerformanceTest.MaxNodeNameLength = config.GetIntValue("Create.MaxNodeNameLength");

                    PopulationStressServiceEventSource.Log.CreateAndDeleteHierarchyCreateStarted(testPath, maxNodes, createBatchLength);

                    createPerformanceTest.CreateHierarchy(ringMaster, testPath, createBatchLength, maxNodes);

                    int maxConcurrentDeleteBatches = config.GetIntValue("Delete.MaxConcurrentBatches");
                    int deleteBatchLength = config.GetIntValue("Delete.BatchLength");

                    var deleteInstrumentation = new DeletePerformanceInstrumentation(this.MetricsFactory);
                    var deletePerformanceTest = new DeletePerformance(deleteInstrumentation, maxConcurrentDeleteBatches, cancellationToken);

                    PopulationStressServiceEventSource.Log.CreateAndDeleteHierarchyDeleteStarted(testPath, maxNodes, deleteBatchLength, useScheduledDelete);

                    if (useScheduledDelete)
                    {
                        deletePerformanceTest.ScheduledDelete(ringMaster, testPath);
                    }
                    else
                    {
                        await deletePerformanceTest.LoadNodes(ringMaster, testPath, maxNodes);
                        deletePerformanceTest.QueueDeletes(ringMaster, deleteBatchLength);
                    }
                }

                PopulationStressServiceEventSource.Log.CreateAndDeleteHierarchyTestCompleted();
            }
            catch (Exception ex)
            {
                PopulationStressServiceEventSource.Log.CreateAndDeleteHierarchyTestFailed(ex.ToString());
            }
        }

        private sealed class CreatePerformanceInstrumentation : CreatePerformance.IInstrumentation
        {
            private readonly IMetric0D createSuccess;
            private readonly IMetric0D createFailure;
            private readonly IMetric0D createLatencyMs;
            private readonly IMetric0D createNodeCount;

            public CreatePerformanceInstrumentation(IMetricsFactory metricsFactory)
            {
                this.createSuccess = metricsFactory.Create0D("createSuccess");
                this.createFailure = metricsFactory.Create0D("createFailure");
                this.createLatencyMs = metricsFactory.Create0D("createLatencyMs");
                this.createNodeCount = metricsFactory.Create0D("createNodeCount");
            }

            public void CreateMultiFailed(int failureCount)
            {
                this.createFailure.LogValue(failureCount);
            }

            public void CreateMultiSucceeded(int successCount, TimeSpan elapsed)
            {
                this.createSuccess.LogValue(successCount);
                this.createLatencyMs.LogValue((long)elapsed.TotalMilliseconds);
            }

            public void NodeQueuedForCreate(int nodeCount)
            {
                this.createNodeCount.LogValue(nodeCount);
            }
        }

        private sealed class DeletePerformanceInstrumentation : DeletePerformance.IInstrumentation
        {
            private readonly IMetric0D deleteSuccess;
            private readonly IMetric0D deleteFailure;
            private readonly IMetric0D deleteLatencyMs;
            private readonly IMetric0D deleteNodeCount;

            public DeletePerformanceInstrumentation(IMetricsFactory metricsFactory)
            {
                this.deleteSuccess = metricsFactory.Create0D("deleteSuccess");
                this.deleteFailure = metricsFactory.Create0D("deleteFailure");
                this.deleteLatencyMs = metricsFactory.Create0D("deleteLatencyMs");
                this.deleteNodeCount = metricsFactory.Create0D("deleteNodeCount");
            }

            public void DeleteMultiFailed(int failureCount)
            {
                this.deleteFailure.LogValue(failureCount);
            }

            public void DeleteMultiSucceeded(int successCount, TimeSpan latency)
            {
                this.deleteSuccess.LogValue(successCount);
                this.deleteLatencyMs.LogValue((long)latency.TotalMilliseconds);
            }

            public void NodeLoaded(int nodeCount)
            {
            }

            public void NodeQueuedForDelete(int nodeCount)
            {
                this.deleteNodeCount.LogValue(nodeCount);
            }
        }
    }
}
