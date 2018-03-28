// <copyright file="IOperationOverrides.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Data
{
    /// <summary>
    /// OperationOverrides can be specified by a request to override the values that will be automatically
    /// assigned by RingMaster.
    /// </summary>
    public interface IOperationOverrides
    {
        /// <summary>
        /// Gets or sets the Transaction Id to use. <c>ulong.MaxValue</c> means do not override.
        /// </summary>
        ulong TxId { get; set; }

        /// <summary>
        /// Gets or sets the Transaction Time to use. <c>ulong.MaxValue</c> means do not override.
        /// </summary>
        ulong TxTime { get; set; }
    }
}
