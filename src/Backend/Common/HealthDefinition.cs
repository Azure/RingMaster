// <copyright file="HealthDefinition.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Class HealthDefinition.
    /// </summary>
    public class HealthDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HealthDefinition"/> class.
        /// </summary>
        /// <param name="isPrimary">Is this member a primary?</param>
        /// <param name="description">The description.</param>
        /// <param name="ratio">The ratio.</param>
        public HealthDefinition(bool isPrimary, double ratio, string description)
        {
            this.IsPrimary = isPrimary;
            this.HealthRatio = ratio;
            this.Description = description;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this member thinks it is a primary
        /// </summary>
        /// <value>true if this is a primary.</value>
        public bool IsPrimary { get; set; }

        /// <summary>
        /// Gets or sets the health ratio. 0 means dead, 1 means perfectly functional
        /// </summary>
        /// <value>The health ratio.</value>
        public double HealthRatio { get; set; }

        /// <summary>
        /// Gets or sets the free text description of the health.
        /// </summary>
        /// <value>The description.</value>
        public string Description { get; set; }
    }
}
