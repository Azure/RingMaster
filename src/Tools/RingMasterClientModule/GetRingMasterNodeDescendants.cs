// <copyright file="GetRingMasterNodeDescendants.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ClientModule
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics.CodeAnalysis;
    using System.Management.Automation;

    /// <summary>
    /// Gets the descendants of the RingMaster node at the given path.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "RingMasterNodeDescendants")]
    public sealed class GetRingMasterNodeDescendants : Cmdlet
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
        /// Gets or sets the maximum number of children to retrieve per request.
        /// </summary>
        [Parameter]
        public int MaxChildrenPerRequest { get; set; } = 1000;

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            using (var descendantQueue = new BlockingCollection<string>())
            {
                var enumTask = this.Session.Client.ForEachDescendant(
                    this.Path,
                    this.MaxChildrenPerRequest,
                    path => descendantQueue.Add(path)).ContinueWith(t => descendantQueue.CompleteAdding());

                foreach (string path in descendantQueue.GetConsumingEnumerable())
                {
                    this.WriteObject(path);
                }

                enumTask.GetAwaiter().GetResult();
            }
        }
    }
}
