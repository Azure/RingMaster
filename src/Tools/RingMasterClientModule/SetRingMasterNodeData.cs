// <copyright file="SetRingMasterNodeData.cs" company="Microsoft">
//     Copyright ©  2018
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ClientModule
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Management.Automation;

    /// <summary>
    /// Sets the data associated with the RingMaster node at the given path.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "RingMasterNodeData")]
    public sealed class SetRingMasterNodeData : Cmdlet
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
        /// Gets or sets the data to be associated with the node.
        /// </summary>
        [Parameter]
        public byte[] Data { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        [Parameter]
        public int Version { get; set; } = -1;

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            var stat = this.Session.Client.SetData(
                this.Path,
                this.Data,
                this.Version).GetAwaiter().GetResult();

            this.WriteObject(stat);
        }
    }
}
