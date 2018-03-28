// <copyright file="OperationOverrides.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Data
{
    /// <summary>
    /// Basic implementation of <see cref="IOperationOverrides"/>.
    /// </summary>
    public class OperationOverrides : IOperationOverrides
    {
        /// <summary>
        /// Gets or sets the Transaction Id to use. <c>ulong.MaxValue</c> means do not override.
        /// </summary>
        public ulong TxId { get; set; }

        /// <summary>
        /// Gets or sets the Transaction Time to use. <c>ulong.MaxValue</c> means do not override.
        /// </summary>
        public ulong TxTime { get; set; }
    }
}