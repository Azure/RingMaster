// <copyright file="ObjectPool.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
#nullable enable
    using System;
    using System.Diagnostics;
    using System.Threading;

    /// <summary>
    /// Thread-safe pool of reusable objects.
    /// </summary>
    /// <remarks>
    ///     <para>
    /// Object pools are used to improve performance for objects which would otherwise need to be created and collected
    /// at high rates.
    ///     </para>
    ///     <para>
    /// Pools are particularly valuable for collections which suffer from bad allocation patterns
    /// involving a lot of copying as a collection expands. For example, if code frequently needs to
    /// use temporary lists, and the lists end up having to frequently expand their payload repeatedly during
    /// their lifetime, that causes a lot of garbage and copying. With a pooled lists instead, the list will
    /// expand a few times as it is used, and then will stabilize. The same list storage will be used over and
    /// over again without ever needing to expand it again. Not only does this avoid garbage, it also helps
    /// the cache.
    ///     </para>
    ///
    /// The pool doesn't have 'Clear' method to avoid the extra complexity since a correct multi-threaded implementation
    /// might be complicated. Instead, just create a new pool instance when you need to clear the pool.
    ///
    /// Unlike <see cref="PoolOf{T}"/> this version supports pooling arbitrary instances, like <see cref="System.Text.StringBuilder"/> and other
    /// instance that grow over time.
    /// </remarks>
    /// <typeparam name="T">Type of objects stored in the pool.</typeparam>
    public sealed class ObjectPool<T>
        where T : class
    {
        /// <summary>
        /// creator is stored for the lifetime of the pool. We will call this only when pool needs to
        /// expand. compared to "new T()", Func gives more flexibility to implementers and faster
        /// than "new T()".
        /// </summary>
        private readonly Func<T> creator;

        /// <summary>
        /// Optional cleanup method that is called before putting cached object back to the pool.
        /// </summary>
        /// <remarks>
        /// Using <see cref="Func&lt;T,T&gt;"/> instead of <see cref="Action&lt;T&gt;"/> allows a clients of the
        /// <see cref="ObjectPool&lt;T&gt;"/> to disable pooling by returning new object in the cleanup method.
        /// <example>
        /// ObjectPool&lt;StringBuilder&gt; disabledPool = new ObjectPool&lt;StringBuilder&gt;(
        ///     creator: () => new StringBuilder(),
        ///     cleanup: sb => new StringBuilder());
        ///
        /// ObjectPool&lt;StringBuilder&gt; regularPool = new ObjectPool&lt;StringBuilder&gt;(
        ///     creator: () => new StringBuilder(),
        ///     cleanup: sb => sb.Clear());
        /// </example>
        /// </remarks>
        private readonly Func<T, T>? cleanup;

        /// <summary>
        /// The storage for the pool objects.
        /// </summary>
        private readonly Element[] items;

        /// <summary>
        /// Number of times a creator was invoked.
        /// </summary>
        private long factoryCall;

        /// <summary>
        /// Number of times an instance was obtained from the pool (the counter is incremented when regardless whether the was created or not).
        /// </summary>
        private long useCount;

        /// <summary>
        /// The number of objects in the pool.
        /// </summary>
        private int objectsInPool;

        /// <summary>
        /// Storage for the pool objects. The first item is stored in a dedicated field because we
        /// expect to be able to satisfy most requests from it.
        /// </summary>
        private T? firstItem;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectPool{T}"/> class.
        /// </summary>
        /// <param name="creator">A method to invoke in order to create object instances to insert into the pool.</param>
        /// <param name="cleanup">An optional method to invoke whenever an object is returned into the pool.</param>
        /// <remarks>
        /// The cleanup method is expected to return the object to a 'clean' state such that it can
        /// recycled into the pool and be handed out as a fresh instance. This method typically clears
        /// an object's state to make it look new for subsequent uses.
        /// </remarks>
        public ObjectPool(Func<T> creator, Action<T>? cleanup)
            : this(creator, FromActionToFunc(cleanup), Environment.ProcessorCount * 2)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectPool{T}"/> class.
        /// </summary>
        /// <param name="creator">A method to invoke in order to create object instances to insert into the pool.</param>
        /// <param name="cleanup">An optional method to invoke whenever an object is returned into the pool.</param>
        /// <remarks>
        /// The cleanup method is expected to return the object to a 'clean' state such that it can
        /// recycled into the pool and be handed out as a fresh instance. This method typically clears
        /// an object's state to make it look new for subsequent uses.
        /// </remarks>
        public ObjectPool(Func<T> creator, Func<T, T>? cleanup)
            : this(creator, cleanup, Environment.ProcessorCount * 2)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectPool{T}"/> class.
        /// </summary>
        /// <param name="creator">A method to invoke in order to create object instances to insert into the pool.</param>
        /// <param name="cleanup">An optional method to invoke whenever an object is returned into the pool.</param>
        /// <param name="size">A size of the pool.</param>
        /// <remarks>
        /// The cleanup method is expected to return the object to a 'clean' state such that it can
        /// recycled into the pool and be handed out as a fresh instance. This method typically clears
        /// an object's state to make it look new for subsequent uses.
        /// </remarks>
        public ObjectPool(Func<T> creator, Func<T, T>? cleanup, int size)
        {
            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            this.creator = creator;
            this.cleanup = cleanup;
            this.items = new Element[size - 1];
        }

        /// <summary>
        /// Gets the number of times an object has been obtained from this pool.
        /// </summary>
        public long UseCount => this.useCount;

        /// <summary>
        /// Gets the number of objects that are currently available in the pool.
        /// </summary>
        public int ObjectsInPool => this.objectsInPool;

        /// <summary>
        /// Gets the number of times a factory method was called.
        /// </summary>
        public long FactoryCalls => this.factoryCall;

        /// <summary>
        /// Gets an object instance from the pool.
        /// </summary>
        /// <param name="factory">An optional factory method used to create an instance in case the pool is empty.</param>
        /// <returns>An object instance.</returns>
        /// <remarks>
        /// If the pool is empty, a new object instance is allocated. Otherwise, a previously used instance
        /// is returned.
        /// </remarks>
        public PooledObjectWrapper GetInstance(Func<T>? factory = null)
        {
            return new PooledObjectWrapper(this, this.Rent(factory));
        }

        /// <summary>
        /// Gets an object instance from the pool.
        /// </summary>
        /// <param name="factory">An optional factory method used to create an instance in case the pool is empty.</param>
        /// <returns>An object instance.</returns>
        /// <remarks>
        /// If the pool is empty, a new object instance is allocated. Otherwise, a previously used instance
        /// is returned.
        /// </remarks>
        public T Rent(Func<T>? factory = null)
        {
            // Examine the first element. If that fails, AllocateSlow will look at the remaining elements.
            T? inst = this.firstItem;
            if (inst == null || inst != Interlocked.CompareExchange(ref this.firstItem, null, inst))
            {
                inst = this.AllocateSlow(factory);
            }
            else
            {
                // Got an element from the first element.
                Interlocked.Decrement(ref this.objectsInPool);
            }

            Interlocked.Increment(ref this.useCount);

            return inst;
        }

        /// <summary>
        /// Returns objects to the pool.
        /// </summary>
        /// <param name="wrapper">A wrapper instance that needs to be put back to the pool.</param>
        /// <remarks>
        /// Search strategy is a simple linear probing which is chosen for it cache-friendliness.
        /// Note that PutInstance will try to store recycled objects close to the start thus statistically
        /// reducing how far we will typically search in GetInstance.
        /// </remarks>
        public void PutInstance(PooledObjectWrapper wrapper) => this.PutInstance(wrapper.Instance);

        /// <summary>
        /// Returns objects to the pool.
        /// </summary>
        /// <param name="obj">An instance that should be put back to the pool.</param>
        public void PutInstance(T obj)
        {
            obj = this.cleanup?.Invoke(obj) ?? obj;

            var item = this.firstItem;

            if (item != null || Interlocked.CompareExchange(ref this.firstItem, obj, null) != null)
            {
                this.FreeSlow(obj);
            }
            else
            {
                Interlocked.Increment(ref this.objectsInPool);
            }
        }

        /// <summary>
        /// Helper function that converts clean up method from <see cref="Action&lt;T&gt;"/> to <see cref="Func&lt;T,T&gt;"/>.
        /// </summary>
        /// <param name="cleanup">A cleanup action.</param>
        /// <returns>
        /// Returns a func used to create an instance.
        /// </returns>
        private static Func<T, T>? FromActionToFunc(Action<T>? cleanup)
        {
            if (cleanup == null)
            {
                return null;
            }

            return t =>
            {
                cleanup(t);
                return t;
            };
        }

#pragma warning disable SA1600 // Elements should be documented
        private void FreeSlow(T obj)
        {
            var items = this.items;
            for (var i = 0; i < items.Length; i++)
            {
                if (items[i].Value == null)
                {
                    // Intentionally not using interlocked here.
                    // In a worst case scenario two objects may be stored into same slot.
                    // It is very unlikely to happen and will only mean that one of the objects will get collected.
                    items[i].Value = obj;
                    Interlocked.Increment(ref this.objectsInPool);
                    break;
                }
            }
        }

        private T CreateInstance(Func<T>? factory = null)
        {
            Interlocked.Increment(ref this.factoryCall);
            var inst = factory?.Invoke() ?? this.creator();
            return inst;
        }

        private T AllocateSlow(Func<T>? factory = null)
        {
            var items = this.items;

            for (var i = 0; i < items.Length; i++)
            {
                // Note that the initial read is optimistically not synchronized. That is intentional.
                // We will interlock only when we have a candidate. in a worst case we may miss some
                // recently returned objects. Not a big deal.
                var inst = items[i].Value;
                if (inst != null)
                {
                    if (inst == Interlocked.CompareExchange(ref items[i].Value, null, inst))
                    {
                        Interlocked.Decrement(ref this.objectsInPool);
                        return inst;
                    }
                }
            }

            return this.CreateInstance(factory);
        }
#pragma warning restore SA1600 // Elements should be documented

        /// <summary>
        /// Object wrapper whose purpose it is to provide a Dispose method that returns the wrapped object to its pool.
        /// </summary>
        public readonly struct PooledObjectWrapper : IDisposable
        {
            /// <summary>
            /// A pool.
            /// </summary>
            private readonly ObjectPool<T> pool;

            /// <summary>
            /// Initializes a new instance of the <see cref="PooledObjectWrapper"/> struct.
            /// </summary>
            /// <param name="pool">A pool instance.</param>
            /// <param name="instance">A pooled instance.</param>
            internal PooledObjectWrapper(ObjectPool<T> pool, T instance)
            {
                this.pool = pool;
                this.Instance = instance;
            }

            /// <summary>
            /// Gets the object being wrapped.
            /// </summary>
            public T Instance { get; }

            /// <summary>
            /// Returns the object being wrapped to its pool.
            /// </summary>
            /// <remarks>
            /// Once this method has been called, the wrapped object should no longer be used.
            /// </remarks>
            public void Dispose()
            {
                this.pool.PutInstance(this.Instance);
            }
        }

        /// <summary>
        /// A wrapper struct to make the array access more efficient.
        /// </summary>
        [DebuggerDisplay("{Value,nq}")]
        private struct Element
        {
            /// <summary>
            /// The value.
            /// </summary>
            internal T? Value;
        }
    }
}
