// <copyright file="SetRingMasterAuth.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ClientModule
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Management.Automation;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Sets the Auth associated with the RingMaster session.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "RingMasterAuth")]
    public sealed class SetRingMasterAuth : Cmdlet
    {
        /// <summary>
        /// Gets or sets the RingMaster session.
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public RingMasterSession Session { get; set; }

        /// <summary>
        /// Gets or sets the auth scheme.
        /// </summary>
        [Parameter(Mandatory = true)]
        [ValidateSet("world", "auth", "digest", "host", "ip")]
        public string Scheme { get; set; }

        /// <summary>
        /// Gets or sets the auth scheme.
        /// </summary>
        [Parameter(Mandatory = true)]
        public string Identifier { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            var id = new Id(this.Scheme, this.Identifier);
            this.Session.Client.SetAuth(id).GetAwaiter().GetResult();
            this.Session.Id = id;

            this.WriteObject(this.Session);
        }
    }
}
