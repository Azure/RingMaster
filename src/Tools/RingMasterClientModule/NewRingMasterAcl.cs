// <copyright file="NewRingMasterAcl.cs" company="Microsoft">
//     Copyright ©  2018
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ClientModule
{
    using System;
    using System.Collections.Generic;
    using System.Management.Automation;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Creates a new RingMaster Acl.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "RingMasterAcl")]
    public sealed class NewRingMasterAcl : Cmdlet
    {
        /// <summary>
        /// Gets or sets the permission.
        /// </summary>
        [Parameter(Mandatory = true)]
        [ValidateSet("None", "Create", "Read", "Write", "Delete", "Admin", "All")]
        public string Permission { get; set; }

        /// <summary>
        /// Gets or sets the scheme.
        /// </summary>
        [Parameter(Mandatory = true)]
        [ValidateSet("world", "auth", "digest", "host", "ip")]
        public string Scheme { get; set; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        [Parameter(Mandatory = true)]
        public string Identifier { get; set; }
        
        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            this.WriteObject(new Acl((int)ParsePermission(this.Permission), new Id(this.Scheme, this.Identifier)));
        }

        /// <summary>
        /// Parse the given permission string.
        /// </summary>
        /// <param name="permissionName">Permission name</param>
        /// <returns>Permission code</returns>
        internal static Acl.Perm ParsePermission(string permissionName)
        {
            switch (permissionName)
            {
                case "Create":
                    return Acl.Perm.CREATE;
                case "Read":
                    return Acl.Perm.READ;
                case "Write":
                    return Acl.Perm.WRITE;
                case "Delete":
                    return Acl.Perm.DELETE;
                case "Admin":
                    return Acl.Perm.ADMIN;
                case "All":
                    return Acl.Perm.ALL;
                default:
                    return Acl.Perm.NONE;
            }
        }
    }
}
