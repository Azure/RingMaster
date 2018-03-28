// <copyright file="GetRingMasterNodeChildren.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ClientModule
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Management.Automation;

    /// <summary>
    /// Gets the children of the RingMaster node at the given path.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "RingMasterNodeChildren")]
    public sealed class GetRingMasterNodeChildren : Cmdlet
    {
        /// <summary>
        /// Gets or sets the RingMaster session.
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public RingMasterSession Session { get; set; }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        [Parameter]
        public string Path { get; set; } = "/";

        /// <summary>
        /// Gets or sets the maximum number of children to retrieve.
        /// </summary>
        [Parameter]
        public int MaxChildren { get; set; } = 100;

        /// <summary>
        /// Gets or sets the name of the child after which the enumeration must be started.
        /// </summary>
        [Parameter]
        public string StartingChildName { get; set; } = string.Empty;

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            var children = this.Session.Client.GetChildren(
                this.Path,
                watcher: null,
                retrievalCondition: $">:{this.MaxChildren}:{this.StartingChildName}").GetAwaiter().GetResult();

            foreach (var child in children)
            {
                this.WriteObject(child);
            }
        }
    }
}
