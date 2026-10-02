// <copyright file="ZooKeeperTcpListener.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterService
{
    using System;
    using System.Diagnostics;
    using System.Fabric;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterApplication.Utilities;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Server.ZooKeeper;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;

    using IZooKeeperServerInstrumentation = Microsoft.Azure.Networking.Infrastructure.RingMaster.Server.ZooKeeper.IZooKeeperServerInstrumentation;
    using RequestInit = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestInit;

    /// <summary>
    /// TCP listener for ZooKeeper endpoint
    /// </summary>
    internal sealed class ZooKeeperTcpListener : ICommunicationListener, IDisposable
    {
        private readonly IZooKeeperServerInstrumentation instrumentation;
        private readonly IZooKeeperCommunicationProtocol protocol;
        private readonly SecureTransport transport;
        private readonly IRingMasterRequestExecutor executor;
        private ZooKeeperServer server;
        private string endpoint;
        private StatefulServiceContext serviceContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZooKeeperTcpListener" /> class.
        /// </summary>
        /// <param name="executor">RingMaster request executor</param>
        /// <param name="instrumentation">Instrumentation consumer</param>
        /// <param name="protocol">The Marshalling protocol</param>
        /// <param name="serviceContext">The service context.</param>
        /// <param name="maximumSupportedProtocolVersion">Maximum supported version</param>
        public ZooKeeperTcpListener(
            IRingMasterRequestExecutor executor,
            IZooKeeperServerInstrumentation instrumentation,
            IZooKeeperCommunicationProtocol protocol,
            StatefulServiceContext serviceContext,
            uint maximumSupportedProtocolVersion)
        {
            this.instrumentation = instrumentation;
            this.protocol = protocol;

            var transportConfig = new SecureTransport.Configuration
            {
                UseSecureConnection = false,
                IsClientCertificateRequired = false,
                CommunicationProtocolVersion = maximumSupportedProtocolVersion,
            };

            this.transport = new SecureTransport(transportConfig);
            this.executor = executor;
            this.serviceContext = serviceContext;
            this.endpoint = string.Empty;
            this.Port = 0;
        }

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
            this.server = new ZooKeeperServer(this.protocol, this.instrumentation, cancellationToken: cancellationToken);

            this.server.RegisterTransport(this.transport);
            this.server.OnInitSession = this.OnInitSession;
            var unused = this.transport.StartServer(this.Port);
            while (true)
            {
                if (this.transport.IsActive && this.transport.LocalEndpoint != null)
                {
                    var port = (ushort)((IPEndPoint)this.transport.LocalEndpoint).Port;
                    if (port != 0)
                    {
                        string nodeIp = this.serviceContext.NodeContext.IPAddressOrFQDN;

                        if (nodeIp.Equals("LocalHost", StringComparison.InvariantCultureIgnoreCase))
                        {
                            nodeIp = RingMasterApplicationHelper.GetHostIp(Dns.GetHostName());
                        }

                        this.endpoint = $"TCP://{nodeIp}:{port}";

                        RingMasterServiceEventSource.Log.CreateListener("ZookeeperProtocol", this.endpoint, ushort.MaxValue);
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
            this.transport.Stop();
            return Task.FromResult(0);
        }

        /// <summary>
        /// Abort function callback
        /// </summary>
        public void Abort()
        {
            RingMasterServiceEventSource.Log.ListenerAbort(this.endpoint);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.transport.Dispose();
            this.server.Dispose();
        }

        private IRingMasterRequestHandlerOverlapped OnInitSession(RequestInit initRequest)
        {
            RingMasterServiceEventSource.Log.ListenerInitSession(this.endpoint, initRequest.Auth?.ClientIP, initRequest.Auth?.ClientDigest);
            return new CoreRequestHandler(this.executor, initRequest);
        }
    }
}
