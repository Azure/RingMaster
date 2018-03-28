// <copyright file="IMutableStat.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Data
{
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Interface IStat
    /// </summary>
    public interface IMutableStat : IStat
    {
        /// <summary>
        /// Gets or sets the czxid - Creation Transaction Id.
        /// </summary>
        /// <value>The czxid.</value>
        new long Czxid { get; set; }

        /// <summary>
        /// Gets or sets the mzxid - Modification Transaction Id.
        /// </summary>
        /// <value>The mzxid.</value>
        new long Mzxid { get; set; }

        /// <summary>
        /// Gets or sets the pzxid - Children Changed Transaction Id.
        /// </summary>
        /// <value>The pzxid.</value>
        new long Pzxid { get; set; }

        /// <summary>
        /// Gets or sets the ctime - Create time.
        /// </summary>
        /// <value>The ctime.</value>
        new long Ctime { get; set; }

        /// <summary>
        /// Gets or sets the mtime - Modification time.
        /// </summary>
        /// <value>The mtime.</value>
        new long Mtime { get; set; }

        /// <summary>
        /// Gets or sets the version - Data Version.
        /// </summary>
        /// <value>The version.</value>
        new int Version { get; set; }

        /// <summary>
        /// Gets or sets the cversion - Children list version.
        /// </summary>
        /// <value>The cversion.</value>
        new int Cversion { get; set; }

        /// <summary>
        /// Gets or sets the aversion - Acl version.
        /// </summary>
        /// <value>The aversion.</value>
        new int Aversion { get; set; }

        /// <summary>
        /// Gets or sets the length of the data.
        /// </summary>
        /// <value>The length of the data.</value>
        new int DataLength { get; set; }

        /// <summary>
        /// Gets or sets the number children.
        /// </summary>
        /// <value>The number children.</value>
        new int NumChildren { get; set; }
    }
}
