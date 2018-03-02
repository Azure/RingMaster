// <copyright file="ZkprCommunicationProtocol.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// WireProtocol used for ringmaster requests and responses.
    /// </summary>
    public class ZkprCommunicationProtocol : IZooKeeperCommunicationProtocol
    {
        /// <summary>
        /// Maximum supported protocol version.
        /// </summary>
        public const uint MaximumSupportedVersion = ZkprSerializationFormatVersions.MaximumSupportedVersion;

        /// <summary>
        /// Minimum supported protocol version.
        /// </summary>
        public const uint MinimumSupportedVersion = ZkprSerializationFormatVersions.MinimumSupportedVersion;

        /// <summary>
        /// Gets a function that recieves a packet from the wire. return null if you want to use the default receiver
        /// </summary>
        public PacketReceiveDelegate PacketReceiver
        {
            get
            {
                return this.PacketReceive;
            }
        }

        /// <summary>
        /// Gets a function that does protocol negotiation. return null if you want to use the default negotiator
        /// </summary>
        public ProtocolNegotiatorDelegate ProtocolNegotiator
        {
            get
            {
                return this.NegotiateProtocol;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether to read using network byte order
        /// </summary>
        public bool UseNetworkByteOrderFlag { get; } = true;

        /// <summary>
        /// Function that serializes the Watcher Response
        /// </summary>
        /// <param name="response">the Watcher Response</param>
        /// <param name="version">Version of the serializer</param>
        /// <returns>A byte array representing the serialized response</returns>
        public byte[] SerializeWatcherResponse(RequestResponse response, uint version)
        {
            using (var serializer = new ZkprSerializer(version))
            {
                serializer.SerializeWatcherResponse(response);
                return serializer.GetBytes();
            }
        }

        /// <summary>
        /// Serialize a <see cref="RequestResponse"/>.
        /// </summary>
        /// <param name="response">Response to serialize</param>
        /// <param name="version">Serialization protocol version to use</param>
        /// <param name="zkprRequest">The Zookeeper request</param>
        /// <returns>Serialized representation of the given response</returns>
        public byte[] SerializeResponse(RequestResponse response, uint version, IZooKeeperRequest zkprRequest)
        {
            using (var serializer = new ZkprSerializer(version))
            {
                serializer.SerializeResponse(response, zkprRequest);
                return serializer.GetBytes();
            }
        }

        /// <summary>
        /// Deserialize a <see cref="RequestCall"/>.
        /// </summary>
        /// <param name="serializedRequest">Serialized representation of the request</param>
        /// <param name="serializedRequestLength">Length of serialized request</param>
        /// <param name="version">Serialization protocol version to use</param>
        /// <param name="sessionState">The Session State</param>
        /// <returns>The deserialized <see cref="RequestCall"/></returns>
        public ProtocolRequestCall DeserializeRequest(byte[] serializedRequest, int serializedRequestLength, uint version, ISessionState sessionState)
        {
            using (var deserializer = new ZkprDeserializer(serializedRequest, serializedRequestLength, version))
            {
                ZkprPerSessionState zkprSessionState = sessionState as ZkprPerSessionState;
                ProtocolRequestCall theCall = deserializer.DeserializeRequest(zkprSessionState);
                return theCall;
            }
        }

        /// <summary>
        /// Negotiate the protocol version to be used for communication.
        /// </summary>
        /// <param name="localProtocolVersion">Maximum protocol version supported by this transport</param>
        /// <returns>A <see cref="Task"/> that resolves to the negotiated protocol version</returns>
        private async Task<uint> NegotiateProtocol(uint localProtocolVersion)
        {
            await Task.Yield();
            return localProtocolVersion;
        }

        private async Task<byte[]> PacketReceive(Stream stream)
        {
            byte[] packetLengthBytes = await this.ReadBytes(stream, 4).ConfigureAwait(false);
            if (packetLengthBytes != null)
            {
                int packetLength = System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt32(packetLengthBytes, 0));

                return await this.ReadBytes(stream, packetLength).ConfigureAwait(false);
            }

            return null;
        }

        /// <summary>
        /// Read the specified number of bytes from the stream that represents the connection.
        /// </summary>
        /// <param name="stream">Stream from which the bytes must be read</param>
        /// <param name="length">Number of bytes to read</param>
        /// <returns>The data that was read or null if there is no more data</returns>
        private async Task<byte[]> ReadBytes(Stream stream, int length)
        {
            int totalRead = 0;
            int bytesRemaining = length;
            byte[] buffer = new byte[length];

            while (bytesRemaining > 0)
            {
                // ReadAsync could return 0 if end of stream has been reached.
                int bytesRead = await stream.ReadAsync(buffer, totalRead, bytesRemaining, CancellationToken.None).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    break;
                }

                totalRead += bytesRead;
                bytesRemaining -= bytesRead;
            }

            if (totalRead == buffer.Length)
            {
                return buffer;
            }

            return null;
        }
    }
}