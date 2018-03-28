﻿// <copyright file="RingMasterService.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterService
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
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
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.InMemory;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.ServiceFabric;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Server;
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
        private readonly AbstractPersistedDataFactory factory;
        private readonly RingMasterBackendCore backend;
        private readonly IRingMasterRequestExecutor executor;

        private RingMasterServer ringMasterServer;

        private ushort port = 98;
        private ushort zkprPort = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterService"/> class.
        /// </summary>
        /// <param name="context">Service context</param>
        /// <param name="ringMasterMetricsFactory">Metrics factory for MDM</param>
        /// <param name="persistenceMetricsFactory">Metrics factory for persistence</param>
        public RingMasterService(StatefulServiceContext context, IMetricsFactory ringMasterMetricsFactory, IMetricsFactory persistenceMetricsFactory)
            : base(context, PersistedDataFactory.CreateStateManager(context))
        {
            RingMasterBackendCore.GetSettingFunction = GetSetting;
            string factoryName = $"{this.Context.ServiceTypeName}-{this.Context.ReplicaId}-{this.Context.NodeContext.NodeName}";
            this.zooKeeperServerInstrumentation = new ZooKeeperServerInstrumentation(ringMasterMetricsFactory);
            this.ringMasterServerInstrumentation = new RingMasterServerInstrumentation(ringMasterMetricsFactory);

            var ringMasterInstrumentation = new RingMasterBackendInstrumentation(ringMasterMetricsFactory);
            var persistenceInstrumentation = new ServiceFabricPersistenceInstrumentation(persistenceMetricsFactory);

            RingMasterBackendCoreInstrumentation.Instance = ringMasterInstrumentation;

            var persistenceConfiguration = new PersistedDataFactory.Configuration
            {
                EnableActiveSecondary = true,
            };

            bool useInMemoryPersistence;
            if (bool.TryParse(GetSetting("InMemoryPersistence"), out useInMemoryPersistence) && useInMemoryPersistence)
            {
                this.factory = new InMemoryFactory();
            }
            else
            {
                this.factory = new PersistedDataFactory(
                    this.StateManager,
                    factoryName,
                    persistenceConfiguration,
                    persistenceInstrumentation,
                    this.cancellationSource.Token);
            }

            this.backend = new RingMasterBackendCore(this.factory);

            this.factory.SetBackend(this.backend);

            this.executor = this.backend;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.backend.Dispose();
            this.factory.Dispose();
            this.cancellationSource.Dispose();

            if (this.ringMasterServer != null)
            {
                this.ringMasterServer.Dispose();
                this.ringMasterServer = null;
            }
        }

        /// <inheritdoc />
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var uptime = Stopwatch.StartNew();
            try
            {
                RingMasterServiceEventSource.Log.RunAsync();

                this.backend.Start();
                this.backend.OnBecomePrimary();

                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                string version = fvi.FileVersion;

                var sfFactory = this.factory as PersistedDataFactory;

                while (!cancellationToken.IsCancellationRequested)
                {
                    var totalSessionCount = 0L;
                    var activeSessionCount = 0L;
                    if (this.ringMasterServer != null)
                    {
                        totalSessionCount = this.ringMasterServer.TotalSessionCout;
                        activeSessionCount = this.ringMasterServer.ActiveSessionCout;
                    }

                    sfFactory?.ReportStatus();
                    RingMasterServiceEventSource.Log.ReportServiceStatus(version, (long)uptime.Elapsed.TotalSeconds, (int)totalSessionCount);
                    RingMasterBackendCoreInstrumentation.Instance.OnUpdateStatus(version, uptime.Elapsed, true, (int)activeSessionCount);

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

        /// <inheritdoc />
        protected override Task OnCloseAsync(CancellationToken cancellationToken)
        {
            try
            {
                RingMasterServiceEventSource.Log.OnCloseAsync();
                this.cancellationSource.Cancel();
                this.backend.Stop();
            }
            catch (Exception ex)
            {
                RingMasterServiceEventSource.Log.OnCloseAsyncFailed(ex.ToString());
            }

            return Task.FromResult(0);
        }

        /// <inheritdoc />
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[]
            {
                new ServiceReplicaListener(this.CreateListener, "ServiceEndpoint", listenOnSecondary: false),
                new ServiceReplicaListener(this.CreateZkprListener, "ZkprServiceEndpoint", listenOnSecondary: false),
            };
        }

        /// <inheritdoc />
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

        [SuppressMessage(
            "Microsoft.Reliability",
            "CA2000:DisposeObjectsBeforeLosingScope",
            Scope = "method",
            Target = "Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterService.RingMasterService.CreateListener()",
            Justification = "TCP listener will be disposed when the service is stopped")]
        private ICommunicationListener CreateListener(StatefulServiceContext context)
        {
            // Partition replica's URL is the node's IP, port, PartitionId, ReplicaId, Guid
            var protocol = EndpointProtocol.Tcp;

            try
            {
                var internalEndpoint = context.CodePackageActivationContext.GetEndpoint("ServiceEndpoint");
                this.port = Convert.ToUInt16(internalEndpoint.Port);

                protocol = internalEndpoint.Protocol;

                RingMasterServiceEventSource.Log.CreateListener("RingMasterProtocol", this.port, 0);
            }
            catch (Exception ex)
            {
                RingMasterServiceEventSource.Log.CreateListener_GetEndpointFailed($"Failed to get ServiceEndpoint for RingMasterProtocol: {ex}");
                throw;
            }

            var communicationProtocol = new RingMasterCommunicationProtocol();
            this.ringMasterServer = new RingMasterServer(
                communicationProtocol,
                this.ringMasterServerInstrumentation,
                CancellationToken.None);

            string nodeIp = context.NodeContext.IPAddressOrFQDN;

            if (nodeIp.Equals("LocalHost", StringComparison.OrdinalIgnoreCase))
            {
                nodeIp = GetHostIp(Dns.GetHostName());
            }

            string uri = $"{protocol}://{nodeIp}:{this.port}/";
            return new TcpCommunicationListener(
                this.ringMasterServer,
                this.port,
                uri,
                this.backend,
                this.ringMasterServerInstrumentation,
                communicationProtocol,
                RingMasterCommunicationProtocol.MaximumSupportedVersion);
        }

        [SuppressMessage(
            "Microsoft.Reliability",
            "CA2000:DisposeObjectsBeforeLosingScope",
            Scope = "method",
            Target = "Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterService.RingMasterService.CreateListener()",
            Justification = "ZK TCP listener will be disposed when the service is stopped")]
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
            return new ZooKeeperTcpListener(
                this.zkprPort,
                uri,
                this.executor,
                this.zooKeeperServerInstrumentation,
                new ZkprCommunicationProtocol(),
                ZkprCommunicationProtocol.MaximumSupportedVersion);
        }
    }
}
