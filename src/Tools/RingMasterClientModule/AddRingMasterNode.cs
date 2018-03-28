// <copyright file="AddRingMasterNode.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ClientModule
{
    using System;
    using System.Management.Automation;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Adds a RingMaster node at the given path.
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "RingMasterNode")]
    public sealed class AddRingMasterNode : Cmdlet
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
        /// Gets or sets the acls to be associated with the node.
        /// </summary>
        [Parameter]
        public Acl[] Acls { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether an ephemeral node must be created.
        /// </summary>
        [Parameter]
        public SwitchParameter Ephemeral { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether sequential node must be created.
        /// </summary>
        [Parameter]
        public SwitchParameter Sequential { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the create operation must succeed even if the specified path already exists.
        /// </summary>
        [Parameter]
        public SwitchParameter SucceedEvenIfNodeExists { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether intermediate paths must be created if they don't exist.
        /// </summary>
        [Parameter]
        public SwitchParameter AllowPathCreation { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            CreateMode mode = this.Ephemeral ? CreateMode.Ephemeral : CreateMode.Persistent;

            if (this.Sequential.IsPresent)
            {
                mode |= CreateMode.Sequential;
            }

            if (this.SucceedEvenIfNodeExists.IsPresent)
            {
                mode |= CreateMode.SuccessEvenIfNodeExistsFlag;
            }

            if (this.AllowPathCreation.IsPresent)
            {
                mode |= CreateMode.AllowPathCreationFlag;
            }

            this.Session.Client.Create(
                this.Path,
                this.Data,
                this.Acls,
                mode).GetAwaiter().GetResult();
        }
    }
}
