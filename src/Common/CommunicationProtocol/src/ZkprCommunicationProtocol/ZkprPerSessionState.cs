// <copyright file="ZkprPerSessionState.cs" company="Microsoft">
//     Copyright ©  2017
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol
{
    using System;
    using Communication;

    public class ZkprPerSessionState : ISessionState
    {
        public ZkprPerSessionState(ulong sessionId)
        {
            this.SessionId = sessionId;
        }

        /// <summary>
        /// The Session Id.
        /// </summary>
        public ulong SessionId { get; }

        /// <summary>
        /// Have we received a connect yet.
        /// </summary>
        public bool ConnectRecieved { get; set; } = false;

        public override string ToString()
        {
            return $"SessionId:{this.SessionId} ConnectRecieved:{this.ConnectRecieved}";
        }
    }
}
