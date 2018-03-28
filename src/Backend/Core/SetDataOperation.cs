// <copyright file="SetDataOperation.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Set data command
    /// </summary>
    internal sealed class SetDataOperation : ISetDataOperation
    {
        private byte[] bytes;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetDataOperation"/> class.
        /// </summary>
        /// <param name="bytes">Data to set</param>
        public SetDataOperation(byte[] bytes)
        {
            this.bytes = bytes;
        }

        /// <summary>
        /// Gets the raw data
        /// </summary>
        public byte[] RawData
        {
            get
            {
                return this.bytes;
            }
        }
    }
}