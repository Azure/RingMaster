// <copyright file="Id.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Data
{
    using System;

    /// <summary>
    /// Identifies an actor.
    /// </summary>
    public class Id
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Id"/> class.
        /// </summary>
        public Id()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id"/> class.
        /// </summary>
        /// <param name="scheme">The scheme.</param>
        /// <param name="id">The identifier.</param>
        public Id(string scheme, string id)
        {
            this.Scheme = string.Intern(scheme);
            this.Identifier = string.Intern(id);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id"/> class.
        /// </summary>
        /// <param name="other">The <see cref="Id"/> instance to copy.</param>
        public Id(Id other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            this.Scheme = string.Intern(other.Scheme);
            this.Identifier = string.Intern(other.Identifier);
        }

        /// <summary>
        /// Gets the scheme.
        /// </summary>
        public string Scheme { get; private set; }

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        public string Identifier { get; private set; }
    }
}
