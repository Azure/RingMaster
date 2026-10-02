// <copyright file="IMemoryBuffer.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication
{
    using System;

    /// <summary>
    /// The memory buffer interface.
    /// </summary>
    public interface IMemoryBuffer : IDisposable
    {
        /// <summary>
        /// Gets the length of the buffer.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Gets the backing buffer. Note its size may be bigger than <see cref="Length"/>. You should only use the first <see cref="Length"/> bytes.
        /// IMPORTANT: This buffer MUST NOT be used after <see cref="IDisposable.Dispose"/> has been called and the <see cref="IMemoryBuffer"/> owning
        /// instance MUST be kept alive as long as you have a reference to the underlying buffer (otherwise it may be reclaimed from garbage collection
        /// finalization).
        /// </summary>
        /// <returns>Buffer.</returns>
        byte[] GetBuffer();
    }
}
