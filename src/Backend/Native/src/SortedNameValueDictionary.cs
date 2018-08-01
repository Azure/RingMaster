// <copyright file="SortedNameValueDictionary.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Native
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Sorted dictionary where key is a string
    /// BUG 2469585: Check the performance and memory footprint to ensure no regression compare with C++/CLI version.
    /// </summary>
    /// <typeparam name="TValue">Type of the value</typeparam>
    public class SortedNameValueDictionary<TValue> : SortedDictionary<string, TValue>
    {
        private SortedSet<KeyValuePair<string, TValue>> underlyingSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="SortedNameValueDictionary{TValue}"/> class.
        /// </summary>
        public SortedNameValueDictionary()
            : base(System.StringComparer.Ordinal)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SortedNameValueDictionary{TValue}"/> class.
        /// </summary>
        /// <param name="dict">Dictionary to copy from</param>
        public SortedNameValueDictionary(IDictionary<string, TValue> dict)
            : base(dict, System.StringComparer.Ordinal)
        {
        }

        /// <summary>
        /// Gets a value indicating whether the dictionary is read-only
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets the list of keys greater than the given string
        /// </summary>
        /// <param name="key">Returned keys must be greater than this</param>
        /// <returns>Keys retrieved.</returns>
        public IEnumerable<string> GetKeysGreaterThan(string key)
        {
            if (this.underlyingSet == null)
            {
                var setInfo = this.GetType().BaseType.GetRuntimeFields().FirstOrDefault(x => x.Name == "_set");
                if (setInfo != null)
                {
                    this.underlyingSet = setInfo.GetValue(this) as SortedSet<KeyValuePair<string, TValue>>;
                }
            }

            if (this.underlyingSet != null)
            {
                bool greater = false;
                var subset = string.IsNullOrEmpty(key)
                    ? this.underlyingSet
                    : this.underlyingSet.GetViewBetween(new KeyValuePair<string, TValue>(key, default(TValue)), this.underlyingSet.Max);
                foreach (var kvp in subset)
                {
                    var k = kvp.Key;
                    if (!greater)
                    {
                        if (string.CompareOrdinal(k, key) > 0)
                        {
                            greater = true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    yield return kvp.Key;
                }
            }
            else
            {
                foreach (var k in this.Keys)
                {
                    if (string.CompareOrdinal(k, key) > 0)
                    {
                        yield return k;
                    }
                }
            }
        }
    }
}
