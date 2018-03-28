// <copyright file="RingMasterCommunicationProtocol.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol
{
    using System;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// WireProtocol used for ringmaster requests and responses.
    /// </summary>
    public class RingMasterCommunicationProtocol : ICommunicationProtocol
    {
        /// <summary>
        /// Maximum supported protocol version.
        /// </summary>
        public const uint MaximumSupportedVersion = SerializationFormatVersions.MaximumSupportedVersion;

        /// <summary>
        /// Minimum supported protocol version.
        /// </summary>
        public const uint MinimumSupportedVersion = SerializationFormatVersions.MinimumSupportedVersion;

        /// <summary>
        /// Gets a function that receives a packet from the wire. return null if you want to use the default receiver
        /// </summary>
        public PacketReceiveDelegate PacketReciever { get; } = null;

        /// <summary>
        /// Gets a function that does protocol negotiation. return null if you want to use the default negotiator
        /// </summary>
        public ProtocolNegotiatorDelegate ProtocolNegotiator { get; } = null;

        /// <summary>
        /// Gets a value indicating whether to read using network byte order
        /// </summary>
        public bool UseNetworkByteOrderFlag { get; } = false;

        /// <summary>
        /// Serialize a <see cref="RequestCall"/>.
        /// </summary>
        /// <param name="request">Request to serialize</param>
        /// <param name="version">Serialization protocol version to use</param>
        /// <returns>Serialized representation of the given request</returns>
        public byte[] SerializeRequest(RequestCall request, uint version)
        {
            using (var serializer = new Serializer(version))
            {
                serializer.SerializeRequest(request);
                return serializer.GetBytes();
            }
        }

        /// <summary>
        /// Serialize a <see cref="RequestResponse"/>.
        /// </summary>
        /// <param name="response">Response to serialize</param>
        /// <param name="version">Serialization protocol version to use</param>
        /// <returns>Serialized representation of the given response</returns>
        public byte[] SerializeResponse(RequestResponse response, uint version)
        {
            using (var serializer = new Serializer(version))
            {
                serializer.SerializeResponse(response);
                return serializer.GetBytes();
            }
        }

        /// <summary>
        /// Deserialize a <see cref="RequestCall"/>.
        /// </summary>
        /// <param name="serializedRequest">Serialized representation of the request</param>
        /// <param name="serializedRequestLength">Length of serialized request</param>
        /// <param name="version">Serialization protocol version to use</param>
        /// <returns>The deserialized <see cref="RequestCall"/></returns>
        public RequestCall DeserializeRequest(byte[] serializedRequest, int serializedRequestLength, uint version)
        {
            using (var deserializer = new Deserializer(serializedRequest, serializedRequestLength, version))
            {
                return deserializer.DeserializeRequest();
            }
        }

        /// <summary>
        /// Deserialize a <see cref="RequestResponse"/>.
        /// </summary>
        /// <param name="serializedResponse">Serialized representation of the response</param>
        /// <param name="version">Serialization protocol version to use</param>
        /// <returns>The deserialized <see cref="RequestResponse"/></returns>
        public RequestResponse DeserializeResponse(byte[] serializedResponse, uint version)
        {
            if (serializedResponse == null)
            {
                throw new ArgumentNullException("serializedResponse");
            }

            using (var deserializer = new Deserializer(serializedResponse, serializedResponse.Length, version))
            {
                return deserializer.DeserializeResponse();
            }
        }
    }
}
