// <copyright file="IRingMasterRequest.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    using System;
    using RingMaster.Data;

    /// <summary>
    /// Interface IRingMasterRequest
    /// </summary>
    public interface IRingMasterRequest
    {
        /// <summary>
        /// Gets the type of the request.
        /// </summary>
        RingMasterRequestType RequestType { get; }

        /// <summary>
        /// Gets the unique id of the request.
        /// </summary>
        ulong Uid { get; }

        /// <summary>
        /// Gets or sets the time stream associated with this request.
        /// </summary>
        ulong TimeStreamId { get; set; }

        /// <summary>
        /// Gets or sets the path of the node associated with this request.
        /// </summary>
        string Path { get; set; }

        /// <summary>
        /// Gets or sets the authentication credentials to use for this request or <c>null</c> of the session's authentication credentials must be used.
        /// </summary>
        ISessionAuth Auth { get; set; }

        /// <summary>
        /// Gets or sets the information about which transaction id and transaction time to use for this request or null to indicate that ring master must
        /// assign those values.
        /// </summary>
        IOperationOverrides Overrides { get; set; }

        /// <summary>
        /// Gets or sets the execution queue id to queue this command into
        /// </summary>
        Guid ExecutionQueueId { get; set; }

        /// <summary>
        /// Gets or sets the maximum time allowed for this request to be waiting on the execution queue
        /// </summary>
        int ExecutionQueueTimeoutMillis { get; set; }

        /// <summary>
        /// Gets a value indicating whether this request is readonly
        /// </summary>
        /// <returns><c>true</c> if this request is read only</returns>
        bool IsReadOnly();
    }
}
