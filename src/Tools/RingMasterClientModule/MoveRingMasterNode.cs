// <copyright file="MoveRingMasterNode.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ClientModule
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Management.Automation;

    /// <summary>
    /// Moves the RingMaster node from the source path to the destination path.
    /// </summary>
    [Cmdlet(VerbsCommon.Move, "RingMasterNode")]
    public sealed class MoveRingMasterNode : Cmdlet
    {
        /// <summary>
        /// Gets or sets the RingMaster session.
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public RingMasterSession Session { get; set; }

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

            this.Session.Client.Move(this.SourcePath, this.Version, this.DestinationPath, moveMode).GetAwaiter().GetResult();
        }
    }
}
