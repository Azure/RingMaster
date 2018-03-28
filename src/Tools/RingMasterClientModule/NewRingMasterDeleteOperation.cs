// <copyright file="NewRingMasterDeleteOperation.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ClientModule
{
    using System;
    using System.Collections.Generic;
    using System.Management.Automation;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Creates a RingMaster delete operation.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "RingMasterDeleteOperation")]
    public sealed class NewRingMasterDeleteOperation : Cmdlet
    {
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

            this.WriteObject(Op.Delete(this.Path, this.Version, deleteMode));
        }
    }
}
