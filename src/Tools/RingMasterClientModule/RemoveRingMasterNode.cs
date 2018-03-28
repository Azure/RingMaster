// <copyright file="RemoveRingMasterNode.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ClientModule
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Management.Automation;

    /// <summary>
    /// Removes the RingMaster node at the given path.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "RingMasterNode")]
    public sealed class RemoveRingMasterNode : Cmdlet
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
        /// Gets or sets the version.
        /// </summary>
        [Parameter]
        public int Version { get; set; } = -1;

        /// <summary>
        /// Gets or sets a value indicating whether fast delete must be performed.
        /// </summary>
        [Parameter]
        public SwitchParameter FastDelete { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether cascade delete must be performed.
        /// </summary>
        [Parameter]
        public SwitchParameter CascadeDelete { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the operation must succeed even if the node does not exist.
        /// </summary>
        [Parameter]
        public SwitchParameter SuccessEvenIfNodeDoesntExist { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            var deleteMode = DeleteMode.None;

            if (this.FastDelete.IsPresent)
            {
                deleteMode |= DeleteMode.FastDelete;
            }

            if (this.CascadeDelete.IsPresent)
            {
                deleteMode |= DeleteMode.CascadeDelete;
            }

            if (this.SuccessEvenIfNodeDoesntExist.IsPresent)
            {
                deleteMode |= DeleteMode.SuccessEvenIfNodeDoesntExist;
            }

            this.Session.Client.Delete(this.Path, this.Version, deleteMode).GetAwaiter().GetResult();
        }
    }
}
