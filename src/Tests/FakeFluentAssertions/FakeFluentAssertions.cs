// <copyright file="FakeFluentAssertions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.VisualStudio.TestTools.UnitTesting
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Poor man's assertion helper
    /// </summary>
    public static class FakeFluentAssertions
    {
        /// <summary>
        /// Constructs a testable class
        /// </summary>
        /// <typeparam name="T">Type being tested</typeparam>
        /// <param name="v">Data being tested</param>
        /// <returns>Testable object</returns>
        public static Testable<T> Should<T>(this T v)
        {
            return new Testable<T>(v);
        }

        /// <summary>
        /// Asserts the testable object is true
        /// </summary>
        /// <param name="t">Testable object</param>
        public static void BeTrue(this Testable<bool> t)
        {
            t.Be(true);
        }

        /// <summary>
        /// Asserts the testable object is false
        /// </summary>
        /// <param name="t">Testable object</param>
        public static void BeFalse(this Testable<bool> t)
        {
            t.Be(false);
        }

        /// <summary>
        /// Asserts the testable object is equivalent to the other collection
        /// </summary>
        /// <typeparam name="TCollection">Type of the collection</typeparam>
        /// <typeparam name="T">Type of the item in the collection</typeparam>
        /// <param name="t">Testable object</param>
        /// <param name="others">Collection to compare with</param>
        /// <param name="message">Failure message</param>
        public static void BeEquivalentTo<TCollection, T>(
            this Testable<TCollection> t,
            TCollection others,
            string message = null)
            where TCollection : IEnumerable<T>
        {
            var s = new HashSet<T>(t.Value);
            Assert.IsTrue(s.SetEquals(others), message);
        }

        /// <summary>
        /// Asserts that the testable collection is null or empty.
        /// </summary>
        /// <typeparam name="TCollection">Type of the collection</typeparam>
        /// <param name="t">Testable object</param>
        /// <param name="message">Failure message</param>
        public static void BeNullOrEmpty<TCollection>(
            this Testable<TCollection> t,
            string message = null)
            where TCollection : IEnumerable
        {
            Assert.IsTrue(t.Value == null || !t.Value.Cast<object>().Any(), "Expected collection to be null or empty.");
        }

        /// <summary>
        /// Constructs a throwable object
        /// </summary>
        /// <typeparam name="T">Type of object being tested</typeparam>
        /// <param name="o">Object being tested</param>
        /// <param name="a">Action to take</param>
        /// <returns>A throwable object</returns>
        public static TestThowable<T> Invoking<T>(this T o, Action<T> a)
        {
            return new TestThowable<T>(a, o);
        }

        /// <summary>
        /// Asserts the action should throw a specific exception
        /// </summary>
        /// <typeparam name="TException">Type of exception to throw</typeparam>
        /// <param name="a">Action to test</param>
        /// <param name="message">Failure message</param>
        public static void ShouldThrow<TException>(this Action a, string message = null)
            where TException : Exception
        {
            bool caught = false;
            try
            {
                a();
            }
            catch (TException)
            {
                caught = true;
            }
            catch
            {
                Assert.Fail(message);
            }

            Assert.IsTrue(caught, message);
        }

        /// <summary>
        /// Testable class to check object value and type
        /// </summary>
        /// <typeparam name="TValue">Type of object</typeparam>
        public class Testable<TValue>
        {
            private TValue value;

            /// <summary>
            /// Initializes a new instance of the <see cref="Testable{TValue}"/> class.
            /// </summary>
            /// <param name="v">Value of object</param>
            public Testable(TValue v)
            {
                this.value = v;
            }

            /// <summary>
            /// Gets the value of the object
            /// </summary>
            public TValue Value => this.value;

            /// <summary>
            /// Asserts the object is equal to the given value
            /// </summary>
            /// <param name="w">Value to compare with</param>
            /// <param name="message">Failure message</param>
            public void Be(TValue w, string message = null)
            {
                Assert.AreEqual(w, this.value, message);
            }

            /// <summary>
            /// Asserts the object is of the given type
            /// </summary>
            /// <typeparam name="T">Type to check</typeparam>
            /// <param name="message">Failure message</param>
            public void BeOfType<T>(string message = null)
                where T : class
            {
                Assert.IsNotNull(this.value as T, message);
            }

            /// <summary>
            /// Asserts the object of the given type
            /// </summary>
            /// <param name="type">Type to check</param>
            /// <param name="message">Failure message</param>
            public void BeOfType(Type type, string message = null)
            {
                Assert.AreEqual(type, this.value.GetType(), message);
            }
        }

        /// <summary>
        /// Throwable class to check exception
        /// </summary>
        /// <typeparam name="T">Type of action input object</typeparam>
        public class TestThowable<T>
        {
            private readonly Action<T> action;
            private readonly T input;

            /// <summary>
            /// Initializes a new instance of the <see cref="TestThowable{T}"/> class
            /// </summary>
            /// <param name="a">Action to check</param>
            /// <param name="i">Input of the action</param>
            public TestThowable(Action<T> a, T i)
            {
                this.action = a;
                this.input = i;
            }

            /// <summary>
            /// Asserts the exception will throw
            /// </summary>
            /// <typeparam name="TException">Exception type</typeparam>
            /// <param name="message">Failure message</param>
            public void ShouldThrow<TException>(string message = null)
                where TException : Exception
            {
                bool caught = false;
                try
                {
                    this.action(this.input);
                }
                catch (TException)
                {
                    caught = true;
                }
                catch
                {
                    Assert.Fail(message);
                }

                Assert.IsTrue(caught, message);
            }
        }
    }
}
