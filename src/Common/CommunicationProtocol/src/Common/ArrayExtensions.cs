// <copyright file="ArrayExtensions.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol
{
    using System;

    public static class ArrayExtensions
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
