// <copyright file="ProtocolRequestCall.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    using System;

    /// <summary>
    /// ProtocolRequestCall. Allows you to store a protocol request in it.
    /// </summary>
    [Serializable]
    public class ProtocolRequestCall : RequestCall
    {
        /// <summary>
        /// Gets or sets the ZooKeeper or Specific Protocol Request that triggered this call
        /// </summary>
        public object ProtocolRequest { get; set; }
    }
}