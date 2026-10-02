// <copyright file="RingMasterRequestTimeoutException.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Runtime.Serialization;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// The ring master request timeout exception
    /// </summary>
    /// <seealso cref="System.Exception" />
    [Serializable]
    public class RingMasterRequestTimeoutException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterRequestTimeoutException"/> class.
        /// </summary>
        /// <param name="requestType">Type of the request.</param>
        /// <param name="path">The path.</param>
        public RingMasterRequestTimeoutException(RingMasterRequestType requestType, string path)
            : base($"Request {requestType} on path {path} timed out")
        {
            this.RequestType = requestType;
            this.Path = path;
        }

        /// <summary>
        /// Gets the type of the request.
        /// </summary>
        /// <value>
        /// The type of the request.
        /// </value>
        public RingMasterRequestType RequestType { get; }

        /// <summary>
        /// Gets the path.
        /// </summary>
        /// <value>
        /// The path.
        /// </value>
        public string Path { get; }

        /// <summary>
        /// When overridden in a derived class, sets the <see cref="T:System.Runtime.Serialization.SerializationInfo" /> with information about the exception.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}
