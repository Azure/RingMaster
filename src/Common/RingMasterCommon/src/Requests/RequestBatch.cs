// <copyright file="RequestBatch.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    using System.Collections.Generic;
    using RingMaster.Data;

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
        public RequestBatch(IReadOnlyList<Op> operations, bool completeSynchronously, ulong uid = 0)
            : this(AbstractRingMasterCompoundRequest.GetRequests(operations), completeSynchronously, uid)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestBatch"/> class.
        /// </summary>
        /// <param name="requests">List of <see cref="IRingMasterRequest"/>s to include in the batch</param>
        /// <param name="completeSynchronously"><c>true</c> if the server must ensure durability before returning</param>
        /// <param name="uid">Unique Id of the request</param>
        public RequestBatch(IReadOnlyList<IRingMasterRequest> requests, bool completeSynchronously, ulong uid = 0)
            : base(RingMasterRequestType.Batch, requests, completeSynchronously, uid)
        {
        }
    }
}
