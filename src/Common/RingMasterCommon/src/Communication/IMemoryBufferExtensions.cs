// <copyright file="IMemoryBufferExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication
{
    using System;

    /// <summary>
    /// Extensions for <see cref="IMemoryBuffer"/>.
    /// </summary>
    public static class IMemoryBufferExtensions
    {
        /// <summary>
        /// Copies a memory buffer to an array.
        /// </summary>
        /// <param name="memoryBuffer">Memory buffer to copy to an array.</param>
        /// <returns>Buffer contents copied to an array.</returns>
        public static byte[] ToArray(this IMemoryBuffer memoryBuffer)
        {
            if (memoryBuffer == null)
            {
                return null;
            }

            var byteArray = new byte[memoryBuffer.Length];
            Array.Copy(memoryBuffer.GetBuffer(), 0, byteArray, 0, memoryBuffer.Length);

            return byteArray;
        }
    }
}
