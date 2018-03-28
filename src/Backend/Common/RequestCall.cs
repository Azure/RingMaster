// <copyright file="RequestCall.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System.Diagnostics;

    /// <summary>
    /// Class RequestCall.
    /// </summary>
    public class RequestCall
    {
        private readonly Stopwatch elapsed = Stopwatch.StartNew();

        /// <summary>
        /// Gets the elapsed time in ticks
        /// </summary>
        public long ElapsedInTicks => this.elapsed.ElapsedTicks;

        /// <summary>
        /// Gets or sets the call identifier.
        /// </summary>
        /// <value>The call identifier.</value>
        public ulong CallId { get; set; }

        /// <summary>
        /// Gets or sets the request.
        /// </summary>
        /// <value>The request.</value>
        public IRingMasterBackendRequest Request { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this was sent to the server.
        /// </summary>
        /// <value>Was this request sent to the server?</value>
        public bool Sent { get; set; }

        /// <summary>
        /// Gets or sets the previous object
        /// </summary>
        internal RequestCall Previous { get; set; }

        /// <summary>
        /// Gets or sets the next object
        /// </summary>
        internal RequestCall Next { get; set; }
    }
}