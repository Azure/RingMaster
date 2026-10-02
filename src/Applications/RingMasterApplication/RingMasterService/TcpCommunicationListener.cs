// <copyright file="TcpCommunicationListener.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterService
{
    using System;
    using System.Fabric;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterApplication.Utilities;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Server;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;

    using IRingMasterServerInstrumentation = Microsoft.Azure.Networking.Infrastructure.RingMaster.Server.IRingMasterServerInstrumentation;
    using RequestInit = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestInit;

    /// <summary>
    /// TCP listener for native RM endpoint
    /// </summary>
    internal sealed class TcpCommunicationListener : ICommunicationListener, IDisposable
    {
        private readonly IRingMasterServerInstrumentation instrumentation;
        private readonly Backend.IRingMasterServerInstrumentation serverInstrumentation;
        private readonly ICommunicationProtocol protocol;
        private readonly SecureTransport transport;
        private readonly IRingMasterRequestExecutor executor;
        private RingMasterServer server;
        private StatefulServiceContext serviceContext;
        private string endpoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpCommunicationListener" /> class.
        /// </summary>
        /// <param name="server">RingMaster server</param>
        /// <param name="executor">RingMaster request executor</param>
        /// <param name="instrumentation">Instrumentation consumer</param>
        /// <param name="serverInstrumentation">The ringmaster server instrumentation</param>
        /// <param name="protocol">The Marshalling protocol</param>
        /// <param name="serviceContext">The stateful service context.</param>
        /// <param name="maxConnectionIdleTime">The maximum amount of time any connection can be idle.</param>
        /// <param name="maxConnectionLifespan">The maximum lifespan of a connection.</param>
        /// <param name="maxAllowedConnections">Maximum number of client connections allowed by server.</param>
        /// <param name="acceptConnectionTimeout">Timeout in seconds when new connection could't be accepted by server.</param>
        /// <param name="maximumSupportedProtocolVersion">Maximum supported version</param>
        public TcpCommunicationListener(
            RingMasterServer server,
            IRingMasterRequestExecutor executor,
            IRingMasterServerInstrumentation instrumentation,
            Backend.IRingMasterServerInstrumentation serverInstrumentation,
            ICommunicationProtocol protocol,
            StatefulServiceContext serviceContext,
            TimeSpan maxConnectionIdleTime,
            TimeSpan maxConnectionLifespan,
            int maxAllowedConnections,
            TimeSpan acceptConnectionTimeout,
            uint maximumSupportedProtocolVersion)
        {
            this.server = server;
            this.instrumentation = instrumentation;
            this.serverInstrumentation = serverInstrumentation;
            this.protocol = protocol;

            var transportConfig = new SecureTransport.Configuration
            {
                UseSecureConnection = false,
                IsClientCertificateRequired = false,
                CommunicationProtocolVersion = maximumSupportedProtocolVersion,
                MaxConnections = maxAllowedConnections,
                AcceptConnectionTimeout = acceptConnectionTimeout,
            };

            if (maxConnectionIdleTime > TimeSpan.Zero && maxConnectionIdleTime < TimeSpan.MaxValue)
            {
                transportConfig.MaxConnectionIdleTime = maxConnectionIdleTime;
            }

            if (maxConnectionLifespan > TimeSpan.Zero && maxConnectionLifespan < TimeSpan.MaxValue)
            {
                transportConfig.MaxConnectionLifespan = maxConnectionLifespan;
            }

            this.transport = new SecureTransport(transportConfig);
            this.executor = executor;
            this.serviceContext = serviceContext;
            this.endpoint = string.Empty;
            this.Port = 0;
        }

        /// <summary>
        /// Gets or sets the callback that must be invoked when listener stopped.
        /// </summary>
        public Action<Exception> OnListenerStopped { get; set; }

        /// <summary>
        /// Gets or sets the port to listen on.
        /// </summary>
        /// <value>TCP port.</value>
        public int Port { get; set; }

        /// <summary>
        /// The open callback
        /// </summary>
        /// <param name="cancellationToken">the Cancellation Token</param>
        /// <returns>A <see cref="Task"/> that tracks completion of this method</returns>
        public async Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            RingMasterServiceEventSource.Log.ListenerOpenAsync();
            this.server.RegisterTransport(this.transport);
            this.server.OnInitSession = this.OnInitSession;
            this.transport.OnServerStopped += this.ListenerStoppedCallback;
            var unused = this.transport.StartServer(this.Port);

            while (true)
            {
                if (this.transport.HasStopped)
                {
                    // The transport stopped after StartServer got called. Consider OpenAsync failed.
                    throw SecureTransportException.Unexpected($"Transport went to Stopped state after StartServer call with port {this.Port}");
                }

                if (this.transport.HasStarted && this.transport.LocalEndpoint != null)
                {
                    var port = (ushort)((IPEndPoint)this.transport.LocalEndpoint).Port;
                    if (port != 0)
                    {
                        string nodeIp = this.serviceContext.NodeContext.IPAddressOrFQDN;

                        if (nodeIp.Equals("LocalHost", StringComparison.OrdinalIgnoreCase))
                        {
                            nodeIp = RingMasterApplicationHelper.GetHostIp(Dns.GetHostName());
                        }

                        this.endpoint = $"TCP://{nodeIp}:{port}";
                        RingMasterServiceEventSource.Log.CreateListener("RingMasterProtocol", this.endpoint, 0);
                        return this.endpoint;
                    }
                }

                await Task.Yield();
            }
        }

        /// <summary>
        /// Close callback
        /// </summary>
        /// <param name="cancellationToken">the Cancellation token</param>
        /// <returns>A <see cref="Task"/> that tracks completion of this method</returns>
        public Task CloseAsync(CancellationToken cancellationToken)
        {
            RingMasterServiceEventSource.Log.ListenerCloseAsync(this.endpoint);
            this.transport.OnServerStopped -= this.ListenerStoppedCallback;
            this.transport.Stop();
            this.server?.Dispose();
            this.server = null;

            return Task.FromResult(0);
        }

        /// <summary>
        /// Abort function callback
        /// </summary>
        public void Abort()
        {
            this.server?.Dispose();
            this.server = null;

            RingMasterServiceEventSource.Log.ListenerAbort(this.endpoint);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.transport.Dispose();
            this.server?.Dispose();
            this.server = null;
        }

        private IRingMasterRequestHandlerOverlapped OnInitSession(RequestInit initRequest)
        {
            RingMasterServiceEventSource.Log.ListenerInitSession(this.endpoint, initRequest.Auth?.ClientIP, initRequest.Auth?.ClientDigest);
            return new CoreRequestHandler(this.executor, initRequest, this.serverInstrumentation);
        }

        private void ListenerStoppedCallback(Exception ex)
        {
            this.OnListenerStopped?.Invoke(ex);
        }
    }
}
