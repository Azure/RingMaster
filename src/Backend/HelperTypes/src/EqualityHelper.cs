// <copyright file="EqualityHelper.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System;
    using System.Collections;

    /// <summary>
    /// Class EqualityHelper.
    /// </summary>
    public class EqualityHelper
    {
        /// <summary>
        /// Computes equality between the specified objects.
        /// </summary>
        /// <param name="one">The one.</param>
        /// <param name="other">The other.</param>
        /// <returns><c>true</c> if objects are equal, <c>false</c> otherwise.</returns>
        public static new bool Equals(object one, object other)
        {
            if (one == other)
            {
                return true;
            }

            if (one == null)
            {
                return false;
            }

            IList list = one as IList;
            if (list != null && other is IList)
            {
                return Equals(list, (IList)other);
            }

            return one.Equals(other);
        }

        /// <summary>
        /// Computes equality between the specified objects
        /// </summary>
        /// <param name="one">The one.</param>
        /// <param name="other">The other.</param>
        /// <returns><c>true</c> if objects are equal, <c>false</c> otherwise.</returns>
        public static bool Equals(IList one, IList other)
        {
            if (one == null)
            {
                if (other == null)
                {
                    return true;
                }

                return false;
            }

            if (other == null)
            {
                return false;
            }

            if (one.Count != other.Count)
            {
                return false;
            }

            for (int i = 0; i < one.Count; i++)
            {
                if (!Equals(one[i], other[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Computes equality between the specified objects.
        /// </summary>
        /// <param name="p1">The p1.</param>
        /// <param name="p2">The p2.</param>
        /// <returns><c>true</c> if objects are equal, <c>false</c> otherwise.</returns>
        public static bool Equals(byte[] p1, byte[] p2)
        {
            if (p1 == p2)
            {
                return true;
            }

            if (p1 == null || p2 == null)
            {
                return false;
            }

            if (p1.Length != p2.Length)
            {
                return false;
            }

            return ((ReadOnlySpan<byte>)p1).SequenceEqual((ReadOnlySpan<byte>)p2);
        }

        /// <summary>
        /// Returns a measurement of the byte[] using sampling
        /// </summary>
        /// <param name="data">The byte[].</param>
        /// <param name="maxSamples">the maximum number of samples to take. &lt;= 0 means consider every byte.</param>
        /// <returns>A hash code for the byte[], suitable for use in hashing algorithms and data structures like a hash table.</returns>
        internal static int MeasureByteArrayWithSampling(byte[] data, int maxSamples)
        {
            if (data == null)
            {
                return 0;
            }

            uint hash = (uint)data.Length;

            uint n = 0;

            int skip = 1;

            if (maxSamples > 0)
            {
                skip = data.Length / maxSamples;
                if (skip < 1)
                {
                    skip = 1;
                }
            }

            int cnt = 1;

            for (int i = 0; i < data.Length; i += skip)
            {
                if ((cnt % 4) == 0)
                {
                    hash ^= n;
                    n = 0;
                }
                else
                {
                    n = n | (uint)i;
                    n = n << 1;
                }

                cnt++;
            }

            if ((cnt % 4) != 0)
            {
                hash ^= n;
            }

            return (int)hash;
        }
    }
}
