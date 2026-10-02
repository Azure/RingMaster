// <copyright file="ISecureTransportInstrumentation.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport
{
    using System;
    using System.Net;

    /// <summary>
    /// Interface that is used by <see cref="SecureTransport"/> to report
    /// metrics.
    /// </summary>
    public interface ISecureTransportInstrumentation
    {
        /// <summary>
        /// Starts the server.
        /// </summary>
        /// <param name="transportId">The transport identifier.</param>
        /// <param name="endpoint">The endpoint.</param>
        void ListenerStarted(long transportId, EndPoint endpoint);

        /// <summary>
        /// Listeners the stopped.
        /// </summary>
        /// <param name="transportId">The transport identifier.</param>
        /// <param name="endpoint">The endpoint.</param>
        void ListenerStopped(long transportId, EndPoint endpoint);

        /// <summary>
        /// A connection with a server was established successfully.
        /// </summary>
        /// <param name="serverEndPoint">Address of the server</param>
        /// <param name="serverIdentity">Identity of the server</param>
        /// <param name="setupTime">Time taken to establish the connection</param>
        void ConnectionEstablished(IPEndPoint serverEndPoint, string serverIdentity, TimeSpan setupTime);

        /// <summary>
        /// An attempt to establish connection with one or more servers failed.
        /// </summary>
        /// <param name="processingTime">Time spent trying to establish connection</param>
        void EstablishConnectionFailed(TimeSpan processingTime);

        /// <summary>
        /// A connection request from a client was accepted successfully
        /// </summary>
        /// <param name="clientEndPoint">Address of the client</param>
        /// <param name="clientIdentity">Identity of the client</param>
        /// <param name="setupTime">Time taken to accept the connection</param>
        void ConnectionAccepted(IPEndPoint clientEndPoint, string clientIdentity, TimeSpan setupTime);

        /// <summary>
        /// A new connection was created.
        /// </summary>
        /// <param name="connectionId">Unique Id of the connection</param>
        /// <param name="remoteEndPoint">The remote endpoint</param>
        /// <param name="remoteIdentity">Identity of the remote endpoint</param>
        void ConnectionCreated(long connectionId, IPEndPoint remoteEndPoint, string remoteIdentity);

        /// <summary>
        /// An existing connection was closed.
        /// </summary>
        /// <param name="connectionId">Unique Id of the connection</param>
        /// <param name="remoteEndPoint">The remote endpoint</param>
        /// <param name="remoteIdentity">Identity of the remote endpoint</param>
        void ConnectionClosed(long connectionId, IPEndPoint remoteEndPoint, string remoteIdentity);

        /// <summary>
        /// A connection request from a client was not accepted.
        /// </summary>
        /// <param name="clientEndPoint">EndPoint of the client that attempted to connect</param>
        /// <param name="processingTime">Time spent processing the connection request</param>
        void AcceptConnectionFailed(IPEndPoint clientEndPoint, TimeSpan processingTime);

        /// <summary>
        /// Outgoing packet queued.
        /// </summary>
        /// <param name="transportId">The transport identifier.</param>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="queueLength">Length of the queue.</param>
        /// <param name="packetLength">Length of the packet.</param>
        void OutgoingPacketQueued(long transportId, long connectionId, int queueLength, int packetLength);

        /// <summary>
        /// Outgoing queue full.
        /// </summary>
        /// <param name="transportId">The transport identifier.</param>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="pendingPacketCount">The pending packet count.</param>
        void OutgoingQueueFull(long transportId, long connectionId, int pendingPacketCount);

        /// <summary>
        /// Outgoing packet sent.
        /// </summary>
        /// <param name="transportId">The transport identifier.</param>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="packetLength">Length of the packet.</param>
        void OutgoingPacketSent(long transportId, long connectionId, int packetLength);
    }
}