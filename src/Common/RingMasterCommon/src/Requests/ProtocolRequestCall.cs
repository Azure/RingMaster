// <copyright file="ProtocolRequestCall.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    using System;

    /// <summary>
    /// ProtocolRequestCall. Allows you to store a protocol request in it.
    /// </summary>
    [Serializable]
    public class ProtocolRequestCall : RequestCall
    {
        /// <summary>
        /// Gets or sets the ZooKeeper or Specific Protocol Request that triggered this call
        /// </summary>
        public object ProtocolRequest { get; set; }
    }
}