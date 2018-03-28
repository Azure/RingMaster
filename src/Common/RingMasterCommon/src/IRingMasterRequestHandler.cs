// <copyright file="IRingMasterRequestHandler.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System;
    using System.Threading.Tasks;
    using RingMaster.Requests;

    /// <summary>
    /// RingMaster request handler interface
    /// </summary>
    public interface IRingMasterRequestHandler : IDisposable
    {
        /// <summary>
        /// Gets or sets the number of milliseconds to wait before a request is timed out.
        /// </summary>
        int Timeout { get; set; }

        /// <summary>
        /// Process a request
        /// </summary>
        /// <param name="request">Request to be processed</param>
        /// <returns>Task that resolves to the response corresponding to the request</returns>
        Task<RequestResponse> Request(IRingMasterRequest request);

        /// <summary>
        /// Close the RequestForwarder.
        /// </summary>
        void Close();
    }
}
