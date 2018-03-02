// <copyright file="RandomGenerator.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Performance
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    public class RandomGenerator
    {
        /// <summary>
        /// Minimum valid code point that can be used in a string.
        /// </summary>
        private const int MinCodePoint = 0x32;

        /// <summary>
        /// Maximum valid code point that can be used in a string.
        /// </summary>
        private const int MaxCodePoint = 0x10FFFF;

        private readonly Random random = new Random((int)DateTime.Now.Ticks);

        public int GetRandomInt(int minValue, int maxValue)
        {
            lock (this.random)
            {
                return this.random.Next(minValue, maxValue);
            }
        }

        /// <summary>
        /// Generate a random buffer with length between the given minimum and maximum node data size.
        /// </summary>
        /// <param name="minDataSize">Minimum size of the generated data buffer</param>
        /// <param name="maxDataSize">Maximum size of the generated data buffer</param>
        /// <returns>A buffer of random length filled with random data</returns>
        public byte[] GetRandomData(int minDataSize, int maxDataSize)
        {
            lock (this.random)
            {
                int bufferLength = this.random.Next(minDataSize, maxDataSize);
                byte[] buffer = new byte[bufferLength];
                this.random.NextBytes(buffer);
                return buffer;
            }
        }

        /// <summary>
        /// Generate a string of random length composed of random code points.
        /// </summary>
        /// <param name="minNameLength">Minimum length of the generated name</param>
        /// <param name="maxNameLength">Maximum length of the generated name</param>
        /// <param name="maxCodePoint">Maximum codepoint to use in the generated name</param>
        /// <returns>A string of random length composed of random code points</returns>
        public string GetRandomName(int minNameLength, int maxNameLength, int maxCodePoint = MaxCodePoint)
        {
            lock (this.random)
            {
                int length = this.random.Next(minNameLength, maxNameLength);
                var builder = new StringBuilder();
                for (int i = 0; i < length; i++)
                {
                    int codePoint = 0;

                    do
                    {
                        codePoint = this.random.Next(MinCodePoint, maxCodePoint);
                    }
                    while (!IsValidCodePoint(codePoint));

                    builder.Append(char.ConvertFromUtf32(codePoint));
                }

                return builder.ToString();
            }
        }

        /// <summary>
        /// Check if the given code point is a valid 21-bit Unicode code point ranging from U+0 to U+10FFFF,
        /// excluding the surrogate pair range from U+D800 to U+DFFF.
        /// </summary>
        /// <param name="codePoint">Code point to check</param>
        /// <returns><c>true</c> if the given value is a valid code point</returns>
        private static bool IsValidCodePoint(int codePoint)
        {
            if (codePoint < MinCodePoint)
            {
                return false;
            }

            if (codePoint > MaxCodePoint)
            {
                return false;
            }

            // '/' is not allowed in a name.
            if (codePoint == 47)
            {
                return false;
            }

            if ((codePoint >= 0xD800) && (codePoint <= 0xDFFF))
            {
                return false;
            }

            return true;
        }
    }
}
