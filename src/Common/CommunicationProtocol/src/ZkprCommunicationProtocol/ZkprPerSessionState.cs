// <copyright file="ZkprPerSessionState.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol
{
    using System;
    using Communication;

    /// <summary>
    /// State per ZooKeeper client session
    /// </summary>
    public class ZkprPerSessionState : ISessionState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ZkprPerSessionState"/> class.
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        public ZkprPerSessionState(ulong sessionId)
        {
            this.SessionId = sessionId;
        }

        /// <summary>
        /// Gets the Session Id.
        /// </summary>
        public ulong SessionId { get; }

        /// <summary>
        /// Gets or sets a value indicating whether we received a connect yet.
        /// </summary>
        public bool ConnectRecieved { get; set; } = false;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"SessionId:{this.SessionId} ConnectRecieved:{this.ConnectRecieved}";
        }
    }
}
