// <copyright file="RequestGetAcl.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    using System.Collections.Generic;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Request to get the <see cref="Acl"/>s associated with a node.
    /// </summary>
    public class RequestGetAcl : AbstractRingMasterRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestGetAcl"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="stat">Expected <see cref="IStat"/> values associated with the node</param>
        /// <param name="uid">Unique Id of the request</param>
        public RequestGetAcl(string path, IStat stat, ulong uid = 0)
            : base(RingMasterRequestType.GetAcl, path, uid)
        {
            this.Stat = stat;
        }

        /// <summary>
        /// Gets the expected <see cref="IStat"/> associated with the node.
        /// </summary>
        public IStat Stat { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this this request is read only.
        /// </summary>
        /// <returns><c>true</c> because this request does not modify any data</returns>
        public override bool IsReadOnly()
        {
            return true;
        }
    }
}
