// <copyright file="ByteArrayBackedBuffer.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication
{
    using System;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;

    /// <summary>
    /// The byte array backed buffer.
    /// </summary>
    /// <seealso cref="Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication.IMemoryBuffer" />
    public class ByteArrayBackedBuffer : IMemoryBuffer
    {
        private readonly byte[] bytes;

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteArrayBackedBuffer"/> class.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        public ByteArrayBackedBuffer(byte[] bytes)
        {
            this.bytes = bytes;
        }

        /// <summary>
        /// Gets the length of the buffer.
        /// </summary>
        public int Length => this.bytes.Length;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public byte[] GetBuffer()
        {
            return this.bytes;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            ByteArrayBackedBuffer other = obj as ByteArrayBackedBuffer;
            if (other == null)
            {
                return false;
            }

            if (this == other)
            {
                return true;
            }

            return EqualityHelper.Equals(this.bytes, other.bytes);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            return EqualityHelper.GetByteArrayHashCode(this.bytes);
        }
    }
}
