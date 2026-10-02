// <copyright file="LinkedListDictionary.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Linked list type
    /// </summary>
    /// <typeparam name="TK">The type of the key.</typeparam>
    /// <typeparam name="TV">The type of the value.</typeparam>
    /// <seealso cref="System.Collections.Generic.IDictionary{TK, TV}" />
    internal class LinkedListDictionary<TK, TV> : IDictionary<TK, TV>
    {
        private LinkedListNode<KeyValuePair<TK, TV>> head;
        private byte size;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinkedListDictionary{TK, TV}"/> class.
        /// </summary>
        public LinkedListDictionary()
        {
            this.size = 0;
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"></see>.
        /// Note that because of possible race conditions (update linked list and then update size), the count may not
        /// be accurate at all time. This should not be an issue for now since the count is not used in critical code path.
        /// </summary>
        public int Count => this.size;

        /// <inheritdoc/>
        public bool IsReadOnly => false;

        /// <inheritdoc/>
        public ICollection<TK> Keys => new KeyCollection(this);

        /// <inheritdoc/>
        public ICollection<TV> Values => new ValueCollection(this);

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void Add(TK key, TV value)
        {
            this.SetOrReplace(key, value, false);
        }

        /// <inheritdoc/>
        public void Add(KeyValuePair<TK, TV> item)
        {
            this.SetOrReplace(item.Key, item.Value, false);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            this.head = null;
            this.size = 0;
        }

        /// <inheritdoc/>
        public bool Contains(KeyValuePair<TK, TV> item)
        {
            var curr = this.head;
            while (curr != null)
            {
                if (curr.Item.Key.Equals(item.Key) && curr.Item.Value.Equals(item.Value))
                {
                    return true;
                }

                curr = curr.Next;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool ContainsKey(TK key)
        {
            var curr = this.head;
            while (curr != null)
            {
                if (curr.Item.Key.Equals(key))
                {
                    return true;
                }

                curr = curr.Next;
            }

            return false;
        }

        /// <inheritdoc/>
        public void CopyTo(KeyValuePair<TK, TV>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<TK, TV>> GetEnumerator()
        {
            var curr = this.head;
            while (curr != null)
            {
                yield return new KeyValuePair<TK, TV>(curr.Item.Key, curr.Item.Value);
                curr = curr.Next;
            }
        }

        /// <inheritdoc/>
        public bool Remove(TK key)
        {
            if (this.head == null)
            {
                return false;
            }

            if (this.head.Item.Key.Equals(key))
            {
                Interlocked.Exchange(ref this.head, this.head.Next);
                this.size--;
                return true;
            }

            var curr = this.head;
            while (curr.Next != null)
            {
                if (curr.Next.Item.Key.Equals(key))
                {
                    curr.SetNextAtomically(curr.Next.Next);
                    this.size--;
                    return true;
                }

                curr = curr.Next;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool Remove(KeyValuePair<TK, TV> item)
        {
            if (this.head == null)
            {
                return false;
            }

            if (this.head.Item.Key.Equals(item.Key) && this.head.Item.Value.Equals(item.Value))
            {
                Interlocked.Exchange(ref this.head, this.head.Next);
                this.size--;
                return true;
            }

            var curr = this.head;
            while (curr.Next != null)
            {
                if (curr.Next.Item.Key.Equals(item.Key) && curr.Next.Item.Value.Equals(item.Value))
                {
                    curr.SetNextAtomically(curr.Next.Next);
                    this.size--;
                    return true;
                }

                curr = curr.Next;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool TryGetValue(TK key, out TV value)
        {
            var curr = this.head;
            while (curr != null)
            {
                if (curr.Item.Key.Equals(key))
                {
                    value = curr.Item.Value;
                    return true;
                }

                curr = curr.Next;
            }

            value = default(TV);
            return false;
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private TV Find(TK key)
        {
            var curr = this.head;
            while (curr != null)
            {
                if (curr.Item.Key.Equals(key))
                {
                    return curr.Item.Value;
                }

                curr = curr.Next;
            }

            throw new KeyNotFoundException(key.ToString());
        }

        private void SetOrReplace(TK key, TV value, bool allowReplace)
        {
            var curr = this.head;
            LinkedListNode<KeyValuePair<TK, TV>> previous = null;
            while (curr != null)
            {
                if (curr.Item.Key.Equals(key))
                {
                    if (!allowReplace)
                    {
                        throw new ArgumentException($"key already exist: {key}");
                    }

                    curr.Item = new KeyValuePair<TK, TV>(key, value);
                    return;
                }

                previous = curr;
                curr = curr.Next;
            }

            if (this.size == byte.MaxValue)
            {
                throw new InsufficientMemoryException("cannot set more than 256 elements here");
            }

            if (this.head == null)
            {
                this.head = new LinkedListNode<KeyValuePair<TK, TV>>(new KeyValuePair<TK, TV>(key, value));
            }
            else
            {
                previous.Next = new LinkedListNode<KeyValuePair<TK, TV>>(new KeyValuePair<TK, TV>(key, value));
            }

            this.size++;
        }

        private class KeyCollection : ICollection<TK>
        {
            private readonly LinkedListDictionary<TK, TV> linkedList;

            public KeyCollection(LinkedListDictionary<TK, TV> linkedList)
            {
                this.linkedList = linkedList;
            }

            public int Count => this.linkedList.Count;

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
                return this.linkedList.ContainsKey(item);
            }

            public void CopyTo(TK[] array, int arrayIndex)
            {
                array.ThrowIfNull();

                var curr = this.linkedList.head;
                while (curr != null)
                {
                    array[arrayIndex++] = curr.Item.Key;
                    curr = curr.Next;
                }
            }

            public IEnumerator<TK> GetEnumerator()
            {
                var curr = this.linkedList.head;
                while (curr != null)
                {
                    yield return curr.Item.Key;
                    curr = curr.Next;
                }
            }

            public bool Remove(TK item)
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                var curr = this.linkedList.head;
                while (curr != null)
                {
                    yield return curr.Item.Key;
                    curr = curr.Next;
                }
            }
        }

        private class ValueCollection : ICollection<TV>
        {
            private readonly LinkedListDictionary<TK, TV> linkedList;

            public ValueCollection(LinkedListDictionary<TK, TV> linkedList)
            {
                this.linkedList = linkedList;
            }

            public int Count => this.linkedList.Count;

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
                var curr = this.linkedList.head;
                while (curr != null)
                {
                    if (curr.Item.Value.Equals(item))
                    {
                        return true;
                    }

                    curr = curr.Next;
                }

                return false;
            }

            public void CopyTo(TV[] array, int arrayIndex)
            {
                array.ThrowIfNull();

                var curr = this.linkedList.head;
                while (curr != null)
                {
                    array[arrayIndex++] = curr.Item.Value;
                    curr = curr.Next;
                }
            }

            public IEnumerator<TV> GetEnumerator()
            {
                var curr = this.linkedList.head;
                while (curr != null)
                {
                    yield return curr.Item.Value;
                    curr = curr.Next;
                }
            }

            public bool Remove(TV item)
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                var curr = this.linkedList.head;
                while (curr != null)
                {
                    yield return curr.Item.Value;
                    curr = curr.Next;
                }
            }
        }

        private class LinkedListNode<T>
        {
            private LinkedListNode<T> next;

            public LinkedListNode(T item)
            {
                this.Item = item;
                this.next = null;
            }

            public T Item { get; set; }

            public LinkedListNode<T> Next
            {
                get
                {
                    return this.next;
                }

                set
                {
                    this.next = value;
                }
            }

            public void SetNextAtomically(LinkedListNode<T> newNext)
            {
                Interlocked.Exchange(ref this.next, newNext);
            }
        }
    }
}
