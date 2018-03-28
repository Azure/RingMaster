// <copyright file="SecureTransportEventSource.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport
{
    using System.Diagnostics;
    using System.Diagnostics.Tracing;

    /// <summary>
    /// Event Source
    /// </summary>
    [EventSource(Name = "Microsoft-Azure-Networking-Infrastructure-RingMaster-SecureTransport")]
    internal sealed class SecureTransportEventSource : EventSource
    {
        private static readonly SecureTransportEventSource LogInstance = new SecureTransportEventSource();

        /// <summary>
        /// Gets or sets a value indicating whether packet receives are logged. False by default
        /// </summary>
        public static bool LogPacketReceives { get; set; } = false;

        /// <summary>
        /// Gets the singleton instance
        /// </summary>
        public static SecureTransportEventSource Log
        {
            get { return LogInstance; }
        }

        /// <summary>
        /// Server listening is about to start
        /// </summary>
        /// <param name="transportId">Monolitically incremeting transport ID</param>
        /// <param name="endpoint">Local endpoint to listen</param>
        [Event(2, Level = EventLevel.LogAlways, Version = 2)]
        public void StartServer(long transportId, string endpoint)
        {
            this.WriteEvent(2, transportId, endpoint);
        }

        /// <summary>
        /// Signals the stopping of the server
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="isListening">If TCP listener exists</param>
        /// <param name="activeConnections">Count of active connections</param>
        [Event(3, Level = EventLevel.LogAlways, Version = 2)]
        public void SignallingStop(long transportId, bool isListening, int activeConnections)
        {
            this.WriteEvent(3, transportId, isListening, activeConnections);
        }

        /// <summary>
        /// Secure transport is stopped
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        [Event(4, Level = EventLevel.LogAlways, Version = 2)]
        public void Stopped(long transportId)
        {
            this.WriteEvent(4, transportId);
        }

        /// <summary>
        /// Connection is being established
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="iteration">Number of attempts</param>
        /// <param name="serverEndpoint">Endpoint of server</param>
        /// <param name="timeoutMilliseconds">Timeout in millisecond</param>
        [Event(5, Level = EventLevel.Informational, Version = 3)]
        public void EstablishConnection(long transportId, int iteration, string serverEndpoint, long timeoutMilliseconds)
        {
            this.WriteEvent(5, transportId, iteration, serverEndpoint, timeoutMilliseconds);
        }

        /// <summary>
        /// TCP connection is accepted, will authenticate the other party
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="iteration">Number of attempts</param>
        /// <param name="remoteEndpoint">Endpoint of the other party</param>
        [Event(6, Level = EventLevel.Informational, Version = 3)]
        public void AcceptConnection(long transportId, int iteration, string remoteEndpoint)
        {
            this.WriteEvent(6, transportId, iteration, remoteEndpoint);
        }

        /// <summary>
        /// New connection is established, will start to receive packets
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="connectionId">Connection ID</param>
        /// <param name="elapsedMilliseconds">Duration of the new connection callback</param>
        [Event(7, Level = EventLevel.Informational, Version = 4)]
        public void OnNewConnection(long transportId, long connectionId, long elapsedMilliseconds)
        {
            this.WriteEvent(7, transportId, connectionId, elapsedMilliseconds);
        }

        /// <summary>
        /// Connection is lost
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="connectionId">Connection ID</param>
        /// <param name="elapsedMilliseconds">Duration of the connection lost callback</param>
        [Event(8, Level = EventLevel.Informational, Version = 4)]
        public void OnConnectionLost(long transportId, long connectionId, long elapsedMilliseconds)
        {
            this.WriteEvent(8, transportId, connectionId, elapsedMilliseconds);
        }

        /// <summary>
        /// Transport protocol is negotiated
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="connectionId">Connection ID</param>
        /// <param name="localProtocolVersion">Protocol version of this party</param>
        /// <param name="remoteProtocolVersion">Protocol version of the other party</param>
        /// <param name="acceptedProtocolVersion">Protocol version agreed by both parties</param>
        [Event(9, Level = EventLevel.Informational, Version = 3)]
        public void NegotiateProtocol(long transportId, long connectionId, uint localProtocolVersion, uint remoteProtocolVersion, uint acceptedProtocolVersion)
        {
            this.WriteEvent(9, transportId, connectionId, localProtocolVersion, remoteProtocolVersion, acceptedProtocolVersion);
        }

        /// <summary>
        /// A packet has been sent
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="connectionId">Connection ID</param>
        /// <param name="packetId">Packet ID</param>
        /// <param name="packetLength">Length of packet</param>
        [Event(10, Level = EventLevel.Verbose, Version = 4)]
        public void Send(long transportId, long connectionId, long packetId, int packetLength)
        {
            this.WriteEvent(10, transportId, connectionId, packetId, packetLength);
        }

        /// <summary>
        /// Connection is closed
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="connectionId">Connection ID</param>
        [Event(11, Level = EventLevel.Informational, Version = 3)]
        public void ConnectionClose(long transportId, long connectionId)
        {
            this.WriteEvent(11, transportId, connectionId);
        }

        /// <summary>
        /// A packet is received
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="connectionId">Connection ID</param>
        /// <param name="packetLength">Length of the packet</param>
        [Event(12, Level = EventLevel.Verbose, Version = 3)]
        public void OnPacketReceived(long transportId, long connectionId, int packetLength)
        {
            if (SecureTransportEventSource.LogPacketReceives)
            {
                this.WriteEvent(12, transportId, connectionId, packetLength);
            }
        }

        /// <summary>
        /// Failed to pull packets in the async task
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="connectionId">Connection ID</param>
        /// <param name="exception">Exception message</param>
        [Event(13, Level = EventLevel.Error, Version = 3)]
        public void PullPacketsFailed(long transportId, long connectionId, string exception)
        {
            this.WriteEvent(13, transportId, connectionId, exception);
        }

        /// <summary>
        /// No more data received, pull packets is completed
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="connectionId">Connection ID</param>
        [Event(14, Level = EventLevel.Informational, Version = 4)]
        public void PullPacketsCompleted(long transportId, long connectionId)
        {
            this.WriteEvent(14, transportId, connectionId);
        }

        /// <summary>
        /// Received an incompleted packet
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="connectionId">Connection ID</param>
        /// <param name="expectedLength">Length of expected packet</param>
        /// <param name="actualLength">Length of actual packet</param>
        [Event(15, Level = EventLevel.Informational, Version = 3)]
        public void ReceiveIncomplete(long transportId, long connectionId, int expectedLength, int actualLength)
        {
            this.WriteEvent(15, transportId, connectionId, expectedLength, actualLength);
        }

        /// <summary>
        /// TCP listner is failed.
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="message">Error message</param>
        [Event(16, Level = EventLevel.Error, Version = 2)]
        public void ListenerFailed(long transportId, string message)
        {
            this.WriteEvent(16, transportId, message);
        }

        /// <summary>
        /// Failed to establish connection
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="iteration">Number of attempts</param>
        /// <param name="host">Host name or IP</param>
        /// <param name="port">Port number</param>
        /// <param name="message">Error message</param>
        /// <param name="elapsedMilliseconds">Duration of attempting to establish connection</param>
        [Event(17, Level = EventLevel.Informational, Version = 3)]
        public void ConnectFailed(long transportId, int iteration, string host, int port, string message, long elapsedMilliseconds)
        {
            this.WriteEvent(17, transportId, iteration, host, port, message, elapsedMilliseconds);
        }

        /// <summary>
        /// Failed to establish connection to all endpoints
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="iteration">Number of attempts</param>
        /// <param name="endpointCount">Number of server endpoints</param>
        /// <param name="message">Error message</param>
        /// <param name="elapsedMilliseconds">Duration of attempting to connect</param>
        [Event(18, Level = EventLevel.Error, Version = 4)]
        public void EstablishConnectionFailed(long transportId, int iteration, int endpointCount, string message, long elapsedMilliseconds)
        {
            this.WriteEvent(18, transportId, iteration, endpointCount, message, elapsedMilliseconds);
        }

        /// <summary>
        /// Client is already started
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        [Event(19, Level = EventLevel.Error, Version = 2)]
        public void StartClientFailed_AlreadyStarted(long transportId)
        {
            this.WriteEvent(19, transportId);
        }

        /// <summary>
        /// Server is already started
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        [Event(20, Level = EventLevel.Error, Version = 2)]
        public void StartServerFailed_AlreadyStarted(long transportId)
        {
            this.WriteEvent(20, transportId);
        }

        /// <summary>
        /// Not started so cannot stop
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        [Event(21, Level = EventLevel.Error, Version = 2)]
        public void StopFailed_NotStarted(long transportId)
        {
            this.WriteEvent(21, transportId);
        }

        /// <summary>
        /// Active connection is being closed
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="connectionId">Connection ID</param>
        [Event(22, Level = EventLevel.Informational, Version = 3)]
        public void CloseActiveConnection(long transportId, long connectionId)
        {
            this.WriteEvent(22, transportId, connectionId);
        }

        /// <summary>
        /// Secure transport is being closed
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        [Event(23, Level = EventLevel.Informational, Version = 2)]
        public void SecureTransportClose(long transportId)
        {
            this.WriteEvent(23, transportId);
        }

        /// <summary>
        /// Server certificate is successfully validated
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="serialNumber">Serial number of the certificate</param>
        /// <param name="issuer">Issuer of the certificate</param>
        /// <param name="subject">Subject name of the certificate</param>
        /// <param name="thumbprint">Thumbprint of the certificate</param>
        [Event(24, Level = EventLevel.Informational, Version = 4)]
        public void ValidateServerCertificateSucceeded(long transportId, string serialNumber, string issuer, string subject, string thumbprint)
        {
            this.WriteEvent(24, transportId, serialNumber, issuer, subject, thumbprint);
        }

        /// <summary>
        /// Skipped server certificate validation
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        [Event(25, Level = EventLevel.Warning, Version = 2)]
        public void ValidateServerCertificateSkipped(long transportId)
        {
            this.WriteEvent(25, transportId);
        }

        /// <summary>
        /// Failed to validate the server certificate
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="serialNumber">Serial number of the certificate</param>
        /// <param name="issuer">Issuer of the certificate</param>
        /// <param name="subject">Subject name of the certificate</param>
        /// <param name="thumbprint">Thumbprint of the certificate</param>
        [Event(26, Level = EventLevel.Error, Version = 3)]
        public void ValidateServerCertificateFailed(long transportId, string serialNumber, string issuer, string subject, string thumbprint)
        {
            this.WriteEvent(26, transportId, serialNumber, issuer, subject, thumbprint);
        }

        /// <summary>
        /// Client certificate is successfully validated
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="serialNumber">Serial number of the certificate</param>
        /// <param name="issuer">Issuer of the certificate</param>
        /// <param name="subject">Subject name of the certificate</param>
        /// <param name="thumbprint">Thumbprint of the certificate</param>
        [Event(27, Level = EventLevel.Informational, Version = 4)]
        public void ValidateClientCertificateSucceeded(long transportId, string serialNumber, string issuer, string subject, string thumbprint)
        {
            this.WriteEvent(27, transportId, serialNumber, issuer, subject, thumbprint);
        }

        /// <summary>
        /// Skipped client certificate validation
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        [Event(28, Level = EventLevel.Warning, Version = 2)]
        public void ValidateClientCertificateSkipped(long transportId)
        {
            this.WriteEvent(28, transportId);
        }

        /// <summary>
        /// Failed to validate the client certificate
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="serialNumber">Serial number of the certificate</param>
        /// <param name="issuer">Issuer of the certificate</param>
        /// <param name="subject">Subject name of the certificate</param>
        /// <param name="thumbprint">Thumbprint of the certificate</param>
        [Event(29, Level = EventLevel.Error, Version = 3)]
        public void ValidateClientCertificateFailed(long transportId, string serialNumber, string issuer, string subject, string thumbprint)
        {
            this.WriteEvent(29, transportId, serialNumber, issuer, subject, thumbprint);
        }

        /// <summary>
        /// Server certificates were not provided
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        [Event(30, Level = EventLevel.Error, Version = 2)]
        public void ServerCertificatesWereNotProvided(long transportId)
        {
            this.WriteEvent(30, transportId);
        }

        /// <summary>
        /// Client certificates were not provided
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        [Event(31, Level = EventLevel.Warning, Version = 2)]
        public void ClientCertificatesWereNotProvided(long transportId)
        {
            this.WriteEvent(31, transportId);
        }

        /// <summary>
        /// Supported client certificate
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="serialNumber">Serial number of the client certificate</param>
        [Event(32, Level = EventLevel.Informational, Version = 3)]
        public void SupportedClientCertificate(long transportId, string serialNumber)
        {
            this.WriteEvent(32, transportId, serialNumber);
        }

        /// <summary>
        /// Supported server certificate
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="serialNumber">Serial number of the server certificate</param>
        [Event(33, Level = EventLevel.Informational, Version = 3)]
        public void SupportedServerCertificate(long transportId, string serialNumber)
        {
            this.WriteEvent(33, transportId, serialNumber);
        }

        /// <summary>
        /// Failed to get the certificates from either thumbprint or file name
        /// </summary>
        /// <param name="exception">Exception message</param>
        [Event(34, Level = EventLevel.Error, Version = 2)]
        public void GetCertificatesFromThumbprintOrFileNameFailed(string exception)
        {
            this.WriteEvent(34, exception);
        }

        /// <summary>
        /// Authenticate and consider this party as a client
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="timeoutInMilliseconds">Authentication timeout in millisecond</param>
        /// <param name="mustCheckCertificateRevocation">True if certificate revocation must be checked</param>
        /// <param name="mustCheckCertificateTrustChain">True if certificate trust chain must be checked</param>
        [Event(35, Level = EventLevel.Informational, Version = 4)]
        public void AuthenticateAsClient(long transportId, int timeoutInMilliseconds, bool mustCheckCertificateRevocation, bool mustCheckCertificateTrustChain)
        {
            this.WriteEvent(35, transportId, timeoutInMilliseconds, mustCheckCertificateRevocation, mustCheckCertificateTrustChain);
        }

        /// <summary>
        /// Failed to authenticate as a client
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="exception">Exception message</param>
        [Event(36, Level = EventLevel.Error, Version = 3)]
        public void AuthenticateAsClientFailed(long transportId, string exception)
        {
            this.WriteEvent(36, transportId, exception);
        }

        /// <summary>
        /// Authenticate and consider this party as a server
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="timeoutInMilliseconds">Authentication timeout in millisecond</param>
        /// <param name="mustCheckCertificateRevocation">True if certificate revocation must be checked</param>
        /// <param name="mustCheckCertificateTrustChain">True if certificate trust chain must be checked</param>
        [Event(37, Level = EventLevel.Informational, Version = 5)]
        public void AuthenticateAsServer(long transportId, int timeoutInMilliseconds, bool mustCheckCertificateRevocation, bool mustCheckCertificateTrustChain)
        {
            this.WriteEvent(37, transportId, timeoutInMilliseconds, mustCheckCertificateRevocation, mustCheckCertificateTrustChain);
        }

        /// <summary>
        /// Failed to authenticate as a server
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="exception">Exception message</param>
        [Event(38, Level = EventLevel.Error, Version = 3)]
        public void AuthenticateAsServerFailed(long transportId, string exception)
        {
            this.WriteEvent(38, transportId, exception);
        }

        /// <summary>
        /// Failed to accept a connection
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="iteration">Number of attempts</param>
        /// <param name="remoteEndpoint">Endpoint of the other party</param>
        /// <param name="elapsedMilliseconds">Duration of accepting connection including authentication</param>
        /// <param name="exception">Exception message</param>
        [Event(39, Level = EventLevel.Error, Version = 4)]
        public void AcceptConnectionFailed(long transportId, int iteration, string remoteEndpoint, long elapsedMilliseconds, string exception)
        {
            this.WriteEvent(39, transportId, iteration, remoteEndpoint, elapsedMilliseconds, exception);
        }

        /// <summary>
        /// TCP listener is stopped
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        [Event(40, Level = EventLevel.Warning, Version = 2)]
        public void ListenerStopped(long transportId)
        {
            this.WriteEvent(40, transportId);
        }

        /// <summary>
        /// Either the server or client start timed out
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        [Event(41, Level = EventLevel.Error, Version = 2)]
        public void StartTimedout(long transportId)
        {
            this.WriteEvent(41, transportId);
        }

        /// <summary>
        /// Timed out stopping the secure transport
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        [Event(42, Level = EventLevel.Error, Version = 3)]
        public void StopTimedout(long transportId)
        {
            this.WriteEvent(42, transportId);
        }

        /// <summary>
        /// Failed to accept the TCP client
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="iteration">Number of attempts</param>
        /// <param name="consecutiveFailureCount">Consecutive failure count</param>
        /// <param name="exception">Exception message</param>
        [Event(43, Level = EventLevel.Error, Version = 3)]
        public void AcceptTcpClientFailed(long transportId, int iteration, int consecutiveFailureCount, string exception)
        {
            this.WriteEvent(43, transportId, iteration, consecutiveFailureCount, exception);
        }

        /// <summary>
        /// Client is about to start
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="endpointCount">Number of server endpoints</param>
        [Event(44, Level = EventLevel.LogAlways, Version = 2)]
        public void StartClient(long transportId, int endpointCount)
        {
            this.WriteEvent(44, transportId, endpointCount);
        }

        /// <summary>
        /// Set the connection life time
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="connectionId">Connection ID</param>
        /// <param name="maxConnectionLifetimeInMs">Max connection life time in millisecond</param>
        [Event(45, Level = EventLevel.Informational, Version = 2)]
        public void SetConnectionLifetimeLimit(long transportId, long connectionId, long maxConnectionLifetimeInMs)
        {
            this.WriteEvent(45, transportId, connectionId, maxConnectionLifetimeInMs);
        }

        /// <summary>
        /// Connection life time limit is expired
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="connectionId">Connection ID</param>
        /// <param name="elapsedMilliseconds">Life time of this connection</param>
        [Event(46, Level = EventLevel.Informational, Version = 3)]
        public void ConnectionLifetimeLimitExpired(long transportId, long connectionId, long elapsedMilliseconds)
        {
            this.WriteEvent(46, transportId, connectionId, elapsedMilliseconds);
        }

        /// <summary>
        /// Failed to push the packets to the other party
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="connectionId">Connection ID</param>
        /// <param name="exception">Exception message</param>
        [Event(47, Level = EventLevel.Error, Version = 2)]
        public void PushPacketsFailed(long transportId, long connectionId, string exception)
        {
            this.WriteEvent(47, transportId, connectionId, exception);
        }

        /// <summary>
        /// Push packets task is completed
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="connectionId">Connection ID</param>
        [Event(48, Level = EventLevel.Informational, Version = 3)]
        public void PushPacketsCompleted(long transportId, long connectionId)
        {
            this.WriteEvent(48, transportId, connectionId);
        }

        /// <summary>
        /// Set connection idle time limit
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="connectionId">Connection ID</param>
        /// <param name="maxConnectionIdleTimeInMs">Max connetion idle time in millisecond</param>
        [Event(49, Level = EventLevel.Informational, Version = 1)]
        public void SetConnectionIdleTimeLimit(long transportId, long connectionId, long maxConnectionIdleTimeInMs)
        {
            this.WriteEvent(49, transportId, connectionId, maxConnectionIdleTimeInMs);
        }

        /// <summary>
        /// Connection idle time limit is expired
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="connectionId">Connection ID</param>
        /// <param name="elapsedMilliseconds">Time since the last activity</param>
        [Event(50, Level = EventLevel.Informational, Version = 1)]
        public void ConnectionIdleTimeLimitExpired(long transportId, long connectionId, long elapsedMilliseconds)
        {
            this.WriteEvent(50, transportId, connectionId, elapsedMilliseconds);
        }

        /// <summary>
        /// Connection is being disconnected
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="connectionId">Connection ID</param>
        [Event(51, Level = EventLevel.Informational, Version = 1)]
        public void Disconnect(long transportId, long connectionId)
        {
            this.WriteEvent(51, transportId, connectionId);
        }

        /// <summary>
        /// Send queue is full
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="connectionId">Connection ID</param>
        /// <param name="packetId">Packet ID</param>
        /// <param name="packetLength">Length of the packet</param>
        [Event(52, Level = EventLevel.Error, Version = 1)]
        public void SendQueueFull(long transportId, long connectionId, long packetId, int packetLength)
        {
            this.WriteEvent(52, transportId, connectionId, packetId, packetLength);
        }

        /// <summary>
        /// Certificate is not a valid <see cref="System.Security.Cryptography.X509Certificates.X509Certificate2" /> object
        /// </summary>
        /// <param name="serialNumber">Serial number of the certificate</param>
        [Event(53, Level = EventLevel.Error, Version = 1)]
        public void CertificateIsNotAValidX509Certificate2(string serialNumber)
        {
            this.WriteEvent(53, serialNumber);
        }

        /// <summary>
        /// Certificate is not valid
        /// </summary>
        /// <param name="serialNumber">Serial number of the certificate</param>
        /// <param name="notValidBefore">Certificate is not valid before this timestamp</param>
        /// <param name="notValidAfter">Certificate is not valid after this timestamp</param>
        [Event(54, Level = EventLevel.Error, Version = 1)]
        public void CertificateIsNotValid(string serialNumber, string notValidBefore, string notValidAfter)
        {
            this.WriteEvent(54, serialNumber, notValidBefore, notValidAfter);
        }

        /// <summary>
        /// Successfully established connection
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="iteration">Number of attempts</param>
        /// <param name="host">Host name of IP address</param>
        /// <param name="port">Port number</param>
        /// <param name="elapsedMilliseconds">Duration of establishing this connection</param>
        [Event(55, Level = EventLevel.Informational, Version = 1)]
        public void ConnectSucceeded(long transportId, int iteration, string host, int port, long elapsedMilliseconds)
        {
            this.WriteEvent(55, transportId, iteration, host, port, elapsedMilliseconds);
        }

        /// <summary>
        /// Successfully established connection to the given server
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="iteration">Number of attempts</param>
        /// <param name="host">Host name or IP address</param>
        /// <param name="elapsedMilliseconds">Duration of establishing the connection</param>
        [Event(56, Level = EventLevel.Error, Version = 1)]
        public void EstablishConnectionSucceeded(long transportId, int iteration, string host, long elapsedMilliseconds)
        {
            this.WriteEvent(56, transportId, iteration, host, elapsedMilliseconds);
        }

        /// <summary>
        /// Connection that is not established successfully is being closed
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="iteration">Number of attempts</param>
        /// <param name="elapsedMilliseconds">Duration of establishing the connection to failure</param>
        [Event(57, Level = EventLevel.Error, Version = 1)]
        public void EstablishConnectionCloseUnsuccessfulConnection(long transportId, int iteration, long elapsedMilliseconds)
        {
            this.WriteEvent(57, transportId, iteration, elapsedMilliseconds);
        }

        /// <summary>
        /// Successfully accepted an connection
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="iteration">Number of attempts</param>
        /// <param name="remoteEndpoint">Endpoint of the remote party</param>
        /// <param name="elapsedMilliseconds">Duration of accepting connection</param>
        [Event(58, Level = EventLevel.Error, Version = 1)]
        public void AcceptConnectionSucceeded(long transportId, int iteration, string remoteEndpoint, long elapsedMilliseconds)
        {
            this.WriteEvent(58, transportId, iteration, remoteEndpoint, elapsedMilliseconds);
        }

        /// <summary>
        /// Connection that is not accepted successfully is being closed
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="iteration">Number of attempts</param>
        /// <param name="remoteEndpoint">Endpoint of the remote party</param>
        /// <param name="elapsedMilliseconds">Duration of accepting the connection to failure</param>
        [Event(59, Level = EventLevel.Error, Version = 1)]
        public void AcceptConnectionCloseUnsuccessfulConnection(long transportId, int iteration, string remoteEndpoint, long elapsedMilliseconds)
        {
            this.WriteEvent(59, transportId, iteration, remoteEndpoint, elapsedMilliseconds);
        }

        /// <summary>
        /// Certificate has no chain status
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="issuer">Issuer of the certificate</param>
        /// <param name="subject">Subject name of the certificate</param>
        /// <param name="serialNumber">Serial number of the certificate</param>
        /// <param name="thumbprint">Thumbprint of the certificate</param>
        [Event(60, Level = EventLevel.Error, Version = 2)]
        public void CertificateHasNoChainStatus(long transportId, string issuer, string subject, string serialNumber, string thumbprint)
        {
            this.WriteEvent(60, transportId, issuer, subject, serialNumber, thumbprint);
        }

        /// <summary>
        /// Certificate CRL is offline
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="issuer">Issuer of the certificate</param>
        /// <param name="subject">Subject name of the certificate</param>
        /// <param name="serialNumber">Serial number of the certificate</param>
        /// <param name="thumbprint">Thumbprint of the certificate</param>
        [Event(61, Level = EventLevel.Warning, Version = 2)]
        public void CertificateCrlOffline(long transportId, string issuer, string subject, string serialNumber, string thumbprint)
        {
            this.WriteEvent(61, transportId, issuer, subject, serialNumber, thumbprint);
        }

        /// <summary>
        /// Certificate SSL policy errors
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="issuer">Issuer of the certificate</param>
        /// <param name="subject">Subject name of the certificate</param>
        /// <param name="serialNumber">Serial number of the certificate</param>
        /// <param name="thumbprint">Thumbprint of the certificate</param>
        /// <param name="policyErrors">Policy error string</param>
        [Event(62, Level = EventLevel.Error, Version = 2)]
        public void CertificateSslPolicyErrors(long transportId, string issuer, string subject, string serialNumber, string thumbprint, string policyErrors)
        {
            this.WriteEvent(62, transportId, issuer, subject, serialNumber, thumbprint, policyErrors);
        }

        /// <summary>
        /// Certificate trust chain validation is skipped
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="issuer">Issuer of the certificate</param>
        /// <param name="subject">Subject name of the certificate</param>
        /// <param name="serialNumber">Serial number of the certificate</param>
        /// <param name="thumbprint">Thumbprint of the certificate</param>
        [Event(63, Level = EventLevel.Warning, Version = 2)]
        public void CertificateTrustChainValidationSkipped(long transportId, string issuer, string subject, string serialNumber, string thumbprint)
        {
            this.WriteEvent(63, transportId, issuer, subject, serialNumber, thumbprint);
        }

        /// <summary>
        /// Connection lost notification callback is completedj
        /// </summary>
        /// <param name="transportId">Monotically incrementing transport ID</param>
        /// <param name="connectionId">Connection ID</param>
        /// <param name="elapsedMilliseconds">Duration of connection lost notification</param>
        [Event(64, Level = EventLevel.Informational, Version = 1)]
        public void OnConnectionLostNotificationCompleted(long transportId, long connectionId, long elapsedMilliseconds)
        {
            this.WriteEvent(64, transportId, connectionId, elapsedMilliseconds);
        }
    }
}
