// <copyright file="IZooKeeperCommunicationProtocol.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication
{
    using System.Threading.Tasks;
    using RingMaster.Requests;

    /// <summary>
    /// Interface to the protocol used to send requests and receive responses.
    /// </summary>
    public interface IZooKeeperCommunicationProtocol
    {
        /// <summary>
        /// Gets a delegate that receives a packet from the wire. if null it tells the server to use default
        /// </summary>
        /// <returns>Either a null or a PacketReceiver Delegate</returns>
        PacketReceiveDelegate PacketReceiver { get; }

        /// <summary>
        /// Gets a delegate that receives a packet from the wire. if null it tells the server to use default
        /// </summary>
        /// <returns>Either a null or a PacketReceiver Delegate</returns>
        ProtocolNegotiatorDelegate ProtocolNegotiator { get; }

        /// <summary>
        /// Gets a value indicating whether the underlying send should use Network byte order when adding length etc.
        /// </summary>
        /// <returns>a flag indicating whether Network byte order should be used</returns>
        bool UseNetworkByteOrderFlag { get; }

        /// <summary>
        /// Watcher responses are special. They are self generated and do not depend on previous request
        /// </summary>
        /// <param name="response">The response from a watched event</param>
        /// <param name="version">Serialization protocol version to use</param>
        /// <returns>Serialized representation of the given response</returns>
        byte[] SerializeWatcherResponse(RequestResponse response, uint version);

        /// <summary>
        /// Serialize a <see cref="RequestResponse"/>.
        /// </summary>
        /// <param name="response">Response to serialize</param>
        /// <param name="version">Serialization protocol version to use</param>
        /// <param name="zkprRequest">The Zookeeper Request</param>
        /// <returns>Serialized representation of the given response</returns>
        byte[] SerializeResponse(RequestResponse response, uint version, IZooKeeperRequest zkprRequest);

        /// <summary>
        /// Deserialize a <see cref="RequestCall"/>.
        /// </summary>
        /// <param name="serializedRequest">Serialized representation of the request</param>
        /// <param name="serializedRequestLength">Length of serialized request</param>
        /// <param name="version">Serialization protocol version to use</param>
        /// <param name="sessionState">The Session State</param>
        /// <returns>The deserialized <see cref="RequestCall"/></returns>
        ProtocolRequestCall DeserializeRequest(byte[] serializedRequest, int serializedRequestLength, uint version, ISessionState sessionState);
    }
}