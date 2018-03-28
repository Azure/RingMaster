// <copyright file="IByteArrayMarshaller.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using RequestResponse = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestResponse;

    /// <summary>
    /// Interface IByteArrayMarshaller abstract the hability to read and write into byte arrays requests and responses
    /// </summary>
    public interface IByteArrayMarshaller
    {
        /// <summary>
        /// Serializes the request as bytes.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>serialized bytes</returns>
        byte[] SerializeRequestAsBytes(RequestCall request);

        /// <summary>
        /// Deserializes the request from bytes.
        /// </summary>
        /// <param name="requestBytes">The request bytes.</param>
        /// <returns>deserialized object</returns>
        RequestCall DeserializeRequestFromBytes(byte[] requestBytes);

        /// <summary>
        /// Serializes the response as bytes.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <returns>serialized bytes.</returns>
        byte[] SerializeResponseAsBytes(RequestResponse response);

        /// <summary>
        /// Deserializes the response from bytes.
        /// </summary>
        /// <param name="responseBytes">The response bytes.</param>
        /// <returns>deserialized object</returns>
        RequestResponse DeserializeResponseFromBytes(byte[] responseBytes);
    }
}