// <copyright file="ConnectionStressService.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ConnectionStressService
{
    using System;
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
    using Microsoft.Vega.Test.Helpers;

    /// <summary>
    /// Service used to stress the RingMaster with lots of connections.
    /// </summary>
    public class ConnectionStressService : StatelessService
    {
        private static readonly Uri RingMasterServiceUri = new Uri("fabric:/RingMaster/RingMasterService");

        public ConnectionStressService(StatelessServiceContext context, IMetricsFactory metricsFactory)
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
                while (!cancellationToken.IsCancellationRequested)
                {
                    ConfigurationSection connectPerformanceTestConfiguration = this.Context.CodePackageActivationContext.GetConfigurationSection("ConnectPerformanceTest");
                    await Task.Run(() => this.ConnectPerformanceTest(connectPerformanceTestConfiguration, cancellationToken));

                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                ConnectionStressServiceEventSource.Log.RunAsyncFailed(ex.ToString());
                throw;
            }
            finally
            {
                ConnectionStressServiceEventSource.Log.Terminated((long)uptime.Elapsed.TotalSeconds);
            }
        }

        private void ConnectPerformanceTest(ConfigurationSection config, CancellationToken cancellationToken)
        {
            try
            {
                var instrumentation = new ConnectPerformanceInstrumentation(this.MetricsFactory);
                var random = new Random();

                string connectionString = config.GetStringValue("TargetConnectionString");
                connectionString = Helpers.GetServerAddressIfNotProvided(connectionString);
                IPEndPoint[] endpoints = SecureTransport.ParseConnectionString(connectionString);
                string testPath = config.GetStringValue("TestPath");
                int numConnections = config.GetIntValue("NumberOfConnections");
                int maxConcurrentRequests = config.GetIntValue("MaxConcurrentRequests");
                int minConnectionLifetimeSeconds = config.GetIntValue("MinConnectionLifetimeSeconds");
                int maxConnectionLifetimeSeconds = config.GetIntValue("MaxConnectionLifetimeSeconds");

                Func<IRingMasterRequestHandler> createConnection = () =>
                {
                    var connectionConfiguration = new SecureTransport.Configuration
                    {
                        UseSecureConnection = false,
                        CommunicationProtocolVersion = RingMasterCommunicationProtocol.MaximumSupportedVersion,
                        MaxConnectionLifespan = TimeSpan.FromSeconds(random.Next(minConnectionLifetimeSeconds, maxConnectionLifetimeSeconds))
                    };

                    var protocol = new RingMasterCommunicationProtocol();
                    var transport = new SecureTransport(connectionConfiguration, instrumentation, cancellationToken);
                    var client = new RingMasterClient(protocol, transport);
                    transport.StartClient(endpoints);

                    ConnectionStressServiceEventSource.Log.CreateConnection(
                        connectionConfiguration.UseSecureConnection,
                        connectionConfiguration.CommunicationProtocolVersion,
                        (long)connectionConfiguration.MaxConnectionLifespan.TotalSeconds);

                    client.Exists("/", watcher: null).Wait();
                    return (IRingMasterRequestHandler)client;
                };

                using (var connectPerformanceTest = new ConnectPerformance(instrumentation, maxConcurrentRequests, cancellationToken))
                {
                    ConnectionStressServiceEventSource.Log.ConnectPerformanceTestStarted(testPath, numConnections, minConnectionLifetimeSeconds, maxConnectionLifetimeSeconds);

                    connectPerformanceTest.EstablishConnections(createConnection, numConnections);

                    connectPerformanceTest.QueueRequests(testPath);
                }

                ConnectionStressServiceEventSource.Log.ConnectPerformanceTestCompleted();
            }
            catch (Exception ex)
            {
                ConnectionStressServiceEventSource.Log.ConnectPerformanceTestFailed(ex.ToString());
            }
        }

        private sealed class ConnectPerformanceInstrumentation : ConnectPerformance.IInstrumentation, ISecureTransportInstrumentation
        {
            private readonly IMetric0D totalConnectionsCreated;
            private readonly IMetric0D connectionEstablished;
            private readonly IMetric0D connectionFailed;
            private readonly IMetric0D connectionClosed;
            private readonly IMetric0D connectionSetupTimeMs;

            public ConnectPerformanceInstrumentation(IMetricsFactory metricsFactory)
            {
                this.totalConnectionsCreated = metricsFactory.Create0D("totalConnectionsCreated");
                this.connectionEstablished = metricsFactory.Create0D("connectionEstablished");
                this.connectionFailed = metricsFactory.Create0D("connectionFailed");
                this.connectionClosed = metricsFactory.Create0D("connectionClosed");
                this.connectionSetupTimeMs = metricsFactory.Create0D("connectionSetupTimeMs");
            }

            public void ConnectionCreated(int connectionCount, TimeSpan elapsed)
            {
                ConnectionStressServiceEventSource.Log.ConnectionCreated(connectionCount, (long)elapsed.TotalMilliseconds);
                this.totalConnectionsCreated.LogValue(connectionCount);
            }

            public void RequestFailed()
            {
            }

            public void RequestSucceeded(TimeSpan elapsed)
            {
            }

            public void ConnectionEstablished(IPEndPoint serverEndPoint, string serverIdentity, TimeSpan setupTime)
            {
                ConnectionStressServiceEventSource.Log.ConnectionEstablished(serverEndPoint.ToString(), serverIdentity, (long)setupTime.TotalMilliseconds);
                this.connectionEstablished.LogValue(1);
                this.connectionSetupTimeMs.LogValue((long)setupTime.TotalMilliseconds);
            }

            public void EstablishConnectionFailed(TimeSpan processingTime)
            {
                ConnectionStressServiceEventSource.Log.EstablishConnectionFailed((long)processingTime.TotalMilliseconds);
                this.connectionFailed.LogValue(1);
            }

            public void ConnectionAccepted(IPEndPoint clientEndPoint, string clientIdentity, TimeSpan setupTime)
            {
            }

            public void AcceptConnectionFailed(IPEndPoint clientEndPoint, TimeSpan processingTime)
            {
            }

            public void ConnectionCreated(long connectionId, IPEndPoint remoteEndPoint, string remoteIdentity)
            {
            }

            public void ConnectionClosed(long connectionId, IPEndPoint remoteEndPoint, string remoteIdentity)
            {
                ConnectionStressServiceEventSource.Log.ConnectionClosed(connectionId, remoteEndPoint.ToString(), remoteIdentity);
                this.connectionClosed.LogValue(1);
            }
        }
    }
}
