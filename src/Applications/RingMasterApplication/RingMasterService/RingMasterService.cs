// <copyright file="RingMasterService.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterService
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.Fabric;
    using System.Fabric.Description;
    using System.Net;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Instrumentation;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Instrumentation;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.ServiceFabric;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;

    using IRingMasterServerInstrumentation = Microsoft.Azure.Networking.Infrastructure.RingMaster.Server.IRingMasterServerInstrumentation;
    using IZooKeeperServerInstrumentation = Microsoft.Azure.Networking.Infrastructure.RingMaster.Server.ZooKeeper.IZooKeeperServerInstrumentation;
    using RingMasterBackendCoreInstrumentation = Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.RingMasterServerInstrumentation;
    using RingMasterServerInstrumentation = Microsoft.Azure.Networking.Infrastructure.RingMaster.Server.RingMasterServerInstrumentation;
    using ZooKeeperServerInstrumentation = Microsoft.Azure.Networking.Infrastructure.RingMaster.Server.ZooKeeper.ZooKeeperServerInstrumentation;

    /// <summary>
    /// RingMaster service
    /// </summary>
    public sealed class RingMasterService : StatefulService, IDisposable
    {
        private readonly CancellationTokenSource cancellationSource = new CancellationTokenSource();
        private readonly IZooKeeperServerInstrumentation zooKeeperServerInstrumentation;
        private readonly IRingMasterServerInstrumentation ringMasterServerInstrumentation;
        private readonly PersistedDataFactory factory;
        private readonly RingMasterBackendCore backend;
        private readonly RingMasterRequestExecutor executor;
        private ushort port = 98;
        private ushort zkprPort = 100;

        private ushort readOnlyPort = 88;

        public RingMasterService(StatefulServiceContext context, IMetricsFactory ringMasterMetricsFactory, IMetricsFactory persistenceMetricsFactory)
            : base(context, PersistedDataFactory.CreateStateManager(context))
        {
            RingMasterBackendCore.GetSettingFunction = GetSetting;
            string factoryName = $"{this.Context.ServiceTypeName}-{this.Context.ReplicaId}-{this.Context.NodeContext.NodeName}";
            this.zooKeeperServerInstrumentation = new ZooKeeperServerInstrumentation(ringMasterMetricsFactory);
            this.ringMasterServerInstrumentation = new RingMasterServerInstrumentation(ringMasterMetricsFactory);

            var ringMasterInstrumentation = new RingMasterBackendInstrumentation(ringMasterMetricsFactory);
            var executorInstrumentation = new ExecutorInstrumentation(ringMasterMetricsFactory);
            var persistenceInstrumentation = new ServiceFabricPersistenceInstrumentation(persistenceMetricsFactory);

            RingMasterBackendCoreInstrumentation.Instance = ringMasterInstrumentation;

            var persistenceConfiguration = new PersistedDataFactory.Configuration
            {
                EnableActiveSecondary = true
            };

            this.factory = new PersistedDataFactory(this.StateManager, factoryName, persistenceConfiguration, persistenceInstrumentation, this.cancellationSource.Token);
            this.backend = new RingMasterBackendCore(this.factory);

            this.factory.SetBackend(this.backend);

            var executorConfiguration = new RingMasterRequestExecutor.Configuration
            {
                DefaultRequestTimeout = TimeSpan.FromMilliseconds(2500)
            };

            RingMasterRequestExecutor.TraceLevel = TraceLevel.Info; // Change this to Verbose for debugging.

            this.executor = new RingMasterRequestExecutor(this.backend, executorConfiguration, executorInstrumentation, this.cancellationSource.Token);
        }

        public void Dispose()
        {
            this.backend.Dispose();
            this.executor.Dispose();
            this.factory.Dispose();
            this.cancellationSource.Dispose();
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var uptime = Stopwatch.StartNew();
            try
            {
                RingMasterServiceEventSource.Log.RunAsync();

                this.executor.Start(threadCount: Environment.ProcessorCount * 2);
                this.backend.Start();
                this.backend.OnBecomePrimary();

                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                string version = fvi.FileVersion;

                while (!cancellationToken.IsCancellationRequested)
                {
                    int totalSessionCount = 0;
                    this.factory.ReportStatus();
                    RingMasterServiceEventSource.Log.ReportServiceStatus(version, (long)uptime.Elapsed.TotalSeconds, totalSessionCount);
                    RingMasterBackendCoreInstrumentation.Instance.OnUpdateStatus(version, uptime.Elapsed, true, totalSessionCount);

                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {

            }
            catch (Exception ex)
            {
                RingMasterServiceEventSource.Log.RunAsyncFailed(ex.ToString());
                throw;
            }
            finally
            {
                RingMasterServiceEventSource.Log.RunAsyncCompleted(uptime.ElapsedMilliseconds);
                this.backend.OnPrimaryStatusLost();
                Environment.Exit(0);
            }
        }

        protected override Task OnCloseAsync(CancellationToken cancellationToken)
        {
            try
            {
                RingMasterServiceEventSource.Log.OnCloseAsync();
                this.cancellationSource.Cancel();
                this.backend.Stop();
                this.executor.Stop();
            }
            catch (Exception ex)
            {
                RingMasterServiceEventSource.Log.OnCloseAsyncFailed(ex.ToString());
            }

            return Task.FromResult(0);
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[]
            {
                new ServiceReplicaListener(this.CreateListener, "ServiceEndpoint", listenOnSecondary: false),
                new ServiceReplicaListener(this.CreateZkprListener, "ZkprServiceEndpoint", listenOnSecondary: false),
            };
        }

        protected override Task OnChangeRoleAsync(ReplicaRole newRole, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// Converts a hostname into ip-address
        /// </summary>
        /// <param name="host">hostname that needs to be converted</param>
        /// <returns>an ip-address in s tring format</returns>
        private static string GetHostIp(string host)
        {
            IPAddress ip;
            if (IPAddress.TryParse(host, out ip))
            {
                return ip.ToString();
            }

            IPAddress[] ips = Dns.GetHostAddresses(host);
            foreach (IPAddress ipAddress in ips)
            {
                if (ipAddress.AddressFamily != AddressFamily.InterNetwork)
                {
                    continue;
                }

                ip = ipAddress;
                return ip.ToString();
            }

            return null;
        }

        private static string GetSetting(string settingName)
        {
            string returnedValue = ConfigurationManager.AppSettings[settingName];
            RingMasterServiceEventSource.Log.RingMaster_GetSetting(settingName, returnedValue);
            return returnedValue;
        }

        private ICommunicationListener CreateListener(StatefulServiceContext context)
        {
            // Partition replica's URL is the node's IP, port, PartitionId, ReplicaId, Guid
            EndpointResourceDescription internalEndpoint = null;
            EndpointProtocol protocol = EndpointProtocol.Tcp;

            try
            {
                internalEndpoint = context.CodePackageActivationContext.GetEndpoint("ServiceEndpoint");
                this.port = Convert.ToUInt16(internalEndpoint.Port);

                internalEndpoint = context.CodePackageActivationContext.GetEndpoint("ReadOnlyEndpoint");
                this.readOnlyPort = Convert.ToUInt16(internalEndpoint.Port);

                protocol = internalEndpoint.Protocol;

                RingMasterServiceEventSource.Log.CreateListener("RingMasterProtocol", this.port, this.readOnlyPort);
            }
            catch (Exception ex)
            {
                RingMasterServiceEventSource.Log.CreateListener_GetEndpointFailed(string.Format("Listener:{0}, Exception:{1}", "RingMasterProtocol", ex.ToString()));
                throw;
            }

            string uri = $"{protocol}://+:{this.port}/";

            string nodeIp = context.NodeContext.IPAddressOrFQDN;

            if (nodeIp.Equals("LocalHost", StringComparison.InvariantCultureIgnoreCase))
            {
                nodeIp = GetHostIp(Dns.GetHostName());
            }

            uri = uri.Replace("+", nodeIp);
            return new TcpCommunicationListener(this.port, uri, this.executor, this.ringMasterServerInstrumentation, new RingMasterCommunicationProtocol(), RingMasterCommunicationProtocol.MaximumSupportedVersion);
        }

        private ICommunicationListener CreateZkprListener(StatefulServiceContext context)
        {
            // Partition replica's URL is the node's IP, port, PartitionId, ReplicaId, Guid
            EndpointResourceDescription internalEndpoint = null;
            EndpointProtocol protocol = EndpointProtocol.Tcp;

            try
            {
                internalEndpoint = context.CodePackageActivationContext.GetEndpoint("ZkprServiceEndpoint");
                this.zkprPort = Convert.ToUInt16(internalEndpoint.Port);

                protocol = internalEndpoint.Protocol;

                RingMasterServiceEventSource.Log.CreateListener("ZookeeperProtocol", this.zkprPort, ushort.MaxValue);
            }
            catch (Exception ex)
            {
                RingMasterServiceEventSource.Log.CreateListener_GetEndpointFailed(string.Format("Listener:{0}, Exception:{1}", "ZookeeperProtocol", ex.ToString()));
                throw;
            }

            string uri = $"{protocol}://+:{this.zkprPort}/";

            string nodeIp = context.NodeContext.IPAddressOrFQDN;

            if (nodeIp.Equals("LocalHost", StringComparison.InvariantCultureIgnoreCase))
            {
                nodeIp = GetHostIp(Dns.GetHostName());
            }

            uri = uri.Replace("+", nodeIp);
            return new ZooKeeperTcpListener(this.zkprPort, uri, this.executor, this.zooKeeperServerInstrumentation, new ZkprCommunicationProtocol(), ZkprCommunicationProtocol.MaximumSupportedVersion);
        }

        private sealed class ExecutorInstrumentation : RingMasterRequestExecutor.IInstrumentation
        {
            private readonly IMetric0D requestExecutionCompleted;
            private readonly IMetric0D requestExecutionTimeMs;
            private readonly IMetric0D requestExecutionCancelled;
            private readonly IMetric0D requestExecutionFailed;
            private readonly IMetric0D requestExecutionTimedout;
            private readonly IMetric0D requestQueueLength;
            private readonly IMetric0D requestQueueOverflow;
            private readonly IMetric0D requestExecutionsActive;
            private readonly IMetric0D requestTimeInQueueMs;
            private readonly IMetric0D requestQueueCapacity;
            private readonly IMetric0D requestExecutionThreadCount;

            public ExecutorInstrumentation(IMetricsFactory metricsFactory)
            {
                this.requestExecutionCompleted = metricsFactory.Create0D(nameof(this.requestExecutionCompleted));
                this.requestExecutionTimeMs = metricsFactory.Create0D(nameof(this.requestExecutionTimeMs));
                this.requestExecutionCancelled = metricsFactory.Create0D(nameof(this.requestExecutionCancelled));
                this.requestExecutionFailed = metricsFactory.Create0D(nameof(this.requestExecutionFailed));
                this.requestExecutionTimedout = metricsFactory.Create0D(nameof(this.requestExecutionTimedout));
                this.requestQueueLength = metricsFactory.Create0D(nameof(this.requestQueueLength));
                this.requestQueueOverflow = metricsFactory.Create0D(nameof(this.requestQueueOverflow));
                this.requestExecutionsActive = metricsFactory.Create0D(nameof(this.requestExecutionsActive));
                this.requestTimeInQueueMs = metricsFactory.Create0D(nameof(this.requestTimeInQueueMs));
                this.requestQueueCapacity = metricsFactory.Create0D(nameof(this.requestQueueCapacity));
                this.requestExecutionThreadCount = metricsFactory.Create0D(nameof(this.requestExecutionThreadCount));
            }

            public void OnExecutionScheduled(int queueLength, int queueCapacity)
            {
                this.requestQueueLength.LogValue(queueLength);
                this.requestQueueCapacity.LogValue(queueCapacity);
            }

            public void OnQueueOverflow(int queueLength, int queueCapacity)
            {
                this.requestQueueOverflow.LogValue(1);
                this.requestQueueLength.LogValue(queueLength);
                this.requestQueueCapacity.LogValue(queueCapacity);
            }

            public void OnExecutionStarted(int currentlyActiveCount, int availableThreads, TimeSpan elapsedInQueue)
            {
                this.requestExecutionsActive.LogValue(currentlyActiveCount);
                this.requestTimeInQueueMs.LogValue((long)elapsedInQueue.TotalMilliseconds);
                this.requestExecutionThreadCount.LogValue(availableThreads);
            }

            public void OnExecutionTimedout(TimeSpan elapsed)
            {
                this.requestExecutionTimedout.LogValue(1);
                this.requestTimeInQueueMs.LogValue((long)elapsed.TotalMilliseconds);
            }

            public void OnExecutionCancelled()
            {
                this.requestExecutionCancelled.LogValue(1);
            }

            public void OnExecutionCompleted(TimeSpan elapsed, int currentlyActiveCount)
            {
                this.requestExecutionCompleted.LogValue(1);
                this.requestExecutionTimeMs.LogValue((long)elapsed.TotalMilliseconds);
                this.requestExecutionsActive.LogValue(currentlyActiveCount);
            }

            public void OnExecutionFailed()
            {
                this.requestExecutionFailed.LogValue(1);
            }
        }
    }
}
