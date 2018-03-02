// <copyright file="NewRingMasterCreateOperation.cs" company="Microsoft">
//     Copyright ©  2018
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ClientModule
{
    using System;
    using System.Collections.Generic;
    using System.Management.Automation;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Creates a RingMaster create operation.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "RingMasterCreateOperation")]
    public sealed class NewRingMasterCreateOperation : Cmdlet
    {
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

            this.WriteObject(Op.Create(this.Path, this.Data, this.Acls, mode));
        }
    }
}
