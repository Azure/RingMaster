// <copyright file="ISetDataOperation.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Data
{
    /// <summary>
    /// A SetData operation that represents a data command.
    /// </summary>
    public interface ISetDataOperation
    {
        /// <summary>
        /// Gets the data associated with the command.
        /// </summary>
        byte[] RawData { get; }
    }
}