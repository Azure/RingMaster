// <copyright file="RequestInternalRun.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    /// <summary>
    /// Request to run an action.
    /// </summary>
    public class RequestInternalRun : AbstractRingMasterRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestInternalRun"/> class.
        /// </summary>
        /// <param name="path">Path to a node</param>
        /// <param name="uid">Unique Id of the request</param>
        public RequestInternalRun(string path, ulong uid = 0)
            : base(RingMasterRequestType.InternalRun, path, uid)
        {
        }

        /// <summary>
        /// Is this request readonly
        /// </summary>
        /// <returns>true if the request is read only</returns>
        public override bool IsReadOnly()
        {
            return false;
        }
    }
}
