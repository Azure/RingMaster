// <copyright file="TestHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypesUnitTest
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// This class contains test helpers
    /// </summary>
    public class TestHelpers
    {
        /// <summary>
        /// Asserts the bytes are equal.
        /// </summary>
        /// <param name="first">The first element.</param>
        /// <param name="second">The second element.</param>
        /// <param name="message">The message if they don't match.</param>
        internal static void AssertBytesEqual(byte[] first, byte[] second, string message)
        {
            if (first == second)
            {
                return;
            }

            Assert.IsNotNull(first, "first is null." + message);
            Assert.IsNotNull(second, "second is null." + message);
            Assert.AreEqual(first.Length, second.Length, "different lengths." + message);

            for (int i = 0; i < first.Length; i++)
            {
                if (first[i] != second[i])
                {
                    Assert.AreEqual(first[i], second[i], "different at position " + i + ". " + message);
                }
            }
        }

        /// <summary>
        /// Asserts the action throws.
        /// </summary>
        /// <typeparam name="T">exception type to throw</typeparam>
        /// <param name="action">The action.</param>
        internal static void AssertThrows<T>(Action action)
            where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            catch (Exception e)
            {
                Assert.Fail("An unexpected exception was thrown " + e);
            }

            Assert.Fail("didn't see any exception");
        }
    }
}