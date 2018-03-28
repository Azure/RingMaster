// <copyright file="SendRingMasterBatchRequest.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ClientModule
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Management.Automation;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Sends a Batch request to RingMaster.
    /// </summary>
    [Cmdlet(VerbsCommunications.Send, "RingMasterBatchRequest")]
    public sealed class SendRingMasterBatchRequest : Cmdlet
    {
        /// <summary>
        /// Gets or sets the RingMaster session.
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public RingMasterSession Session { get; set; }

        /// <summary>
        /// Gets or sets the operations that must be applied.
        /// </summary>
        [Parameter(Mandatory = true)]
        public Op[] Operations { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this request must wait for replication to complete before returning.
        /// </summary>
        [Parameter]
        public SwitchParameter MustCompleteSynchronously { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            var results = this.Session.Client.Batch(
                this.Operations,
                this.MustCompleteSynchronously.IsPresent).GetAwaiter().GetResult();

            foreach (var result in results)
            {
                this.WriteObject(result);
            }
        }
    }
}
