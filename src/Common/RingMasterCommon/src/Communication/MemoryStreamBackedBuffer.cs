// <copyright file="MemoryStreamBackedBuffer.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication
{
    using System;
    using System.IO;
    using Microsoft.IO;

    /// <summary>
    /// The memory stream backend buffer.
    /// </summary>
    /// <seealso cref="Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication.IMemoryBuffer" />
    public class MemoryStreamBackedBuffer : IMemoryBuffer
    {
        private readonly MemoryStream memoryStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryStreamBackedBuffer"/> class.
        /// </summary>
        /// <param name="memoryStream">The memory stream.</param>
        public MemoryStreamBackedBuffer(MemoryStream memoryStream)
        {
            this.memoryStream = memoryStream;
        }

        /// <summary>
        /// Gets the length of the buffer.
        /// </summary>
        public int Length => (int)this.memoryStream.Length;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.memoryStream.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public byte[] GetBuffer()
        {
            // This is a workaround as recyclableMemoryStream has a known issue with .Net core:
            // https://github.com/Microsoft/Microsoft.IO.RecyclableMemoryStream/issues/55
            var recyclableStream = this.memoryStream as RecyclableMemoryStream;
            return recyclableStream == null ? this.memoryStream.GetBuffer() : recyclableStream.GetBuffer();
        }
    }
}
