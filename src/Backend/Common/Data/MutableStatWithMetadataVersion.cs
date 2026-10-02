// <copyright file="MutableStatWithMetadataVersion.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Data
{
    using System;

    /// <summary>
    /// Mutable stat with user metadata field.
    /// </summary>
    /// <seealso cref="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Data.MutableStat" />
    [Serializable]
    public sealed class MutableStatWithMetadataVersion : MutableStat
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MutableStatWithMetadataVersion" /> class.
        /// </summary>
        /// <param name="czxid">The czxid.</param>
        /// <param name="mzxid">The mzxid.</param>
        /// <param name="ctime">The ctime.</param>
        /// <param name="mtime">The mtime.</param>
        /// <param name="version">The version.</param>
        /// <param name="cversion">The cversion.</param>
        /// <param name="aversion">The aversion.</param>
        /// <param name="dataLength">Length of the data.</param>
        /// <param name="numChildren">The number children.</param>
        /// <param name="numEphemeralChildren">The number ephemeral children.</param>
        /// <param name="pzxid">The pzxid.</param>
        /// <param name="uversion">The uversion.</param>
        public MutableStatWithMetadataVersion(long czxid, long mzxid, long ctime, long mtime, int version, int cversion, int aversion, int dataLength, int numChildren, int numEphemeralChildren, long pzxid, int uversion)
            : base(czxid, mzxid, ctime, mtime, version, cversion, aversion, dataLength, numChildren, numEphemeralChildren, pzxid)
        {
            this.Uversion = uversion;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MutableStatWithMetadataVersion"/> class.
        /// </summary>
        /// <param name="other">The other.</param>
        public MutableStatWithMetadataVersion(IMutableStat other)
            : base(other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            this.Uversion = other.Uversion;
        }

        /// <summary>
        /// Gets or sets the version number of the most recent change to this node's user metadata.
        /// </summary>
        public override int Uversion { get; set; }

        /// <summary>
        /// Determines whether the specified <see cref="object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            MutableStatWithMetadataVersion other = obj as MutableStatWithMetadataVersion;
            if (other == null)
            {
                return false;
            }

            if (!base.Equals(obj))
            {
                return false;
            }

            return this.Uversion == other.Uversion;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            hash ^= this.Uversion;

            return hash;
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{base.ToString()} uversion:{this.Uversion}";
        }
    }
}
