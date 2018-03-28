// <copyright file="BehaviorAction.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    /// <summary>
    /// Behavior of processing requests
    /// </summary>
    public enum BehaviorAction
    {
        /// <summary>
        /// Request is allowed
        /// </summary>
        AllowRequest = 1,

        /// <summary>
        /// Request is failed
        /// </summary>
        FailRequest,
    }
}
