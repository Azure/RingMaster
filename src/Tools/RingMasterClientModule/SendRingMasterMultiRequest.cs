// <copyright file="SendRingMasterMultiRequest.cs" company="Microsoft">
//     Copyright ©  2018
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ClientModule
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Management.Automation;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Sends a Multi request to RingMaster.
    /// </summary>
    [Cmdlet(VerbsCommunications.Send, "RingMasterMultiRequest")]
    public sealed class SendRingMasterMultiRequest : Cmdlet
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

        /// <summary>
        /// Gets or sets the name that must be used to schedule the multi for deferred execution.
        /// </summary>
        [Parameter]
        public string ScheduleName { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            var results = this.Session.Client.Multi(
                this.Operations,
                this.ScheduleName,
                this.MustCompleteSynchronously.IsPresent).GetAwaiter().GetResult();

            foreach (var result in results)
            {
                this.WriteObject(result);
            }
        }
    }
}
