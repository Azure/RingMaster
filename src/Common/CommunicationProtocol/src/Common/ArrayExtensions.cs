// <copyright file="ArrayExtensions.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol
{
    using System;

    /// <summary>
    /// Extension methods for <see cref="Array"/>
    /// </summary>
    internal static class ArrayExtensions
    {
        /// <summary>
        /// Reverse a byte array
        /// </summary>
        /// <param name="b">the Array</param>
        /// <returns>the Modified array</returns>
        public static byte[] Reverse(this byte[] b)
        {
            Array.Reverse(b);
            return b;
        }
    }
}
