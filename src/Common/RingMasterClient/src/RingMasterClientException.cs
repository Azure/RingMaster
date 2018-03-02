// <copyright file="RingMasterClientException.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System;
    using System.Runtime.Serialization;

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

        public enum Code
        {
            RequestQueueFull
        }

        public Code ErrorCode { get; private set; }

        public static RingMasterClientException RequestQueueFull(int queueLength)
        {
            return new RingMasterClientException(Code.RequestQueueFull, $"RequestQueue has reached his configured limit {queueLength}");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Code", this.ErrorCode);
        }
    }
}