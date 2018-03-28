// <copyright file="ClusterMember.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence
{
    using System.Net;

    /// <summary>
    /// runtime member of the cluster
    /// </summary>
    public class ClusterMember
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterMember"/> class.
        /// </summary>
        /// <param name="memberId">Member ID of the cluster</param>
        /// <param name="address">IP Address of the cluster</param>
        public ClusterMember(string memberId, IPAddress address)
        {
            this.MemberId = memberId;
            this.Address = address;
        }

        /// <summary>
        /// Gets or sets the member ID
        /// </summary>
        public string MemberId { get; set; }

        /// <summary>
        /// Gets or sets the address of the cluster
        /// </summary>
        public IPAddress Address { get; set; }
    }
}
