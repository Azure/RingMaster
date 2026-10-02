// <copyright file="SortedListExtensions.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

#nullable enable

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Contains extension methods for <see cref="SortedList{TKey,TValue}"/>
    /// </summary>
    internal static class SortedListExtensions
    {
        /// <summary>
        /// Returns a struct enumerator for iterating over <paramref name="sortedList"/> without causing boxing allocation of the iterator.
        /// </summary>
        public static SortedListEnumerator<TKey, TValue> AsStructEnumerable<TKey, TValue>(
            this SortedList<TKey, TValue> sortedList)
            where TKey : notnull
        {
            return new SortedListEnumerator<TKey, TValue>(sortedList);
        }

        /// <summary>
        /// A value type enumerator for allocation-free iterating over <see cref="SortedList{TKey,TValue}"/>
        /// </summary>
        public struct SortedListEnumerator<TKey, TValue>
            where TKey : notnull
        {
            private readonly SortedList<TKey, TValue> sortedList;
            private int index;

            public SortedListEnumerator(SortedList<TKey, TValue> sortedList)
            {
                this.sortedList = sortedList;
                this.index = -1;
            }

            public KeyValuePair<TKey, TValue> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return new(this.sortedList.Keys[this.index], this.sortedList.Values[this.index]);
                }
            }

            public SortedListEnumerator<TKey, TValue> GetEnumerator() => this;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                this.index++;
                return this.index < this.sortedList.Count;
            }
        }
    }
}
