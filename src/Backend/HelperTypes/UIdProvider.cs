// <copyright file="UIdProvider.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System.Threading;

    /// <summary>
    /// Class UIdProvider. provides a simple way to generate monotonically increasing IDs
    /// </summary>
    public class UIdProvider
    {
        /// <summary>
        /// The _last identifier
        /// </summary>
        private long lastId;

        /// <summary>
        /// Initializes a new instance of the <see cref="UIdProvider"/> class.
        /// </summary>
        /// <param name="lastId">The last identifier.</param>
        public UIdProvider(long lastId)
        {
            this.lastId = lastId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UIdProvider"/> class.
        /// </summary>
        public UIdProvider()
        {
            this.lastId = 0;
        }

        /// <summary>
        /// Gets the last unique identifier.
        /// </summary>
        /// <returns>UInt64.</returns>
        public virtual ulong GetLastUniqueId()
        {
            return (ulong)this.lastId;
        }

        /// <summary>
        /// Sets the last identifier.
        /// </summary>
        /// <param name="last">The last.</param>
        public virtual void SetLastId(ulong last)
        {
            this.lastId = (long)last;
        }

        /// <summary>
        /// Gets the next unique identifier.
        /// </summary>
        /// <returns>UInt64.</returns>
        public virtual ulong NextUniqueId()
        {
            return (ulong)Interlocked.Increment(ref this.lastId);
        }

        /// <summary>
        /// Determines whether [is identifier in past] [the specified other identifier].
        /// </summary>
        /// <param name="otherId">The other identifier.</param>
        /// <returns><c>true</c> if the given id is in the past; otherwise, <c>false</c>.</returns>
        public virtual bool IsIdInPast(long otherId)
        {
            return otherId <= this.lastId;
        }
    }
}
