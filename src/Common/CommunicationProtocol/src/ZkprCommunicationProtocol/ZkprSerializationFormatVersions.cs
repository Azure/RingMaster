// <copyright file="ZkprSerializationFormatVersions.cs" company="Microsoft">
//     Copyright 2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Serialization format versions.
    /// </summary>
    public class ZkprSerializationFormatVersions
    {
        /// <summary>
        /// The minimum supported serialization version.
        /// </summary>
        public const uint MinimumSupportedVersion = Version1;

        /// <summary>
        /// The maximum supported serialization version.
        /// </summary>
        public const uint MaximumSupportedVersion = Version1;

        /// <summary>
        /// The very first protocol version.
        /// </summary>
        public const uint Version1 = 1;
    }
}