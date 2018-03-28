// <copyright file="RingMasterWatchdog.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterWatchdog
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Fabric;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Instrumentation;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.ServiceFabric;
    using Microsoft.ServiceFabric.Services.Runtime;

    /// <summary>
    /// RingMaster service
    /// </summary>
    public sealed class RingMasterWatchdog : StatelessService
    {
        private const int DefaultMaxNodeDataLength = 1024;
        private const string ServiceEndpointName = "ServiceEndpoint";
        private static readonly Uri RingMasterServiceUri = new Uri("fabric:/RingMaster/RingMasterService");

        private readonly IMetric1D ringMasterWatchdogTestSucceeded;

        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterWatchdog"/> class.
        /// </summary>
        /// <param name="context">Service context</param>
        /// <param name="metricsFactory">Metric factory for MDM</param>
        public RingMasterWatchdog(StatelessServiceContext context, IMetricsFactory metricsFactory)
            : base(context)
        {
            if (metricsFactory == null)
            {
                throw new ArgumentNullException(nameof(metricsFactory));
            }

            System.Diagnostics.Contracts.Contract.EndContractBlock();

            this.ringMasterWatchdogTestSucceeded = metricsFactory.Create1D(nameof(this.ringMasterWatchdogTestSucceeded), "testName");
        }

        /// <inheritdoc />
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var uptime = Stopwatch.StartNew();
            try
            {
                RingMasterWatchdogEventSource.Log.RunAsync();

                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                string version = fvi.FileVersion;

                string instanceRootPath = $"/$watchdogs/RingMasterWatchdog/{this.Context.ReplicaOrInstanceId}";

                long iteration = 0;
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        using (var ringMaster = await RingMasterWatchdog.ConnectToRingMaster(iteration, cancellationToken))
                        {
                            if (ringMaster != null && await this.TestRingMasterFunctionality(ringMaster, instanceRootPath, iteration))
                            {
                                this.ringMasterWatchdogTestSucceeded.LogValue(1, "RingMasterFunctionality");
                            }
                        }
                    }
                    catch (FabricTransientException ex)
                    {
                        RingMasterWatchdogEventSource.Log.RunAsync_TransientException(iteration, ex.ToString());
                    }

                    iteration++;
                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                RingMasterWatchdogEventSource.Log.RunAsyncFailed(ex.ToString());
                throw;
            }
            finally
            {
                RingMasterWatchdogEventSource.Log.RunAsyncCompleted((long)uptime.Elapsed.TotalSeconds);
            }
        }

        private static async Task<IRingMasterRequestHandler> ConnectToRingMaster(long iteration, CancellationToken cancellationToken)
        {
            using (var serviceDiscovery = new ServiceDiscovery())
            {
                IReadOnlyList<Uri> endpoints = await serviceDiscovery.GetServiceEndpoints(RingMasterServiceUri, ServiceEndpointName);

                if (endpoints.Count == 0)
                {
                    return null;
                }

                var configuration = new RingMasterClient.Configuration();
                string connectionString = $"{endpoints[0].Host}:{endpoints[0].Port}";
                RingMasterWatchdogEventSource.Log.ConnectToRingMaster(iteration, connectionString);
                return new RingMasterClient(connectionString, configuration, null, cancellationToken);
            }
        }

        /// <summary>
        /// Tests basic ringmaster functionality
        /// </summary>
        /// <param name="ringMaster">RingMaster client</param>
        /// <param name="instanceRootPath">Root path that must be used by this instance for creating nodes</param>
        /// <param name="iteration">Current iteration</param>
        /// <returns><c>true</c> if the functionality test passed, <c>false</c> otherwise</returns>
        private async Task<bool> TestRingMasterFunctionality(IRingMasterRequestHandler ringMaster, string instanceRootPath, long iteration)
        {
            var timer = Stopwatch.StartNew();
            try
            {
                var random = new Random();

                string nodePath = string.Format($"{instanceRootPath}/Node");
                RingMasterWatchdogEventSource.Log.Create(iteration, nodePath);
                await ringMaster.Create(nodePath, null, null, CreateMode.PersistentAllowPathCreation, throwIfNodeExists: false);

                RingMasterWatchdogEventSource.Log.Exists(iteration, nodePath);
                var nodeStat = await ringMaster.Exists(nodePath, watcher: null);

                int nodeDataLength = random.Next(RingMasterWatchdog.DefaultMaxNodeDataLength);
                byte[] nodeData = new byte[nodeDataLength];
                random.NextBytes(nodeData);

                RingMasterWatchdogEventSource.Log.SetData(iteration, nodePath, nodeData.Length);
                await ringMaster.SetData(nodePath, nodeData, nodeStat.Version);

                RingMasterWatchdogEventSource.Log.GetData(iteration, nodePath);
                var retrievedData = await ringMaster.GetData(nodePath, watcher: null);

                if (retrievedData == null)
                {
                    RingMasterWatchdogEventSource.Log.GetDataFailed_RetrievedDataIsNull(iteration, nodePath, nodeData.Length);
                    throw new InvalidOperationException($"Node {nodePath}: Retrieved data is null. expectedDataLength={nodeData.Length}");
                }

                if (retrievedData.Length != nodeData.Length)
                {
                    RingMasterWatchdogEventSource.Log.GetDataFailed_RetrievedDataLengthMismatch(iteration, nodePath, nodeData.Length, retrievedData.Length);
                    throw new InvalidOperationException($"Node {nodePath}: Retrieved data length mismatch retrievedDataLength={retrievedData.Length} expectedDataLength={nodeData.Length}");
                }

                if (!retrievedData.SequenceEqual(nodeData))
                {
                    RingMasterWatchdogEventSource.Log.GetDataFailed_RetrievedDataIsDifferent(iteration, nodePath, nodeData.Length);
                    throw new InvalidOperationException($"Node {nodePath}: Retrieved data is different");
                }

                RingMasterWatchdogEventSource.Log.Delete(iteration, nodePath, nodeStat.Version);
                await ringMaster.Delete(nodePath, -1);

                RingMasterWatchdogEventSource.Log.TestRingMasterFunctionalitySucceeded(iteration, timer.ElapsedMilliseconds);
                return true;
            }
            catch (System.Exception ex)
            {
                RingMasterWatchdogEventSource.Log.TestRingMasterFunctionalityFailed(iteration, timer.ElapsedMilliseconds, ex.ToString());
            }

            return false;
        }
    }
}
