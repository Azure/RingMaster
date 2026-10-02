// <copyright file="RequestBatch.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    using System.Collections.Generic;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Request to execute a list of <see cref="IRingMasterRequest"/>s as a batch.
    /// </summary>
    public class RequestBatch : AbstractRingMasterCompoundRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestBatch"/> class.
        /// </summary>
        /// <param name="operations">List of <see cref="Op"/>s to include in the batch</param>
        /// <param name="completeSynchronously"><c>true</c> if the server must ensure durability before returning</param>
        /// <param name="uid">Unique Id of the request</param>
        /// <param name="invokeCallbackBeforeComplete">If invoke callback before complete</param>
        public RequestBatch(IReadOnlyList<Op> operations, bool completeSynchronously, ulong uid = 0, bool invokeCallbackBeforeComplete = false)
            : this(AbstractRingMasterCompoundRequest.GetRequests(operations), completeSynchronously, uid, invokeCallbackBeforeComplete)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestBatch"/> class.
        /// </summary>
        /// <param name="requests">List of <see cref="IRingMasterRequest"/>s to include in the batch</param>
        /// <param name="completeSynchronously"><c>true</c> if the server must ensure durability before returning</param>
        /// <param name="uid">Unique Id of the request</param>
        /// <param name="invokeCallbackBeforeComplete">If invoke callback before complete</param>
        public RequestBatch(IReadOnlyList<IRingMasterRequest> requests, bool completeSynchronously, ulong uid = 0, bool invokeCallbackBeforeComplete = false)
            : base(RingMasterRequestType.Batch, requests, completeSynchronously, uid, invokeCallbackBeforeComplete)
        {
        }
    }
}
