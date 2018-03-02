// <copyright file="SortedArrayList.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    internal class SortedArrayList<TK, TV> : IDictionary<TK, TV>
    {
        private struct Entry
        {
            public TK K;
            public TV V;
        }

        private Entry[] array;
        private byte size;

        public SortedArrayList(int capacity)
        {
            this.array = new Entry[capacity];
            this.size = 0;
        }

        public TV this[TK key]
        {
            get
            {
                return this.Find(key);
            }

            set
            {
                this.SetOrReplace(key, value, true);
            }
        }

        private TV Find(TK key)
        {
            for (int i = 0; i < this.size; i++)
            {
                if (this.array[i].K.Equals(key))
                {
                    return this.array[i].V;
                }
            }

            throw new KeyNotFoundException(key.ToString());
        }

        private void SetOrReplace(TK key, TV value, bool allowReplace)
        {
            for (int i = 0; i < this.size; i++)
            {
                if (this.array[i].K.Equals(key))
                {
                    if (!allowReplace)
                    {
                        throw new ArgumentException($"key already exist: {key}");
                    }

                    this.array[i].V = value;
                    return;
                }
            }

            if (this.size == byte.MaxValue)
            {
                throw new InsufficientMemoryException("cannot set more than 256 elements here");
            }

            if (this.size == this.array.Length)
            {
                // Array.Resize<Entry>(ref this.array, Math.Min((int)(1.6 * size), byte.MaxValue));
                Array.Resize(ref this.array, byte.MaxValue);
            }

            this.array[this.size].K = key;
            this.array[this.size++].V = value;
        }

        public int Count => this.size;

        public bool IsReadOnly => false;

        private class KeyCollection : ICollection<TK>
        {
            private readonly SortedArrayList<TK, TV> array;

            public KeyCollection(SortedArrayList<TK, TV> array)
            {
                this.array = array;
            }

            public int Count => this.array.Count;

            public bool IsReadOnly => true;

            public void Add(TK item)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool Contains(TK item)
            {
                return this.array.ContainsKey(item);
            }

            public void CopyTo(TK[] array, int arrayIndex)
            {
                if (array == null)
                {
                    throw new ArgumentNullException(nameof(array));
                }

                for (int i = 0; i < this.array.size; i++)
                {
                    array[arrayIndex++] = this.array.array[i].K;
                }
            }

            public IEnumerator<TK> GetEnumerator()
            {
                for (int i = 0; i < this.array.size; i++)
                {
                    yield return this.array.array[i].K;
                }
            }

            public bool Remove(TK item)
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                for (int i = 0; i < this.array.size; i++)
                {
                    yield return this.array.array[i].K;
                }
            }
        }

        private class ValueCollection : ICollection<TV>
        {
            private readonly SortedArrayList<TK, TV> array;

            public ValueCollection(SortedArrayList<TK, TV> array)
            {
                this.array = array;
            }

            public int Count => this.array.Count;

            public bool IsReadOnly => true;

            public void Add(TV item)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool Contains(TV item)
            {
                for (int i = 0; i < this.array.size; i++)
                {
                    if (this.array.array[i].V.Equals(item))
                    {
                        return true;
                    }
                }

                return false;
            }

            public void CopyTo(TV[] array, int arrayIndex)
            {
                if (array == null)
                {
                    throw new ArgumentNullException(nameof(array));
                }

                for (int i = 0; i < this.array.size; i++)
                {
                    array[arrayIndex++] = this.array.array[i].V;
                }
            }

            public bool Remove(TV item)
            {
                throw new NotImplementedException();
            }

            IEnumerator<TV> IEnumerable<TV>.GetEnumerator()
            {
                for (int i = 0; i < this.array.size; i++)
                {
                    yield return this.array.array[i].V;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                for (int i = 0; i < this.array.size; i++)
                {
                    yield return this.array.array[i].V;
                }
            }
        }

        public ICollection<TK> Keys => new KeyCollection(this);

        public ICollection<TV> Values => new ValueCollection(this);

        public void Add(KeyValuePair<TK, TV> item)
        {
            this.SetOrReplace(item.Key, item.Value, false);
        }

        public void Add(TK key, TV value)
        {
            this.SetOrReplace(key, value, false);
        }

        public void Clear()
        {
            this.size = 0;
        }

        public bool Contains(KeyValuePair<TK, TV> item)
        {
            for (int i = 0; i < this.size; i++)
            {
                if (this.array[i].K.Equals(item.Key) && this.array[i].V.Equals(item.Value))
                {
                    return true;
                }
            }

            return false;
        }

        public bool ContainsKey(TK key)
        {
            for (int i = 0; i < this.size; i++)
            {
                if (this.array[i].K.Equals(key))
                {
                    return true;
                }
            }

            return false;
        }

        public void CopyTo(KeyValuePair<TK, TV>[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            for (int i = 0; i < this.size; i++)
            {
                array[arrayIndex++] = new KeyValuePair<TK, TV>(this.array[i].K, this.array[i].V);
            }
        }

        public IEnumerator<KeyValuePair<TK, TV>> GetEnumerator()
        {
            for (int i = 0; i < this.size; i++)
            {
                yield return new KeyValuePair<TK, TV>(this.array[i].K, this.array[i].V);
            }
        }

        public bool Remove(KeyValuePair<TK, TV> item)
        {
            for (int i = 0; i < this.size; i++)
            {
                if (this.array[i].K.Equals(item.Key) && this.array[i].V.Equals(item.Value))
                {
                    this.size--;

                    for (int j = i; j < this.size; j++)
                    {
                        this.array[j] = this.array[j + 1];
                    }

                    if (this.size < 128 && this.array.Length >= 2 * this.size)
                    {
                        Array.Resize(ref this.array, this.size);
                    }

                    return true;
                }
            }

            return false;
        }

        public bool Remove(TK key)
        {
            for (int i = 0; i < this.size; i++)
            {
                if (this.array[i].K.Equals(key))
                {
                    this.size--;

                    for (int j = i; j < this.size; j++)
                    {
                        this.array[j] = this.array[j + 1];
                    }

                    if (this.size < 128 && this.array.Length >= 2 * this.size)
                    {
                        Array.Resize(ref this.array, this.size);
                    }

                    return true;
                }
            }

            return false;
        }

        public bool TryGetValue(TK key, out TV value)
        {
            for (int i = 0; i < this.size; i++)
            {
                if (this.array[i].K.Equals(key))
                {
                    value = this.array[i].V;
                    return true;
                }
            }

            value = default(TV);
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}