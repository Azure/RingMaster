// <copyright file="RecyclableMemoryStreamFactory.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System.IO;
    using Microsoft.IO;

    /// <summary>
    /// Recyclable memory stream factory.
    /// </summary>
    public class RecyclableMemoryStreamFactory : IMemoryStreamFactory
    {
        /// <inheritdoc />
        public MemoryStream CreateStream(string tag)
        {
            return new RecyclableMemoryStream(RecyclableMemoryBufferPool.Instance, tag);
        }

        /// <inheritdoc />
        public MemoryStream CreateStream(string tag, int requestedSize)
        {
            return new RecyclableMemoryStream(RecyclableMemoryBufferPool.Instance, tag, requestedSize);
        }
    }
}
