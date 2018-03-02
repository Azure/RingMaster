// <copyright file="RequestCheck.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    using System;

    /// <summary>
    /// Request to check if the Version, CVersion and AVersion on the node are equal to the values
    /// specified in this request.
    /// </summary>
    public class RequestCheck : AbstractRingMasterRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestCheck"/> class.
        /// </summary>
        /// <param name="path">Path of the node</param>
        /// <param name="version">Expected value of the node's data version</param>
        /// <param name="uid">Unique Id of this request</param>
        public RequestCheck(string path, int version, ulong uid = 0)
            : this(path, version, -1, -1, Guid.Empty, UniqueIncarnationIdType.None, uid)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestCheck"/> class.
        /// </summary>
        /// <param name="path">Path of the node</param>
        /// <param name="version">Expected value of the node's data version</param>
        /// <param name="cversion">Expected value of the node's children version</param>
        /// <param name="aversion">Expected value of the node's <see cref="Acl"/> version</param>
        /// <param name="uniqueIncarnation">Expected value of unique incarnation id</param>
        /// <param name="uniqueIncarnationIdKind">Expected kind of unique incarnation id</param>
        /// <param name="uid">Unique Id of the request</param>
        public RequestCheck(string path, int version, int cversion, int aversion, Guid uniqueIncarnation, UniqueIncarnationIdType uniqueIncarnationIdKind, ulong uid = 0)
            : base(RingMasterRequestType.Check, path, uid)
        {
            this.Version = version;
            this.CVersion = cversion;
            this.AVersion = aversion;
            this.UniqueIncarnationId = uniqueIncarnation;
            this.UniqueIncarnationIdKind = uniqueIncarnationIdKind;
        }

        /// <summary>
        /// Types of unique incarnation ids
        /// </summary>
        public enum UniqueIncarnationIdType : byte
        {
            /// <summary>
            /// No unique incarnation id.
            /// </summary>
            None = 0,

            /// <summary>
            /// A simple unique incarnation id that is based on only the data version of the node.
            /// </summary>
            Simple = 1,

            /// <summary>
            /// Unique incarnation id that takes into account the child version in addition to the data version.
            /// </summary>
            Extended = 2
        }

        /// <summary>
        /// Gets or sets the expected unique incarnation Id.
        /// </summary>
        public Guid UniqueIncarnationId { get; set; }

        /// <summary>
        /// Gets or sets the expected data version.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets the expected children version.
        /// </summary>
        public int CVersion { get; set; }

        /// <summary>
        /// Gets or sets the expected <see cref="Acl"/> version.
        /// </summary>
        public int AVersion { get; set; }

        /// <summary>
        /// Gets or sets the type of unique incarnation id.
        /// </summary>
        public UniqueIncarnationIdType UniqueIncarnationIdKind { get; set; }

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
