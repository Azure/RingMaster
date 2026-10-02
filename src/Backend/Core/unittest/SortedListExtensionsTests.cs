// <copyright file="SortedListExtensionsTests.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterBackendCoreUnitTest
{
    using System;using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SortedListExtensionsTests
    {
        [TestMethod]
        public void AsStructEnumerable_Is_The_Same_As_Regular_Enumeration()
        {
            int compareCount = 100;
            var rnd = new Random(42);
            for (int i = 0; i < compareCount; i++)
            {
                var list = GenerateSortedList(count: rnd.Next(minValue: 10, maxValue: 1000), rnd);
                EnsureEnumerationIsTheSame(list);
            }
        }

        private void EnsureEnumerationIsTheSame(SortedList<int, int> list)
        {
            var left = list.ToList();
            var right = new List<KeyValuePair<int, int>>();
            foreach (var item in list.AsStructEnumerable())
            {
                right.Add(item);
            }

            Assert.AreEqual(left.Count, right.Count);
            for (int i = 0; i < left.Count; i++)
            {
                var l = left[i];
                var r = right[i];
                Assert.AreEqual(l, r);
            }
        }

        private static SortedList<int, int> GenerateSortedList(int count, Random random)
        {
            var result = new SortedList<int, int>();
            for (int i = 0; i < count; i++)
            {
                var key = random.Next();
                var value = random.Next();
                if (!result.ContainsKey(key))
                {
                    result.Add(key, value);
                }
            }

            return result;
        }
    }
}
