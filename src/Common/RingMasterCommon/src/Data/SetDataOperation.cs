// <copyright file="SetDataOperation.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Data
{
    /// <summary>
    /// <see cref="SetDataOperation"/> encapsulates information about a set data operation.
    /// </summary>
    internal sealed class SetDataOperation : ISetDataOperation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SetDataOperation"/> class.
        /// </summary>
        /// <param name="rawData">The set data operation bytes.</param>
        public SetDataOperation(byte[] rawData)
        {
            this.RawData = rawData;
        }

        /// <summary>
        /// Gets the raw set data operation bytes.
        /// </summary>
        public byte[] RawData { get; private set; }
    }
}
