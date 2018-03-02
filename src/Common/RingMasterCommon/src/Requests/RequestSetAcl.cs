// <copyright file="RequestSetAcl.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    using System.Collections.Generic;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Request to set a list of <see cref="Acl"/>s on a node.
    /// </summary>
    public class RequestSetAcl : AbstractRingMasterRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestSetAcl"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="acl">List of <see cref="Acls"/> that must be set on the node</param>
        /// <param name="version">The expected value of the <c>Aversion</c> on the node</param>
        /// <param name="uid">Unique Id of the request</param>
        public RequestSetAcl(string path, IReadOnlyList<Acl> acl, int version, ulong uid = 0)
            : base(RingMasterRequestType.SetAcl, path, uid)
        {
            this.Acl = acl;
            this.Version = version;
        }

        /// <summary>
        /// Gets the list of <see cref="Acl"/>s that will be set on the node.
        /// </summary>
        public IReadOnlyList<Acl> Acl { get; private set; }

        /// <summary>
        /// Gets the expected value of the node's <c>Aversion</c>
        /// </summary>
        public int Version { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this this request is read only.
        /// </summary>
        /// <returns><c>false</c> because this request modifies the <see cref="Acl"/>s on a node</returns>
        public override bool IsReadOnly()
        {
            return false;
        }
    }
}
