// <copyright file="IMemoryStreamFactory.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System.IO;

    /// <summary>
    /// Memory stream factory.
    /// </summary>
    public interface IMemoryStreamFactory
    {
        /// <summary>
        /// Creates a memory stream.
        /// </summary>
        /// <param name="tag">Tag associated with the memory stream for logging purposes.</param>
        /// <returns>Memory stream.</returns>
        MemoryStream CreateStream(string tag);

        /// <summary>
        /// Creates a memory stream.
        /// </summary>
        /// <param name="tag">Tag associated with the memory stream for logging purposes.</param>
        /// <param name="requestedSize">Requested size of the stream.</param>
        /// <returns>Memory stream.</returns>
        MemoryStream CreateStream(string tag, int requestedSize);
    }
}
