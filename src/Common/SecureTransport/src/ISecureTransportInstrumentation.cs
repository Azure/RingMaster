// <copyright file="ISecureTransportInstrumentation.cs" company="Microsoft">
//     Copyright ©  2015
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
    }
}