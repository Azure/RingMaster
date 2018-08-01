// <copyright file="MutableStat.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Data
{
    using System;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Class Stat.
    /// </summary>
    [Serializable]
    public sealed class MutableStat : IMutableStat
    {
        /// <summary>
        /// Windows file time units is 100-nanoseconds, so there is 10k fs ticks in a millisecond:
        /// </summary>
        private const long FsTimeTicksPerMillisecond = 10000;

        /// <summary>
        /// Initializes a new instance of the <see cref="MutableStat"/> class.
        /// </summary>
        public MutableStat()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MutableStat"/> class.
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
        /// <param name="numEphemeralChildren">The number of ephemeral children</param>
        /// <param name="pzxid">The pzxid.</param>
        public MutableStat(long czxid, long mzxid, long ctime, long mtime, int version, int cversion, int aversion, int dataLength, int numChildren, int numEphemeralChildren, long pzxid)
        {
            this.Czxid = czxid;
            this.Mzxid = mzxid;
            this.Ctime = ctime;
            this.Mtime = mtime;
            this.Version = version;
            this.Cversion = cversion;
            this.Aversion = aversion;
            this.DataLength = dataLength;
            this.NumChildren = numChildren;
            this.NumEphemeralChildren = numEphemeralChildren;
            this.Pzxid = pzxid;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MutableStat"/> class.
        /// </summary>
        /// <param name="other">The other.</param>
        public MutableStat(IStat other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            this.Czxid = other.Czxid;
            this.Mzxid = other.Mzxid;
            this.Ctime = other.Ctime;
            this.Mtime = other.Mtime;
            this.Version = other.Version;
            this.Cversion = other.Cversion;
            this.Aversion = other.Aversion;
            this.DataLength = other.DataLength;
            this.NumChildren = other.NumChildren;
            this.Pzxid = other.Pzxid;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MutableStat"/> class.
        /// </summary>
        /// <param name="other">The other.</param>
        public MutableStat(IMutableStat other)
            : this((IStat)other)
        {
            this.NumEphemeralChildren = other.NumEphemeralChildren;
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
        public long Mzxid { get; set; }

        /// <summary>
        /// Gets or sets the pzxid.
        /// </summary>
        /// <value>The pzxid.</value>
        public long Pzxid { get; set; }

        /// <summary>
        /// Gets or sets the ctime.
        /// </summary>
        /// <value>The ctime.</value>
        public long Ctime { get; set; }

        /// <summary>
        /// Gets or sets the mtime.
        /// </summary>
        /// <value>The mtime.</value>
        public long Mtime { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        public int Version { get; set; }

        /// <summary>
        /// Gets the unique incarnation id for this object
        /// </summary>
        public Guid UniqueIncarnationId => GetUniqueIncarnationId(this, false);

        /// <summary>
        /// Gets the unique incarnation id for this object, also considering changes on its children.
        /// </summary>
        public Guid UniqueExtendedIncarnationId => GetUniqueIncarnationId(this, true);

        /// <summary>
        /// Gets or sets the cversion.
        /// </summary>
        /// <value>The cversion.</value>
        public int Cversion { get; set; }

        /// <summary>
        /// Gets or sets the aversion.
        /// </summary>
        /// <value>The aversion.</value>
        public int Aversion { get; set; }

        /// <summary>
        /// Gets or sets the length of the data.
        /// </summary>
        /// <value>The length of the data.</value>
        public int DataLength { get; set; }

        /// <summary>
        /// Gets or sets the number children.
        /// </summary>
        /// <value>The number children.</value>
        public int NumChildren { get; set; }

        /// <inheritdoc />
        /// <remarks>
        /// Note that number is only changed by the backend core when ephemeral nodes are added or removed. Concret
        /// class of PersistedData/Factory (including SF and other ones) should not deal with ephemeral nodes.
        /// </remarks>
        public int NumEphemeralChildren { get; set; }

        /// <summary>
        /// computes the unique incarnation id for an arbitrary IStat
        /// </summary>
        /// <param name="stat">the stat to evaluate</param>
        /// <param name="useExtended">if true, the returned guid is an extended incarnation id (including children version)</param>
        /// <returns>the unique incarnation id for the stat</returns>
        public static Guid GetUniqueIncarnationId(IMutableStat stat, bool useExtended)
        {
            if (stat == null)
            {
                return Guid.Empty;
            }

            int a = stat.Version;
            short b = 0;
            short c = 0;
            byte[] bytes = BitConverter.GetBytes(stat.Ctime);

            if (useExtended)
            {
                b = (short)(((ushort)stat.Cversion) >> 2);
                c = (short)(stat.Cversion % 0xffff);
            }

            return new Guid(a, b, c, bytes);
        }

        /// <summary>
        /// Extracts the version from a unique incarnation ID
        /// </summary>
        /// <param name="uniqueIncarnationId">ID of the incarnation</param>
        /// <returns>Extracted version</returns>
        public static int ExtractVersionFromUniqueIncarnationId(Guid uniqueIncarnationId)
        {
            byte[] bytes = uniqueIncarnationId.ToByteArray();
            return BitConverter.ToInt32(bytes, 0);
        }

        /// <summary>
        /// Converts the time.
        /// </summary>
        /// <param name="t">The t.</param>
        /// <returns>System.Int64.</returns>
        public static long ConvertTime(DateTime t)
        {
            return t.ToFileTimeUtc();
        }

        /// <summary>
        /// Converts the time.
        /// </summary>
        /// <param name="lt">The lt.</param>
        /// <returns>DateTime.</returns>
        public static DateTime ConvertTime(long lt)
        {
            return DateTime.FromFileTimeUtc(lt);
        }

        /// <summary>
        /// Converts the time span into RM time delta.
        /// </summary>
        /// <param name="ts">The timespan to convert.</param>
        /// <returns>the RM internalized time delta represented by ts</returns>
        public static long ConvertToRmTimeDelta(TimeSpan ts)
        {
            return (long)(ts.TotalMilliseconds * FsTimeTicksPerMillisecond);
        }

        /// <summary>
        /// Converts the RM time delta into a timespan.
        /// </summary>
        /// <param name="timedelta">The RM time delta.</param>
        /// <returns>the timespan representing the time delta</returns>
        public static TimeSpan ConvertFromRmTimeDelta(long timedelta)
        {
            return TimeSpan.FromMilliseconds(timedelta / (double)FsTimeTicksPerMillisecond);
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
            hash ^= this.NumChildren.GetHashCode();
            hash ^= this.Pzxid.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="string" /> that represents this instance.</returns>
        public override string ToString()
        {
            return $"[STAT czxid:{this.Czxid} mzxid:{this.Mzxid} pzxid:{this.Pzxid} "
                + $"ctime:{ConvertTime(this.Ctime).ToString("o")} "
                + $"mtime:{ConvertTime(this.Mtime).ToString("o")} version:{this.Version} "
                + $"cversion:{this.Cversion} aversion:{this.Aversion} "
                + $"numChildren:{this.NumChildren} uniqueIncId:{this.UniqueIncarnationId} "
                + $"uniqueExtIncId:{this.UniqueExtendedIncarnationId}]";
        }
    }
}
