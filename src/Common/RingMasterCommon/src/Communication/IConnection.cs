// <copyright file="IConnection.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// Delegate to receive packets from the wire. The packet format is specific to the protocol like RingMaster, Zookeeper etc.
    /// </summary>
    /// <param name="stream">The stream to read from</param>
    /// <returns>a byte array as the packet</returns>
    public delegate Task<byte[]> PacketReceiveDelegate(Stream stream);

    /// <summary>
    /// Delegate to negotiate a protocol version from the wire. The protocol version negotiation is specific to the protocol like RingMaster, Zookeeper etc.
    /// </summary>
    /// <param name="localProtocolVersion">The local protocol version to compare against</param>
    /// <returns>a <c>uint</c> indicating the negotiated protocol version</returns>
    public delegate Task<uint> ProtocolNegotiatorDelegate(uint localProtocolVersion);

    /// <summary>
    /// Interface to a connection established by the <see cref="ITransport"/>.
    /// </summary>
    public interface IConnection : IDisposable
    {
        /// <summary>
        /// Gets the unique id assigned to this connection by the transport.
        /// </summary>
        ulong Id { get; }

        /// <summary>
        /// Gets the remote endpoint of this connection.
        /// </summary>
        EndPoint RemoteEndPoint { get; }

        /// <summary>
        /// Gets the identity of the remote endpoint if mutual authentication was used
        /// </summary>
        string RemoteIdentity { get; }

        /// <summary>
        /// Gets the negotiated protocol version.
        /// </summary>
        uint ProtocolVersion { get; }

        /// <summary>
        /// Gets or sets the callback that must be invoked when a packet is received.
        /// </summary>
        Action<byte[]> OnPacketReceived { get; set; }

        /// <summary>
        /// Gets or sets the callback that must be invoked when this connection is lost.
        /// </summary>
        Action OnConnectionLost { get; set; }

        /// <summary>
        /// Gets or sets the callback that should be invoked if the incoming packet is not in the standard
        /// RingMaster format of length + data. This allows other protocols which do not follow
        /// this format to provide their own implementation
        /// </summary>
        PacketReceiveDelegate DoPacketReceive { get; set; }

        /// <summary>
        /// Gets or sets the callback that should be invoked if the incoming packet is not in the standard
        /// RingMaster. This allows other protocols like Zookeeper which do not do protocol negotiation via first 4 bytes.
        /// </summary>
        ProtocolNegotiatorDelegate DoProtocolNegotiation { get; set; }

        /// <summary>
        /// Send a packet to the remote endpoint.
        /// </summary>
        /// <param name="packet">Packet to send</param>
        void Send(byte[] packet);

        /// <summary>
        /// Send a packet to the remote endpoint.
        /// </summary>
        /// <param name="packet">Packet to send</param>
        /// <returns>A Task that tracks completion of the send</returns>
        Task SendAsync(byte[] packet);

        /// <summary>
        /// Disconnect the connection.
        /// </summary>
        void Disconnect();
    }
}