// <copyright file="IClientConnectionInstrumentation.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using Code = Microsoft.Azure.Networking.Infrastructure.RingMaster.Data.RingMasterException.Code;

    /// <summary>
    /// This interface abstracts a client connection instrumentation.
    /// </summary>
    public interface IClientConnectionInstrumentation
    {
        /// <summary>
        /// Start operation trace.
        /// </summary>
        /// <param name="operation">Operation type</param>
        /// <param name="operationId">Operation Id</param>
        void StartOperation(RepositoryOperation operation, Guid operationId);

        /// <summary>
        /// End operation trace.
        /// </summary>
        /// <param name="operation">Operation type</param>
        /// <param name="operationId">Operation Id</param>
        /// <param name="ringMasterCode">Ring master return code</param>
        /// <param name="elapsedMilliseconds">Elapsed milliseconds</param>
        void EndOperation(RepositoryOperation operation, Guid operationId, int ringMasterCode, long elapsedMilliseconds);

        /// <summary>
        /// Fires when request is sent for processing.
        /// Reports request queue length waiting for response from server.
        /// </summary>
        /// <param name="queueLength">Length of a queue of requests awaiting for response</param>
        void ReportAwaitingRequestQueue(int queueLength);

        /// <summary>
        /// Fires when request is sent for processing.
        /// Reports request queue length waiting to be sent to the server.
        /// </summary>
        /// <param name="queueLength">Length of a queue of requests awaiting to be sent from client</param>
        void ReportOutgoingRequestQueue(int queueLength);

        /// <summary>
        /// Fires when request is dropped before sending to the queue.
        /// </summary>
        /// <param name="request">Request being dropped</param>
        /// <param name="error">Drop reason</param>
        void ReportDroppedRequest(IRingMasterBackendRequest request, Code error);
    }
}
