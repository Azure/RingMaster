// <copyright file="ObjectPoolTests.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypesUnitTest
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests for <see cref="ObjectPool{T}"/>.
    /// </summary>
    [TestClass]
    public class ObjectPoolTests
    {
#pragma warning disable SA1600 // Elements should be documented
        [TestMethod]
        public void A_Given_Constructor_Is_Called_On_Empty_Pool()
        {
            var pool = new ObjectPool<PooledClass>(() => new PooledClass(), _ => { });

            using var handle = pool.GetInstance(() => new PooledClass(value: 42));
            Assert.AreEqual(42, handle.Instance.Value);
        }

        [TestMethod]
        public void Cleanup_Is_Called_On_Dispose()
        {
            bool cleanupCalled = false;
            var pool = new ObjectPool<PooledClass>(() => new PooledClass(), _ =>
            {
                cleanupCalled = true;
            });

            using (pool.GetInstance(() => new PooledClass(value: 42)))
            {
            }

            Assert.IsTrue(cleanupCalled);
        }

        [TestMethod]
        public void Instance_Is_ReUsed_From_Pool()
        {
            var pool = new ObjectPool<PooledClass>(() => new PooledClass(), _ => { });

            using (pool.GetInstance(() => new PooledClass(value: 42)))
            {
            }

            // Passing a callback, but the instance from the pool should be used.
            using var handle = pool.GetInstance(factory: () => new PooledClass(1));

            Assert.AreEqual(42, handle.Instance.Value);
        }

        [TestMethod]
        public void GetInstance_Returns_New_Instance_If_Cleanup_Creates_New_Instance()
        {
            var pool = new ObjectPool<PooledClass>(() => new PooledClass(), _ => new PooledClass());

            PooledClass firstInstance;
            using (var handle = pool.GetInstance())
            {
                firstInstance = handle.Instance;
            }

            PooledClass secondInstance;
            using (var handle = pool.GetInstance())
            {
                secondInstance = handle.Instance;
            }

            Assert.AreNotSame(firstInstance, secondInstance);
        }

        [TestMethod]
        public void GetInstance_Returns_Same_Instance()
        {
            var pool = new ObjectPool<PooledClass>(() => new PooledClass(), _ => { });

            PooledClass firstInstance;
            using (var handle = pool.GetInstance())
            {
                firstInstance = handle.Instance;
            }

            PooledClass secondInstance;
            using (var handle = pool.GetInstance())
            {
                secondInstance = handle.Instance;
            }

            Assert.AreSame(firstInstance, secondInstance);
        }

        [TestMethod]
        public void ObjectInPool_Returns_2_When_Two_Instances_Are_Used_At_The_Same_Time()
        {
            var pool = new ObjectPool<PooledClass>(() => new PooledClass(), _ => { });

            using (pool.GetInstance())
            {
                using (pool.GetInstance())
                {
                    Assert.AreEqual(0, pool.ObjectsInPool);
                    Assert.AreEqual(2, pool.FactoryCalls);
                    Assert.AreEqual(2, pool.UseCount);
                }
            }

            Assert.AreEqual(2, pool.ObjectsInPool);
        }

        [TestMethod]
        public void Objects_Are_Dropped_If_Pool_IsFull()
        {
            var pool = new ObjectPool<PooledClass>(() => new PooledClass(), i => i, size: 1);
            var instance1 = new PooledClass();
            pool.PutInstance(instance1);
            using (var handle = pool.GetInstance())
            {
                Assert.AreEqual(0, pool.ObjectsInPool);
                Assert.AreSame(instance1, handle.Instance);
            }

            // The pool is full, so the instance is dropped.
            Assert.AreEqual(1, pool.ObjectsInPool);

            var instance2 = new PooledClass();
            pool.PutInstance(instance2);
            Assert.AreEqual(1, pool.ObjectsInPool);
        }

        [TestMethod]
        public void Objects_Are_Dropped_If_Pool_IsFull_Size_2()
        {
            // Need to separate test cases, since the pool has a hot element stored in a field.
            var pool = new ObjectPool<PooledClass>(() => new PooledClass(), i => i, size: 2);
            var instance1 = new PooledClass(value: 1);
            var instance2 = new PooledClass(value: 2);
            pool.PutInstance(instance1);
            pool.PutInstance(instance2);
            Assert.AreEqual(2, pool.ObjectsInPool);
            using (var handle = pool.GetInstance())
            {
                Assert.IsTrue(handle.Instance == instance1 || handle.Instance == instance2);

                using (var handle2 = pool.GetInstance())
                {
                    Assert.AreEqual(0, pool.ObjectsInPool);
                    Assert.IsTrue(handle2.Instance == instance1 || handle2.Instance == instance2, $"handle2.Instance: {handle2.Instance}, instance1: {instance1}, instance2: {instance2}");
                }
            }

            // The pool is full, so the instance is dropped.
            Assert.AreEqual(2, pool.ObjectsInPool);

            var instance3 = new PooledClass();
            pool.PutInstance(instance3);
            Assert.AreEqual(2, pool.ObjectsInPool);
        }

        [TestMethod]
        public async Task GetInstance_Never_Returns_The_Same_Instance_Under_Stress_For_Hot_Instance()
        {
            // This is a stress test that attempts to grab the first instance multiple times concurrently
            // in order to attempt to show the lack of race conditions.
            // The tests can't fully prove the absence of the races, but the stress test like this at least could help
            // us to catch silly and naive bugs.
            //
            // When the race conditions were intentionally introduced into the implementation the test was consistently failing.
            var pool = new ObjectPool<PooledClass>(() => new PooledClass(), i => i, size: 1);

            pool.PutInstance(new PooledClass());
            const int iterations = 100_000;
            for (int i = 0; i < iterations; i++)
            {
                await Task.WhenAll(Enumerable.Range(0, Environment.ProcessorCount).Select(_ => Task.Run(() =>
                {
                    using var handle = pool.GetInstance();
                    using var handle2 = pool.GetInstance();
                    Assert.AreNotSame(handle.Instance, handle2.Instance);
                })));
            }
        }

        [TestMethod]
        public async Task GetInstance_Never_Returns_The_Same_Instance_Under_Stress_For_Not_Hot_Instance()
        {
            // This is a similar stress test, but instead of getting a hot instance stored in a field,
            // this method obtains the second item from the pool
            var pool = new ObjectPool<PooledClass>(() => new PooledClass(), i => i, size: 2);

            pool.PutInstance(new PooledClass());
            pool.PutInstance(new PooledClass());
            const int iterations = 100_000;
            for (int i = 0; i < iterations; i++)
            {
                await Task.WhenAll(Enumerable.Range(0, Environment.ProcessorCount).Select(_ => Task.Run(() =>
                {
                    using var handle = pool.GetInstance();
                    using var handle2 = pool.GetInstance();
                    using var handle3 = pool.GetInstance();
                    Assert.IsTrue(handle3.Instance != handle.Instance && handle3.Instance != handle2.Instance);
                })));
            }
        }

        private class PooledClass
        {
            public PooledClass()
            {
                Value = 0;
            }

            public PooledClass(int value)
            {
                Value = value;
            }

            public int Value { get; }

            public override string ToString()
            {
                return Value.ToString();
            }
        }
#pragma warning restore SA1600 // Elements should be documented
    }
}