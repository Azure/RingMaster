// <copyright file="SetRingMasterNodeAcl.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ClientModule
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Management.Automation;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Sets the Acl associated with the RingMaster node at the given path.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "RingMasterNodeAcl")]
    public sealed class SetRingMasterNodeAcl : Cmdlet
    {
        /// <summary>
        /// Gets or sets the RingMaster session.
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public RingMasterSession Session { get; set; }

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
            var stat = this.Session.Client.SetACL(
                this.Path,
                this.Acls,
                this.Version).GetAwaiter().GetResult();

            this.WriteObject(stat);
        }
    }
}
