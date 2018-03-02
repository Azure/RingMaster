// <copyright file="IRingMasterRequestExecutor.cs" company="Microsoft">
//     Copyright 2017
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// Interface to an object that can execute RingMaster requests.
    /// </summary>
    public interface IRingMasterRequestExecutor
    {
        /// <summary>
        /// Processes a session initialization request.
        /// </summary>
        /// <param name="call">The initialization request</param>
        /// <param name="session">The session to be initialized</param>
        /// <returns>Response for the initialization request</returns>
        RequestResponse ProcessSessionInitialization(RequestCall call, ClientSession session);

        /// <summary>
        /// Process the given request.
        /// </summary>
        /// <param name="request">Request that must be processed</param>
        /// <param name="session">Client session associated with the request</param>
        /// <param name="onCompletion">Action that must be invoked when the request is completed</param>
        void ProcessMessage(IRingMasterBackendRequest request, ClientSession session, Action<RequestResponse> onCompletion);
    }
}