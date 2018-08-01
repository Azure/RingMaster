// <copyright file="DictionaryOfCollection.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System.Collections.Generic;

    /// <summary>
    /// Class DictionaryOfCollection.
    /// </summary>
    /// <typeparam name="TK">The type of the tk.</typeparam>
    /// <typeparam name="TV">The type of the tv.</typeparam>
    /// <typeparam name="TCollection">The type of the t collection.</typeparam>
    public class DictionaryOfCollection<TK, TV, TCollection>
        where TCollection : class, ICollection<TV>, new()
    {
        /// <summary>
        /// The _keys
        /// </summary>
        private readonly Dictionary<TK, object> keys = new Dictionary<TK, object>();

        /// <summary>
        /// Adds the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Add(TK key, TV value)
        {
            object v;
            if (!this.keys.TryGetValue(key, out v))
            {
                this.keys.Add(key, value);
            }
            else
            {
                TCollection list = null;
                TCollection collection = v as TCollection;
                if (collection != null)
                {
                    list = collection;
                }

                if (list == null)
                {
                    list = new TCollection { (TV)v };
                    this.keys[key] = list;
                }

                list.Add(value);
            }
        }

        /// <summary>
        /// clears the structure
        /// </summary>
        public void Clear()
        {
            this.keys.Clear();
        }

        /// <summary>
        /// Determines whether the specified key contains any.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if the instance contains any element with the specified key; otherwise, <c>false</c>.</returns>
        public bool ContainsAny(TK key)
        {
            return this.keys.ContainsKey(key);
        }

        /// <summary>
        /// Determines whether [contains] [the specified key].
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if the instance contains the specified key; otherwise, <c>false</c>.</returns>
        public bool Contains(TK key, TV value)
        {
            object v;

            if (this.keys.TryGetValue(key, out v))
            {
                TCollection collection = v as TCollection;
                if (collection != null)
                {
                    TCollection list = collection;
                    return list.Contains(value);
                }

                object ovalue = value;
                if (Equals(ovalue, v))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if object was present (and hence it was removed here), <c>false</c> otherwise.</returns>
        public bool Remove(TK key)
        {
            return this.keys.Remove(key);
        }

        /// <summary>
        /// Removes the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if object was present (and hence it was removed here), <c>false</c> otherwise.</returns>
        public bool Remove(TK key, TV value)
        {
            object v;
            if (!this.keys.TryGetValue(key, out v))
            {
                return false;
            }
            else
            {
                TCollection collection = v as TCollection;
                if (collection != null)
                {
                    TCollection list = collection;
                    bool found = list.Remove(value);
                    if (list.Count != 1)
                    {
                        return found;
                    }

                    using (IEnumerator<TV> enume = list.GetEnumerator())
                    {
                        enume.MoveNext();
                        this.keys[key] = enume.Current;
                    }

                    return found;
                }

                object ovalue = value;
                if (Equals(ovalue, v))
                {
                    this.keys.Remove(key);
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the values.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>IEnumerable&lt;TV&gt;.</returns>
        public IEnumerable<TV> GetValues(TK key)
        {
            object v;

            if (this.keys.TryGetValue(key, out v))
            {
                TCollection collection = v as TCollection;
                if (collection != null)
                {
                    TCollection list = collection;

                    foreach (TV val in list)
                    {
                        yield return val;
                    }
                }
                else
                {
                    yield return (TV)v;
                }
            }
        }
    }
}