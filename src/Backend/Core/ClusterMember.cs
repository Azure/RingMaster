// ***********************************************************************
// Assembly         : RingMaster
// <copyright file="ClusterMember.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// runtime member of the cluster
    /// </summary>
    public class ClusterMember
    {
        public string MemberId { get; set; }
        public IPAddress Address { get; set; }

        public ClusterMember(string memberId, IPAddress address)
        {
            this.MemberId = memberId;
            this.Address = address;
        }
    }
}