// <copyright file="AbstractRingMasterCompoundRequest.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    using System;
    using System.Collections.Generic;
    using RingMaster.Data;

    /// <summary>
    /// Base class for <see cref="IRingMasterRequest"/>s that are composed of other requests.
    /// </summary>
    public abstract class AbstractRingMasterCompoundRequest : AbstractRingMasterRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractRingMasterCompoundRequest"/> class.
        /// </summary>
        /// <param name="requestType">Type of the compound request</param>
        /// <param name="requests">List of requests</param>
        /// <param name="completeSynchronously"><c>true</c> if the server must ensure durability before returning</param>
        /// <param name="uid">Unique Id of the request</param>
        internal AbstractRingMasterCompoundRequest(RingMasterRequestType requestType, IReadOnlyList<IRingMasterRequest> requests, bool completeSynchronously, ulong uid)
            : base(requestType, string.Empty, uid)
        {
            this.Requests = requests;
            this.CompleteSynchronously = completeSynchronously;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this operation needs to be completed synchronously (i.e. the server must ensure durability before returning)
        /// </summary>
        public bool CompleteSynchronously { get; set; }

        /// <summary>
        /// Gets the requests associated with this compound request.
        /// </summary>
        public IReadOnlyList<IRingMasterRequest> Requests { get; }

        /// <summary>
        /// Is this request readonly
        /// </summary>
        /// <returns>true if the request is read only</returns>
        public override bool IsReadOnly()
        {
            if (this.Requests == null || this.Requests.Count == 0)
            {
                return true;
            }

            foreach (IRingMasterRequest r in this.Requests)
            {
                if (!r.IsReadOnly())
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets <see cref="IRingMasterRequest"/> that correspond to the given <see cref="Op"/>s.
        /// </summary>
        /// <param name="ops">List of operations</param>
        /// <returns>List of <see cref="IRingMasterRequest"/>s</returns>
        protected static IReadOnlyList<IRingMasterRequest> GetRequests(IReadOnlyList<Op> ops)
        {
            if (ops == null)
            {
                throw new ArgumentNullException("ops");
            }

            IRingMasterRequest[] requests = new IRingMasterRequest[ops.Count];
            for (int i = 0; i < requests.Length; i++)
            {
                requests[i] = ops[i].Request;
            }

            return requests;
        }
    }
}
