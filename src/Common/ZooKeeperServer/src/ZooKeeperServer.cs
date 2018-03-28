// <copyright file="ZooKeeperServer.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Server.ZooKeeper
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// Implements a ZooKeeper server.
    /// </summary>
    public sealed class ZooKeeperServer : IDisposable
    {
        private readonly CancellationToken cancellationToken;
        private readonly IZooKeeperCommunicationProtocol protocol;
        private readonly IZooKeeperServerInstrumentation instrumentation;

        private long totalSessionCount = 0;
        private long activeSessionCount = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZooKeeperServer"/> class.
        /// </summary>
        /// <param name="protocol">Protocol used for communication</param>
        /// <param name="instrumentation">Instrumentation consumer</param>
        /// <param name="cancellationToken">Token to observe for cancellation signal</param>
        public ZooKeeperServer(IZooKeeperCommunicationProtocol protocol, IZooKeeperServerInstrumentation instrumentation, CancellationToken cancellationToken)
        {
            if (protocol == null)
            {
                throw new ArgumentNullException(nameof(protocol));
            }

            this.protocol = protocol;
            this.instrumentation = instrumentation;
            this.cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Gets or sets the function to invoke when a new session is to be initialized.
        /// </summary>
        public Func<RequestInit, IRingMasterRequestHandlerOverlapped> OnInitSession { get; set; }

        /// <summary>
        /// Gets or sets the function that will be invoked to determine if a request must be redirected.
        /// </summary>
        public Func<RedirectSuggested> Redirect { get; set; }

        /// <summary>
        /// Registers a transport with the server.
        /// </summary>
        /// <param name="transport">Interface to the transport</param>
        public void RegisterTransport(ITransport transport)
        {
            if (transport == null)
            {
                throw new ArgumentNullException(nameof(transport));
            }

            ProtocolNegotiatorDelegate protocolNegotiator = this.protocol.ProtocolNegotiator;
            if (protocolNegotiator != null)
            {
                transport.OnProtocolNegotiation = protocolNegotiator;
            }

            transport.UseNetworkByteOrder = this.protocol.UseNetworkByteOrderFlag;

            ZooKeeperServerEventSource.Log.RegisterTransport();
            transport.OnNewConnection = this.OnNewConnection;
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            ZooKeeperServerEventSource.Log.Disposed();
        }

        private void OnNewConnection(IConnection connection)
        {
            ulong sessionId = (ulong)Interlocked.Increment(ref this.totalSessionCount);
            string client = connection.RemoteEndPoint.ToString();
            ZooKeeperSession session = new ZooKeeperSession(this, sessionId, this.OnInitSession, connection, this.protocol, this.instrumentation);

            Interlocked.Increment(ref this.activeSessionCount);
            ZooKeeperServerEventSource.Log.SessionCreated(sessionId, connection.Id, client);
            this.instrumentation?.OnSessionCreated(sessionId, client);

            PacketReceiveDelegate packetReciever = this.protocol.PacketReceiver;
            if (packetReciever != null)
            {
                connection.DoPacketReceive = packetReciever;
            }

            connection.OnPacketReceived = packet =>
            {
                Task.Run(
                    async () =>
                    {
                        try
                        {
                            await session.OnPacketReceived(packet);
                        }
                        catch (Exception ex)
                        {
                            ZooKeeperServerEventSource.Log.OnPacketReceived_Failed(session.Id, ex.ToString());
                        }
                    },
                    this.cancellationToken);
            };

            connection.OnConnectionLost = () =>
            {
                session.Close();
                Interlocked.Decrement(ref this.activeSessionCount);
                ZooKeeperServerEventSource.Log.SessionClosed(sessionId, connection.Id, client);
                this.instrumentation?.OnSessionClosed(sessionId, client);
            };
        }
    }
}
