// <copyright file="GetRingMasterNodeAcl.cs" company="Microsoft">
//     Copyright ©  2018
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ClientModule
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Management.Automation;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Gets the Acl associated with the RingMaster node at the given path.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "RingMasterNodeAcl")]
    public sealed class GetRingMasterNodeAcl : Cmdlet
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
        /// Gets or sets the stat.
        /// </summary>
        [Parameter]
        public IStat Stat { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            var acls = this.Session.Client.GetACL(
                this.Path,
                this.Stat).GetAwaiter().GetResult();

            if (acls != null)
            {
                foreach (var acl in acls)
                {
                    this.WriteObject(acl);
                }
            }
        }
    }
}
