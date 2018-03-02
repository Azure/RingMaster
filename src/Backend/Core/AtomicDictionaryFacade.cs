// ***********************************************************************
// <copyright file="AtomicDictionaryFacade.cs" company="Microsoft">
//     Copyright 2015
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    /// <summary>
    /// AtomicDictionaryFacade wraps the given underlying dictionary with a spin lock to synchronize
    /// reads and updates.
    /// </summary>
    /// <typeparam name="TKey">Type of the key</typeparam>
    /// <typeparam name="TValue">Type of value stored in the dictionary</typeparam>
    internal class AtomicDictionaryFacade<TKey, TValue> : IDictionary<TKey, TValue>
    {
        /// <summary>
        /// Mask to check if a writer is active.
        /// </summary>
        private const long WriterIsActiveMask = (long)0x80000000;

        /// <summary>
        /// Mask for reader count.
        /// </summary>
        private const long ReaderCountMask = (long)0x7FFFFFFF;

        /// <summary>
        /// The dictionary that is wrapped by this facade.
        /// </summary>
        private readonly IDictionary<TKey, TValue> underlyingDictionary;

        /// <summary>
        /// Location where reader writer lock control information is stored.
        /// </summary>
        private long readerWriterLockControl;

        /// <summary>
        /// Initializes a new instance of the <see cref="AtomicDictionaryFacade"/> class.
        /// </summary>
        /// <param name="dictionary">Dictionary that must be protected with a lock</param>
        public AtomicDictionaryFacade(IDictionary<TKey, TValue> dictionary)
        {
            this.underlyingDictionary = dictionary;
        }

        /// <summary>
        /// Gets the underlying dictionary.
        /// </summary>
        public IDictionary<TKey, TValue> UnderlyingDictionary
        {
            get { return this.underlyingDictionary; }
        }

        /// <summary>
        /// Gets or sets the value associated with the given key in the underlying dictionary.
        /// </summary>
        /// <param name="key">Key to lookup</param>
        /// <returns>Value associated with the key</returns>
        public TValue this[TKey key]
        {
            get
            {
                return this.RunWithReadLock<TValue>(() =>
                {
                    TValue value;
                    if (this.underlyingDictionary.TryGetValue(key, out value))
                    {
                        return value;
                    }

                    throw new KeyNotFoundException();
                });
            }

            set
            {
                this.RunWithUpdateLock(() => this.underlyingDictionary[key] = value);
            }
        }

        /// <summary>
        /// Gets the number of items in the underlying dictionary.
        /// </summary>
        public int Count
        {
            get
            {
                return this.RunWithReadLock<int>(() => this.underlyingDictionary.Count);
            }
        }

        /// <summary>
        /// Gets whether the underlying dictionary is read only.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return this.RunWithReadLock<bool>(() => this.underlyingDictionary.IsReadOnly);
            }
        }

        /// <summary>
        /// Gets the collection of keys in the dictionary.
        /// </summary>
        /// <remarks>
        /// The dictionary must not be updated for the entire duration of enumeration in order
        /// to avoid getting incorrect results. That must be accomplished with a higher level lock
        /// and is out of scope of the spin lock used by this class.
        /// </remarks>
        public ICollection<TKey> Keys
        {
            get
            {
                return this.underlyingDictionary.Keys;
            }
        }


        /// <summary>
        /// Gets the collection of values in the dictionary.
        /// </summary>
        /// <remarks>
        /// The dictionary must not be updated for the entire duration of enumeration in order
        /// to avoid getting incorrect results. That must be accomplished with a higher level lock
        /// and is out of scope of the spin lock used by this class.
        /// </remarks>
        public ICollection<TValue> Values
        {
            get
            {
                return this.underlyingDictionary.Values;
            }
        }

        /// <summary>
        /// Adds an item to the key value collection.
        /// </summary>
        /// <param name="item">Item to add</param>
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            this.RunWithUpdateLock(() => this.underlyingDictionary.Add(item));
        }

        /// <summary>
        /// Adds an element with the provided key and value to the underlying dictionary.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        public void Add(TKey key, TValue value)
        {
            this.RunWithUpdateLock(() =>
            {
                this.underlyingDictionary.Add(key, value);
            });
        }

        /// <summary>
        /// Removes all items from the dictionary.
        /// </summary>
        public void Clear()
        {
            this.RunWithUpdateLock(() => this.underlyingDictionary.Clear());
        }

        /// <summary>
        /// Determines whether the dictionary contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the dictionary</param>
        /// <returns></returns>
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return this.RunWithReadLock(() => this.underlyingDictionary.Contains(item));
        }

        /// <summary>
        /// Determinse whether the dictionary contains an element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the dictionary</param>
        /// <returns><c>true</c> if the dictionary contains an element with the key; otherwise <c>false</c></returns>
        public bool ContainsKey(TKey key)
        {
            return this.RunWithReadLock(() => this.underlyingDictionary.ContainsKey(key));
        }

        /// <summary>
        /// Copies the elements from the key-value collection to an array, starting at a particular array index.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            this.RunWithReadLock(() =>
            {
                this.underlyingDictionary.CopyTo(array, arrayIndex);
                return true;
            });
        }


        /// <summary>
        /// Removes the the given item.
        /// </summary>
        /// <param name="item">The object to remove</param>
        /// <returns><c>true</c> if the item was successfully removed; otherwise <c>false</c></returns>
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return this.RunWithUpdateLock(() => this.underlyingDictionary.Remove(item));
        }

        /// <summary>
        /// Removes the element with the specified key.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns><c>true</c> if the element was successfully removed; otherwise <c>false</c></returns>
        public bool Remove(TKey key)
        {
            return this.RunWithUpdateLock(() => this.underlyingDictionary.Remove(key));
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">Value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter.
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the dictionary contains an element with the specified key; otherwise <c>false</c></returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            Tuple<bool, TValue> result = this.RunWithReadLock(() =>
            {
                TValue tempValue;
                bool found = this.underlyingDictionary.TryGetValue(key, out tempValue);
                return new Tuple<bool, TValue>(found, tempValue);
            });

            if (result.Item1)
            {
                value = result.Item2;
            }
            else
            {
                value = default(TValue);
            }

            return result.Item1;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <remarks>
        /// The dictionary must not be updated for the entire duration of enumeration in order
        /// to avoid getting incorrect results. That must be accomplished with a higher level lock
        /// and is out of scope of the spin lock used by this class.
        /// </remarks>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return this.underlyingDictionary.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <remarks>
        /// The dictionary must not be updated for the entire duration of enumeration in order
        /// to avoid getting incorrect results. That must be accomplished with a higher level lock
        /// and is out of scope of the spin lock used by this class.
        /// </remarks>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.underlyingDictionary.GetEnumerator();
        }

        /// <summary>
        /// Runs the given function which performs a query on the underlying dictionary.
        /// </summary>
        /// <typeparam name="T">Type of the query result</typeparam>
        /// <param name="func">Function that implements the query</param>
        /// <returns>Result of the query</returns>
        private T RunWithReadLock<T>(Func<T> func)
        {
            try
            {
                this.AcquireReadLock();
                return func();
            }
            finally
            {
                this.ReleaseReadLock();
            }
        }

        /// <summary>
        /// Applies the given action which performs an update on the underlying dictionary.
        /// </summary>
        /// <param name="action">Action that performs the update</param>
        private void RunWithUpdateLock(Action action)
        {
            this.RunWithUpdateLock<bool>(() =>
            {
                action();
                return true;
            });
        }

        /// <summary>
        /// Runs the given function which performs an update on the underlying dictionary.
        /// </summary>
        /// <param name="func">Function that performs the update</param>
        /// <returns>Result of the update</returns>
        private T RunWithUpdateLock<T>(Func<T> func)
        {
            try
            {
                this.AcquireWriteLock();
                return func();
            }
            finally
            {
                this.ReleaseWriteLock();
            }
        }

        private void AcquireReadLock()
        {
            for (;;)
            {
                // Spin as long as a writer is active
                SpinWait.SpinUntil(() => (this.readerWriterLockControl & WriterIsActiveMask) == 0);

                long oldReaderCount = (this.readerWriterLockControl & ReaderCountMask);
                long newReaderCount = oldReaderCount + 1;

                // Attempt to replace the reader count. If a new writer came in after the check above, the following 
                // compare exchange operation will fail.  If will also fail if the reader count had been incremented  by
                // another thread and we will spin once again.
                if (Interlocked.CompareExchange(ref this.readerWriterLockControl, newReaderCount, oldReaderCount) == oldReaderCount)
                {
                    return;
                }
            }
        }

        private void ReleaseReadLock()
        {
            if ((Interlocked.Decrement(ref this.readerWriterLockControl) & WriterIsActiveMask) != 0)
            {
                throw new InvalidOperationException("Writer was active when read lock was released");
            }
        }

        private void AcquireWriteLock()
        {
            for (;;)
            {
                // Spin as long as any reader or writer has the lock.
                SpinWait.SpinUntil(() => this.readerWriterLockControl == 0);

                long oldLock = 0;
                long newLock = WriterIsActiveMask;

                // If there is still no reader or writer, update the lock control with the new value.
                if (Interlocked.CompareExchange(ref this.readerWriterLockControl, newLock, oldLock) == oldLock)
                {
                    return;
                }
            }
        }

        private void ReleaseWriteLock()
        {
            if (Interlocked.CompareExchange(ref this.readerWriterLockControl, 0, WriterIsActiveMask) != WriterIsActiveMask)
            {
                throw new InvalidOperationException("Writer was not active when write lock was released");
            }
        }
    }
}