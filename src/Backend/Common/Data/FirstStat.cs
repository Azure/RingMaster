// <copyright file="FirstStat.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Data
{
    using System;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Class FirstStat.
    /// </summary>
    [Serializable]
    public sealed class FirstStat : IMutableStat
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FirstStat"/> class.
        /// </summary>
        /// <param name="other">The other.</param>
        public FirstStat(IStat other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            this.Czxid = other.Czxid;
            this.Ctime = other.Ctime;
            this.DataLength = other.DataLength;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FirstStat"/> class.
        /// </summary>
        /// <param name="zxid">The zxid.</param>
        /// <param name="time">The time.</param>
        /// <param name="dataLength">Length of the data.</param>
        public FirstStat(long zxid, long time, int dataLength)
        {
            this.Czxid = zxid;
            this.Ctime = time;
            this.DataLength = dataLength;
        }

        /// <summary>
        /// Gets or sets the czxid.
        /// </summary>
        /// <value>The czxid.</value>
        public long Czxid { get; set; }

        /// <summary>
        /// Gets or sets the mzxid.
        /// </summary>
        /// <value>The mzxid.</value>
        /// <exception cref="System.InvalidOperationException">Set is not allowed</exception>
        public long Mzxid
        {
            get { return this.Czxid; }
            set { throw new InvalidOperationException(); }
        }

        /// <summary>
        /// Gets or sets the pzxid.
        /// </summary>
        /// <value>The pzxid.</value>
        /// <exception cref="System.InvalidOperationException">Set is not allowed</exception>
        public long Pzxid
        {
            get { return this.Czxid; }
            set { throw new InvalidOperationException(); }
        }

        /// <summary>
        /// Gets or sets the ctime.
        /// </summary>
        /// <value>The ctime.</value>
        public long Ctime { get; set; }

        /// <summary>
        /// Gets or sets the mtime.
        /// </summary>
        /// <value>The mtime.</value>
        /// <exception cref="System.InvalidOperationException">Set is not allowed</exception>
        public long Mtime
        {
            get { return this.Ctime; }
            set { throw new InvalidOperationException(); }
        }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        /// <exception cref="System.InvalidOperationException">Set is not allowed</exception>
        public int Version
        {
            get { return 1; }
            set { throw new InvalidOperationException(); }
        }

        /// <summary>
        /// Gets or sets the cversion.
        /// </summary>
        /// <value>The cversion.</value>
        /// <exception cref="System.InvalidOperationException">Set is not allowed</exception>
        public int Cversion
        {
            get { return this.Version; }
            set { throw new InvalidOperationException(); }
        }

        /// <summary>
        /// Gets or sets the aversion.
        /// </summary>
        /// <value>The aversion.</value>
        /// <exception cref="System.InvalidOperationException">Set is not allowed</exception>
        public int Aversion
        {
            get { return this.Version; }
            set { throw new InvalidOperationException(); }
        }

        /// <summary>
        /// Gets or sets the length of the data.
        /// </summary>
        /// <value>The length of the data.</value>
        public int DataLength { get; set; }

        /// <summary>
        /// Gets or sets the number children.
        /// </summary>
        /// <value>The number children.</value>
        public int NumChildren
        {
            get { return 0; }
            set { throw new InvalidOperationException($"{nameof(FirstStat)} must not have child"); }
        }

        /// <inheritdoc />
        public int NumEphemeralChildren
        {
            get { return 0; }
            set { throw new InvalidOperationException($"{nameof(FirstStat)} must not have child"); }
        }

        /// <summary>
        /// Gets the unique incarnation id for this object
        /// </summary>
        public Guid UniqueIncarnationId => Stat.GetUniqueIncarnationId(this, false);

        /// <summary>
        /// Gets the unique incarnation id for this object, also considering changes on its children.
        /// </summary>
        public Guid UniqueExtendedIncarnationId => Stat.GetUniqueIncarnationId(this, true);

        /// <summary>
        /// Turns the value into a first stat if it makes sense.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>the original value, or a firststat that is equivalent to it</returns>
        public static IMutableStat TurnIntoFirstStatIfNeeded(IMutableStat value)
        {
            if (value != null &&
                value.Version == 1 &&
                value.Aversion == 1 &&
                value.Cversion == 1 &&
                value.NumChildren == 0 &&
                value.Ctime == value.Mtime &&
                value.Czxid == value.Mzxid &&
                value.Pzxid == value.Czxid)
            {
                return new FirstStat(value);
            }

            return value;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            IMutableStat other = obj as IMutableStat;
            if (other == null)
            {
                return false;
            }

            return this.Czxid == other.Czxid &&
                this.Mzxid == other.Mzxid &&
                this.Ctime == other.Ctime &&
                this.Mtime == other.Mtime &&
                this.Version == other.Version &&
                this.Cversion == other.Cversion &&
                this.Aversion == other.Aversion &&
                this.DataLength == other.DataLength &&
                this.NumChildren == other.NumChildren &&
                this.NumEphemeralChildren == other.NumEphemeralChildren &&
                this.Pzxid == other.Pzxid;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            int hash = this.Czxid.GetHashCode();
            hash ^= this.Mzxid.GetHashCode();
            hash ^= this.Ctime.GetHashCode();
            hash ^= this.Mtime.GetHashCode();
            hash ^= this.Version.GetHashCode();
            hash ^= this.Cversion.GetHashCode();
            hash ^= this.Aversion.GetHashCode();
            hash ^= this.DataLength.GetHashCode();
            hash ^= this.Pzxid.GetHashCode();
            return hash;
        }
    }
}
