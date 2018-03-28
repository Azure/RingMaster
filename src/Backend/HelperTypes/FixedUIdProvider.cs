// <copyright file="FixedUIdProvider.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System.Threading;

    /// <summary>
    /// Class FixedUIdProvider will provide a fixed number as "nextUIniqueId"
    /// </summary>
    public class FixedUIdProvider : UIdProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FixedUIdProvider"/> class.
        /// </summary>
        /// <param name="fixedresult">Last identifier</param>
        public FixedUIdProvider(long fixedresult)
            : base(fixedresult)
        {
        }

        /// <summary>
        /// Gets the next unique identifier.
        /// </summary>
        /// <returns>UInt64.</returns>
        public override ulong NextUniqueId()
        {
            return this.GetLastUniqueId();
        }
    }
}
