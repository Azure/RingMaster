// <copyright file="IRingMasterRequestHandlerOverlapped.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System;
    using System.Threading.Tasks;
    using RingMaster.Requests;

    /// <summary>
    /// RingMaster request handler interface with overlapped request method
    /// </summary>
    public interface IRingMasterRequestHandlerOverlapped : IRingMasterRequestHandler
    {
        /// <summary>
        /// Process a request
        /// </summary>
        /// <param name="request">Request to be processed</param>
        /// <param name="onCompletion">Action to perform on replication completion or failure</param>
        void RequestOverlapped(IRingMasterRequest request, Action<RequestResponse, Exception> onCompletion);
    }
}
