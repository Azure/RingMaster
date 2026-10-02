// <copyright file="RingMasterService.cs" company="Microsoft Corporation">
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
    using System.IO;
    using System.Linq;
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
    using Microsoft.Extensions.Configuration;
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
        private readonly TimeSpan maxConnectionIdleTime;
        private readonly TimeSpan maxConnectionLifespan;
        private readonly int maxAllowedConnections;
        private readonly TimeSpan acceptConnectionTimeout;

        private RingMasterServer ringMasterServer;

        // Based on https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-reliable-services-lifecycle, the Stateful service startup will first call OnOpenAsync,
        // then call CreateServiceReplicaListeners() and StatefulServiceBase.RunAsync() in parallel. So we clear the tcpListenerStoppedOnException in OnOpenAsync, and set it
        // when Transport raise the Stop due to exception. And in the RunAsync, we monitor this exception and finish RunAsync with the exception if any.
        private Exception tcpListenerStoppedOnException = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterService"/> class.
        /// </summary>
        /// <param name="context">Service context</param>
        /// <param name="ringMasterMetricsFactory">Metrics factory for MDM</param>
        /// <param name="persistenceMetricsFactory">Metrics factory for persistence</param>
        public RingMasterService(StatefulServiceContext context, IMetricsFactory ringMasterMetricsFactory, IMetricsFactory persistenceMetricsFactory)
            : base(context, PersistedDataFactory.CreateStateManager(context))
        {
            RingMasterBackendCore.GetSettingFunction = this.GetSetting;
            string factoryName = $"{this.Context.ServiceTypeName}-{this.Context.ReplicaId}-{this.Context.NodeContext.NodeName}";
            this.zooKeeperServerInstrumentation = new ZooKeeperServerInstrumentation(ringMasterMetricsFactory);
            this.ringMasterServerInstrumentation = new RingMasterServerInstrumentation(ringMasterMetricsFactory);

            var ringMasterInstrumentation = new RingMasterBackendInstrumentation(ringMasterMetricsFactory);
            var persistenceInstrumentation = new ServiceFabricPersistenceInstrumentation(persistenceMetricsFactory);

            RingMasterBackendCoreInstrumentation.Instance = ringMasterInstrumentation;

            bool needFixStatDuringLoad = bool.TryParse(this.GetSetting("WinFabPersistence.FixStatDuringLoad"), out needFixStatDuringLoad) && needFixStatDuringLoad;
            bool enableHotSecondary = bool.TryParse(this.GetSetting("WinFabPersistence.HotSecondary"), out enableHotSecondary) && enableHotSecondary;
            bool ignoreErrorsDuringLoad = bool.TryParse(this.GetSetting("WinFabPersistence.IgnoreErrorsDuringLoad"), out ignoreErrorsDuringLoad) && ignoreErrorsDuringLoad;
            TimeSpan.TryParse(this.GetSetting("RingMaster.Transport.ClientConnection.MaxConnectionIdleTime"), out this.maxConnectionIdleTime);
            TimeSpan.TryParse(this.GetSetting("RingMaster.Transport.ClientConnection.MaxConnectionLifespan"), out this.maxConnectionLifespan);

            uint timeoutSeconds;
            if (!uint.TryParse(this.GetSetting("RingMaster.Transport.Server.AcceptConnectionTimeoutSeconds"), out timeoutSeconds))
            {
                timeoutSeconds = 5;
            }

            this.acceptConnectionTimeout = TimeSpan.FromSeconds(timeoutSeconds);
            if (!int.TryParse(this.GetSetting("RingMaster.Transport.Server.MaxAllowedConnections"), out this.maxAllowedConnections))
            {
                this.maxAllowedConnections = 1000;
            }
            else
            {
                Trace.TraceInformation($"Accepted MaxAllowedConnections setting override: {this.maxAllowedConnections}");
            }

            var persistenceConfiguration = new PersistedDataFactory.Configuration
            {
                EnableActiveSecondary = enableHotSecondary,
                FixStatDuringLoad = needFixStatDuringLoad,
                IgnoreErrorsDuringLoad = ignoreErrorsDuringLoad,
            };

            bool useInMemoryPersistence;
            if (bool.TryParse(this.GetSetting("InMemoryPersistence"), out useInMemoryPersistence) && useInMemoryPersistence)
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

                var tcs = new TaskCompletionSource<Exception>();

                this.backend.OnBackendRestartFailure = (o) =>
                {
                    tcs.SetResult(new FabricException("Backend failed to timely restart on primary status lost"));
                };

                this.factory.OnFatalError = (msg, ex) =>
                {
                    // Unable to commit transaction, let exception propagate to RunAsync so the replica can be restarted
                    if (ex is FabricTransientException)
                    {
                        tcs.TrySetResult(new FabricTransientException(msg, ex));
                    }
                    else
                    {
                        tcs.TrySetResult(new FabricException(msg, ex));
                    }
                };

                // Start the backend core at the background. If LoadTree gets stuck due to service fabric replicator
                // issue, the below while loop will handle the failure properly.
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    this.backend.Start(cancellationToken);
                    this.backend.OnBecomePrimary();
                });

                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                string version = fvi.FileVersion;

                var sfFactory = this.factory as PersistedDataFactory;

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

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

                    await Task.WhenAny(
                        Task.Delay(TimeSpan.FromSeconds(30), cancellationToken),
                        tcs.Task);

                    if (tcs.Task.IsCompleted)
                    {
                        throw tcs.Task.Result;
                    }

                    var transportException = this.tcpListenerStoppedOnException;
                    if (transportException != null)
                    {
                        this.tcpListenerStoppedOnException = null;
                        throw transportException;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // TaskCancelledException is handled here. Ignore it. Let the primary become secondary.
            }
            catch (Exception ex)
            {
                var firstEx = (ex as AggregateException)?.Flatten().InnerExceptions.First();
                if (firstEx == null || !(firstEx is OperationCanceledException))
                {
                    // If it's not operation cancelled, we want to know why and handle it later.
                    RingMasterServiceEventSource.Log.RunAsyncFailed(ex.ToString());
                }

                throw;
            }
            finally
            {
                RingMasterServiceEventSource.Log.RunAsyncCompleted(uptime.ElapsedMilliseconds);
                var task = this.backend.OnPrimaryStatusLost().ContinueWith((t) =>
                {
                    if (t.Exception != null)
                    {
                        Trace.TraceError($"OnPrimaryStatusLost finished with exception: {t.Exception}");
                    }
                });
            }
        }

        /// <inheritdoc />
        protected override Task OnOpenAsync(ReplicaOpenMode openMode, CancellationToken cancellationToken)
        {
            this.tcpListenerStoppedOnException = null;

            return base.OnOpenAsync(openMode, cancellationToken);
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

        private System.Fabric.Description.ConfigurationSection GetConfigurationSection(ICodePackageActivationContext context, string sectionName, string configurationPackageName = "Config")
        {
            if (context == null)
            {
                return null;
            }

            ConfigurationPackage package = context.GetConfigurationPackageObject(configurationPackageName);
            if (package == null)
            {
                return null;
            }

            var configSettings = package.Settings.Sections;
            return configSettings.Contains(sectionName) ? configSettings[sectionName] : null;
        }

        private string GetSetting(string settingName)
        {
            return this.GetSetting(settingName, true);
        }

        private string GetSetting(string settingName, bool allowAppConfig)
        {
            try
            {
                if (settingName != "AppConfigOverrides")
                {
                    string overrides = this.GetSetting("AppConfigOverrides", false);

                    if (overrides != null)
                    {
                        foreach (string entry in overrides.Split(';'))
                        {
                            string[] pieces = entry.Split('=');
                            if (pieces.Length == 2 && string.Equals(pieces[0], settingName))
                            {
                                return pieces[1];
                            }
                        }
                    }
                }

                var section = this.GetConfigurationSection(this.Context.CodePackageActivationContext, "RingMasterService");

                if (section != null)
                {
                    string val = section.Parameters[settingName].Value;

                    if (!string.IsNullOrEmpty(val))
                    {
                        return val;
                    }
                }
            }
            catch
            {
                // ignore
            }

            if (!allowAppConfig)
            {
                return null;
            }

            // Fallback: use appSettings.json:
            var path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var builder = new ConfigurationBuilder().SetBasePath(Path.GetDirectoryName(path)).AddJsonFile("appSettings.json");
            IConfiguration appSettings = builder.Build();
            var returnedValue = appSettings[settingName];
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
            var endpoint = context.CodePackageActivationContext.GetEndpoint("ServiceEndpoint");

            var communicationProtocol = new RingMasterCommunicationProtocol();
            this.ringMasterServer = new RingMasterServer(
                communicationProtocol,
                this.ringMasterServerInstrumentation,
                CancellationToken.None);

            var listener = new TcpCommunicationListener(
                this.ringMasterServer,
                this.backend,
                this.ringMasterServerInstrumentation,
                this.backend.ServerInstrumentation,
                communicationProtocol,
                context,
                this.maxConnectionIdleTime,
                this.maxConnectionLifespan,
                this.maxAllowedConnections,
                this.acceptConnectionTimeout,
                RingMasterCommunicationProtocol.MaximumSupportedVersion)
                {
                    Port = endpoint.Port,
                };

            this.tcpListenerStoppedOnException = null;
            listener.OnListenerStopped += (ex) =>
            {
                this.tcpListenerStoppedOnException = ex;
            };

            return listener;
        }

        [SuppressMessage(
            "Microsoft.Reliability",
            "CA2000:DisposeObjectsBeforeLosingScope",
            Scope = "method",
            Target = "Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterService.RingMasterService.CreateListener()",
            Justification = "ZK TCP listener will be disposed when the service is stopped")]
        private ICommunicationListener CreateZkprListener(StatefulServiceContext context)
        {
            var endpoint = context.CodePackageActivationContext.GetEndpoint("ZkprServiceEndpoint");

            return new ZooKeeperTcpListener(
                this.executor,
                this.zooKeeperServerInstrumentation,
                new ZkprCommunicationProtocol(),
                context,
                ZkprCommunicationProtocol.MaximumSupportedVersion)
                {
                    Port = endpoint.Port,
                };
        }
    }
}
