// <copyright file="GetRingMasterNodeData.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ClientModule
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Management.Automation;

    /// <summary>
    /// Gets the data associated with the RingMaster node at the given path.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "RingMasterNodeData")]
    public sealed class GetRingMasterNodeData : Cmdlet
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

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            var data = this.Session.Client.GetData(
                this.Path,
                watcher: null).GetAwaiter().GetResult();

            this.WriteObject(data);
        }
    }
}
