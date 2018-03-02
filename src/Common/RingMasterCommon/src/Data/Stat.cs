// <copyright file="Stat.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Data
{
    using System;
    using System.IO;

    /// <summary>
    /// Statistics data for a node.
    /// </summary>
    [Serializable]
    public sealed class Stat : IStat
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Stat"/> class.
        /// </summary>
        public Stat()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Stat"/> class.
        /// </summary>
        /// <param name="czxid">The id of the transaction that created the node</param>
        /// <param name="mzxid">The id of the most recent transaction that modified the node's data</param>
        /// <param name="ctime">The time at which the node was created</param>
        /// <param name="mtime">The time at which the last modification of this node's data was performed</param>
        /// <param name="version">Version number of the most recent change to the node's data</param>
        /// <param name="cversion">Version number of the most recent change to the node's children</param>
        /// <param name="aversion">Version number of the most recent change to the node's <see cref="Acl"/></param>
        /// <param name="ephemeralOwner">The ephemeral owner.</param>
        /// <param name="dataLength">Length of the data associated with the node</param>
        /// <param name="numChildren">The number children</param>
        /// <param name="pzxid">The id of the most recent transaction that modified the node's children</param>
        public Stat(long czxid, long mzxid, long ctime, long mtime, int version, int cversion, int aversion, long ephemeralOwner, int dataLength, int numChildren, long pzxid)
        {
            this.Czxid = czxid;
            this.Mzxid = mzxid;
            this.Ctime = ctime;
            this.Mtime = mtime;
            this.Version = version;
            this.Cversion = cversion;
            this.Aversion = aversion;
            this.EphemeralOwner = ephemeralOwner;
            this.DataLength = dataLength;
            this.NumChildren = numChildren;
            this.Pzxid = pzxid;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Stat"/> class.
        /// </summary>
        /// <param name="other">The other.</param>
        public Stat(IStat other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            this.Czxid = other.Czxid;
            this.Mzxid = other.Mzxid;
            this.Ctime = other.Ctime;
            this.Mtime = other.Mtime;
            this.Version = other.Version;
            this.Cversion = other.Cversion;
            this.Aversion = other.Aversion;
            this.EphemeralOwner = 0;
            this.DataLength = other.DataLength;
            this.NumChildren = other.NumChildren;
            this.Pzxid = other.Pzxid;
        }

        /// <summary>
        /// Gets or sets the id of the transaction that created the node.
        /// </summary>
        public long Czxid { get; set; }

        /// <summary>
        /// Gets or sets the id of the most recent transaction that modified this node's data.
        /// </summary>
        public long Mzxid { get; set; }

        /// <summary>
        /// Gets or sets the id of the most recent transaction that modified this node's children.
        /// </summary>
        public long Pzxid { get; set; }

        /// <summary>
        /// Gets or sets the time at which this node was created.
        /// </summary>
        public long Ctime { get; set; }

        /// <summary>
        /// Gets or sets the time at which the last modification of this node's data was performed.
        /// </summary>
        public long Mtime { get; set; }

        /// <summary>
        /// Gets or sets the version of this node's data.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Gets the unique incarnation id for this object
        /// </summary>
        public Guid UniqueIncarnationId
        {
            get
            {
                return GetUniqueIncarnationId(this, false);
            }
        }

        /// <summary>
        /// Gets the unique incarnation id for this object, also considering changes on its children.
        /// </summary>
        public Guid UniqueExtendedIncarnationId
        {
            get
            {
                return GetUniqueIncarnationId(this, true);
            }
        }

        /// <summary>
        /// Gets or sets the version number of the most recent change to this node's children.
        /// </summary>
        public int Cversion { get; set; }

        /// <summary>
        /// Gets or sets the version number of the most recent change to this node's <see cref="Acl"/>.
        /// </summary>
        public int Aversion { get; set; }

        /// <summary>
        /// Gets or sets the length of the data associated with this node.
        /// </summary>
        public int DataLength { get; set; }

        /// <summary>
        /// Gets or sets the number of children of this node.
        /// </summary>
        public int NumChildren { get; set; }

        /// <summary>
        /// Gets or sets the ephemeral owner.
        /// </summary>
        public long EphemeralOwner { get; set; }

        /// <summary>
        /// Compute the unique incarnation id for an arbitrary <see cref="IStat"/>.
        /// </summary>
        /// <param name="stat">The stat to evaluate</param>
        /// <param name="useExtended">if true, the returned <see cref="Guid"/> is an extended incarnation id (including children version)</param>
        /// <returns>The unique incarnation id for the stat</returns>
        public static Guid GetUniqueIncarnationId(IStat stat, bool useExtended)
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
        /// Extract the version number that was used to create the given unique incarnation id.
        /// </summary>
        /// <param name="uniqueIncarnationId">Unique Incarnation Id</param>
        /// <returns>The version number used to create the unique incarnation id</returns>
        public static int ExtractVersionFromUniqueIncarnationId(Guid uniqueIncarnationId)
        {
            byte[] bytes = uniqueIncarnationId.ToByteArray();
            return BitConverter.ToInt32(bytes, 0);
        }

        /// <summary>
        /// Converts the time.
        /// </summary>
        /// <param name="time">Time represented as UTC file time</param>
        /// <returns>DateTime object that represents the time</returns>
        public static DateTime ConvertTime(long time)
        {
            return DateTime.FromFileTimeUtc(time);
        }

        /// <summary>
        /// Converts the time.
        /// </summary>
        /// <param name="t">The t.</param>
        /// <returns><see cref="long"/> value that represents the time</returns>
        public static long ConvertTime(DateTime t)
        {
            return t.ToFileTimeUtc();
        }
        
        /// <summary>
        /// Deserializes the object from <see cref="BinaryReader"/> object
        /// </summary>
        /// <param name="reader">binary reader object</param>
        /// <returns>Deserialized object</returns>
        public static Stat ReadStat(BinaryReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            return new Data.Stat(
                reader.ReadInt64(),
                reader.ReadInt64(),
                reader.ReadInt64(),
                reader.ReadInt64(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                0L,
                0,
                0,
                reader.ReadInt64());
        }

        /// <summary>
        /// Serializes to a <see cref="BinaryWriter"/> object
        /// </summary>
        /// <param name="writer">binary writer object</param>
        public void Write(BinaryWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            writer.Write(this.Czxid);
            writer.Write(this.Mzxid);
            writer.Write(this.Ctime);
            writer.Write(this.Mtime);
            writer.Write(this.Version);
            writer.Write(this.Cversion);
            writer.Write(this.Aversion);
            writer.Write(this.Pzxid);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            IStat other = obj as IStat;
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
            hash ^= this.EphemeralOwner.GetHashCode();
            hash ^= this.DataLength.GetHashCode();
            hash ^= this.NumChildren.GetHashCode();
            hash ^= this.Pzxid.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return string.Format("[STAT czxid:{0} mzxid:{1} pzxid:{2} ctime:{3} mtime:{4} version:{5} cversion:{6} aversion:{7} ephemeralOwner:{8} dataLength:{9} numChildren:{10} uniqueIncId:{11} uniqueExtIncId:{12}]", this.Czxid, this.Mzxid, this.Pzxid, ConvertTime(this.Ctime).ToString("o"), ConvertTime(this.Mtime).ToString("o"), this.Version, this.Cversion, this.Aversion, this.EphemeralOwner, this.DataLength, this.NumChildren, this.UniqueIncarnationId, this.UniqueExtendedIncarnationId);
        }
    }
}
