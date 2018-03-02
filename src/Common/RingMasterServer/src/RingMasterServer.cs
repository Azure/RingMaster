﻿// <copyright file="RingMasterServer.cs" company="Microsoft">
//     Copyright ©  2016
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Server
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
    /// Implements a RingMaster server.
    /// </summary>
    public sealed class RingMasterServer : IDisposable
    {
        private readonly CancellationToken cancellationToken;
        private readonly ICommunicationProtocol protocol;
        private readonly IRingMasterServerInstrumentation instrumentation;

        private long totalSessionCount = 0;
        private long activeSessionCount = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterServer"/> class.
        /// </summary>
        /// <param name="protocol">Protocol used for communication</param>
        /// <param name="instrumentation">Instrumentation consumer</param>
        /// <param name="cancellationToken">Token to observe for cancellation signal</param>
        public RingMasterServer(ICommunicationProtocol protocol, IRingMasterServerInstrumentation instrumentation, CancellationToken cancellationToken)
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
        /// Gets or sets the <see cref="TraceLevel"/> that controls which trace messages are logged by the <see cref="RingMasterServer"/>.
        /// </summary>
        public static TraceLevel TraceLevel
        {
            get
            {
                return RingMasterServerEventSource.Log.TraceLevel;
            }

            set
            {
                RingMasterServerEventSource.Log.TraceLevel = value;
            }
        }

        /// <summary>
        /// Gets or sets the function to invoke when a new session is to be initialized.
        /// </summary>
        public Func<RequestInit, IRingMasterRequestHandler> OnInitSession { get; set; }

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

            RingMasterServerEventSource.Log.RegisterTransport();
            transport.OnNewConnection = this.OnNewConnection;
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            RingMasterServerEventSource.Log.Disposed();
        }

        private void OnNewConnection(IConnection connection)
        {
            ulong sessionId = (ulong)Interlocked.Increment(ref this.totalSessionCount);
            string client = connection.RemoteEndPoint.ToString();
            Session session = new Session(this, sessionId, this.OnInitSession, connection, this.protocol, this.instrumentation);

            Interlocked.Increment(ref this.activeSessionCount);
            RingMasterServerEventSource.Log.SessionCreated(sessionId, connection.Id, client);
            this.instrumentation?.OnSessionCreated(sessionId, client);

            PacketReceiveDelegate packetReciever = this.protocol.PacketReciever;
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
                            RingMasterServerEventSource.Log.OnPacketReceived_Failed(session.Id, ex.ToString());
                        }
                    },
                    this.cancellationToken);
            };

            connection.OnConnectionLost = () =>
            {
                session.Close();
                Interlocked.Decrement(ref this.activeSessionCount);
                RingMasterServerEventSource.Log.SessionClosed(sessionId, connection.Id, client);
                this.instrumentation?.OnSessionClosed(sessionId, client);
            };
        }
    }
}