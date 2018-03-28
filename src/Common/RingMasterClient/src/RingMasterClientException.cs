// <copyright file="RingMasterClientException.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Ring master client exception
    /// </summary>
    [Serializable]
    public class RingMasterClientException : Exception
    {
        private RingMasterClientException(Code errorCode, string message)
            : base(message)
        {
            this.ErrorCode = errorCode;
        }

        private RingMasterClientException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.ErrorCode = (Code)info.GetValue("Code", typeof(Code));
        }

        /// <summary>
        /// Error code
        /// </summary>
        public enum Code
        {
            /// <summary>
            /// Request queue is full
            /// </summary>
            RequestQueueFull,
        }

        /// <summary>
        /// Gets the error code of the exception
        /// </summary>
        public Code ErrorCode { get; private set; }

        /// <summary>
        /// Creates an exception with RequestQueueFull code
        /// </summary>
        /// <param name="queueLength">Length of the queue</param>
        /// <returns>Exception created</returns>
        public static RingMasterClientException RequestQueueFull(int queueLength)
        {
            return new RingMasterClientException(Code.RequestQueueFull, $"RequestQueue has reached his configured limit {queueLength}");
        }

        /// <inheritdoc />
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Code", this.ErrorCode);
        }
    }
}