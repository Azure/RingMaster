// <copyright file="ICommunicationProtocol.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication
{
    using System.Threading.Tasks;
    using RingMaster.Requests;

    /// <summary>
    /// Interface to the protocol used to send requests and receive responses.
    /// </summary>
    public interface ICommunicationProtocol
    {
        /// <summary>
        /// Gets a delegate that receives a packet from the wire. if null it tells the server to use default
        /// </summary>
        /// <returns>Either a null or a PacketReceiver Delegate</returns>
        PacketReceiveDelegate PacketReciever { get; }

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
        /// Serialize a <see cref="RequestCall"/>.
        /// </summary>
        /// <param name="request">Request to serialize</param>
        /// <param name="version">Serialization protocol version to use</param>
        /// <returns>Serialized representation of the given request</returns>
        byte[] SerializeRequest(RequestCall request, uint version);

        /// <summary>
        /// Serialize a <see cref="RequestResponse"/>.
        /// </summary>
        /// <param name="response">Response to serialize</param>
        /// <param name="version">Serialization protocol version to use</param>
        /// <returns>Serialized representation of the given response</returns>
        byte[] SerializeResponse(RequestResponse response, uint version);

        /// <summary>
        /// Deserialize a <see cref="RequestCall"/>.
        /// </summary>
        /// <param name="serializedRequest">Serialized representation of the request</param>
        /// <param name="serializedRequestLength">Length of serialized request</param>
        /// <param name="version">Serialization protocol version to use</param>
        /// <returns>The deserialized <see cref="RequestCall"/></returns>
        RequestCall DeserializeRequest(byte[] serializedRequest, int serializedRequestLength, uint version);

        /// <summary>
        /// Deserialize a <see cref="RequestResponse"/>.
        /// </summary>
        /// <param name="serializedResponse">Serialized representation of the response</param>
        /// <param name="version">Serialization protocol version to use</param>
        /// <returns>The deserialized <see cref="RequestResponse"/></returns>
        RequestResponse DeserializeResponse(byte[] serializedResponse, uint version);
    }
}