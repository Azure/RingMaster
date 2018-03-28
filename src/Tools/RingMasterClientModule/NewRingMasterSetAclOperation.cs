// <copyright file="NewRingMasterSetAclOperation.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ClientModule
{
    using System;
    using System.Collections.Generic;
    using System.Management.Automation;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Creates a RingMaster set data operation.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "RingMasterSetAclOperation")]
    public sealed class NewRingMasterSetAclOperation : Cmdlet
    {
        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        [Parameter(Mandatory = true)]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the acls to be associated with the node.
        /// </summary>
        [Parameter]
        public Acl[] Acls { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        [Parameter]
        public int Version { get; set; } = -1;

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            this.WriteObject(Op.SetAcl(this.Path, this.Acls, this.Version));
        }
    }
}
