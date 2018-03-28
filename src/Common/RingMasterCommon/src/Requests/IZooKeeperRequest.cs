// <copyright file="IZooKeeperRequest.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    using System;

    /// <summary>
    /// Base interface for every Zookeeper request
    /// </summary>
    public interface IZooKeeperRequest
    {
        /// <summary>
        /// Gets the request id
        /// </summary>
        int Xid { get; }

        /// <summary>
        /// Gets the request type
        /// </summary>
        ZooKeeperRequestType RequestType { get; }
    }
}
