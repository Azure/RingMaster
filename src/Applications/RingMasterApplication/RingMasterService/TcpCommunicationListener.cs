// <copyright file="TcpCommunicationListener.cs" company="Microsoft">
//     Copyright ©  2016
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterService
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Server;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;

    using IRingMasterServerInstrumentation = Microsoft.Azure.Networking.Infrastructure.RingMaster.Server.IRingMasterServerInstrumentation;
    using RequestInit = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestInit;

    internal sealed class TcpCommunicationListener : ICommunicationListener, IDisposable
    {
        private readonly IRingMasterServerInstrumentation instrumentation;
        private readonly ICommunicationProtocol protocol;
        private readonly SecureTransport transport;
        private readonly RingMasterRequestExecutor executor;
        private readonly int port;
        private readonly string uriPublished;
        private RingMasterServer server;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpCommunicationListener" /> class.
        /// </summary>
        /// <param name="port">Port where this listener will listen</param>
        /// <param name="uriPublished">The specific uri to listen on</param>
        /// <param name="executor">RingMaster request executor</param>
        /// <param name="instrumentation">Instrumentation consumer</param>
        /// <param name="protocol">The Marshalling protocol</param>
        /// <param name="maximumSupportedProtocolVersion">Maximum supported version</param>
        public TcpCommunicationListener(
            int port,
            string uriPublished,
            RingMasterRequestExecutor executor,
            IRingMasterServerInstrumentation instrumentation,
            ICommunicationProtocol protocol,
            uint maximumSupportedProtocolVersion)
        {
            this.port = port;
            this.uriPublished = uriPublished;
            this.instrumentation = instrumentation;
            this.protocol = protocol;

            var transportConfig = new SecureTransport.Configuration
            {
                UseSecureConnection = false,
                IsClientCertificateRequired = false,
                CommunicationProtocolVersion = maximumSupportedProtocolVersion
            };

            this.transport = new SecureTransport(transportConfig);
            this.executor = executor;
        }

        /// <summary>
        /// The open callback
        /// </summary>
        /// <param name="cancellationToken">the Cancellation Token</param>
        /// <returns>A <see cref="Task"/> that tracks completion of this method</returns>
        public Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            RingMasterServiceEventSource.Log.ListenerOpenAsync(this.uriPublished);
            this.server = new RingMasterServer(this.protocol, this.instrumentation, cancellationToken: CancellationToken.None);
            this.server.RegisterTransport(this.transport);
            this.server.OnInitSession = this.OnInitSession;
            this.transport.StartServer(this.port);
            return Task.FromResult(this.uriPublished);
        }

        /// <summary>
        /// Close callback
        /// </summary>
        /// <param name="cancellationToken">the Cancellation token</param>
        /// <returns>A <see cref="Task"/> that tracks completion of this method</returns>
        public Task CloseAsync(CancellationToken cancellationToken)
        {
            RingMasterServiceEventSource.Log.ListenerCloseAsync(this.uriPublished);
            this.transport.Stop();
            return Task.FromResult(0);
        }

        /// <summary>
        /// Abort function callback
        /// </summary>
        public void Abort()
        {
            RingMasterServiceEventSource.Log.ListenerAbort(this.uriPublished);
        }

        public void Dispose()
        {
            this.transport.Dispose();
            this.server.Dispose();
        }

        private IRingMasterRequestHandler OnInitSession(RequestInit initRequest)
        {
            RingMasterServiceEventSource.Log.ListenerInitSession(this.uriPublished, initRequest.Auth?.ClientIP, initRequest.Auth?.ClientDigest);
            return new CoreRequestHandler(this.executor, initRequest);
        }
    }
}
