// <copyright file="NewRingMasterMoveOperation.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ClientModule
{
    using System;
    using System.Collections.Generic;
    using System.Management.Automation;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Creates a RingMaster move operation.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "RingMasterMoveOperation")]
    public sealed class NewRingMasterMoveOperation : Cmdlet
    {
        /// <summary>
        /// Gets or sets the source path.
        /// </summary>
        [Parameter(Mandatory = true)]
        public string SourcePath { get; set; }

        /// <summary>
        /// Gets or sets the destination path.
        /// </summary>
        [Parameter(Mandatory = true)]
        public string DestinationPath { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        [Parameter]
        public int Version { get; set; } = -1;

        /// <summary>
        /// Gets or sets a value indicating whether intermediate paths must be created if they don't exist.
        /// </summary>
        [Parameter]
        public SwitchParameter AllowPathCreation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the move must succeed only if the source path is empty.
        /// </summary>
        [Parameter]
        public SwitchParameter OnlyIfSourcePathIsEmpty { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            var moveMode = MoveMode.None;

            if (this.AllowPathCreation.IsPresent)
            {
                moveMode |= MoveMode.AllowPathCreationFlag;
            }

            if (this.OnlyIfSourcePathIsEmpty.IsPresent)
            {
                moveMode |= MoveMode.OnlyIfSourcePathIsEmpty;
            }

            this.WriteObject(Op.Move(this.SourcePath, this.Version, this.DestinationPath, moveMode));
        }
    }
}
