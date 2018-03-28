// <copyright file="GetRingMasterNodeStat.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ClientModule
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Management.Automation;

    /// <summary>
    /// Gets the stat of the RingMaster node at the given path.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "RingMasterNodeStat")]
    public sealed class GetRingMasterNodeStat : Cmdlet
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
            var stat = this.Session.Client.Exists(
                this.Path,
                watcher: null).GetAwaiter().GetResult();

            this.WriteObject(stat);
        }
    }
}
