// <copyright file="TestSortedNameValueDictionary.cs" company="Microsoft">
//     Copyright ©  2016
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterBackendNativeUnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;
    using Backend.Native;
    using VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests for the <see cref="SortedNameValueDictionary"/> class.
    /// </summary>
    [TestClass]
    public sealed class TestSortedNameValueDictionary
    {
        [TestMethod]
        public void TestEmptyDictionary()
        {
            var dictionary = new SortedNameValueDictionary<object>();

            dictionary.Count.Should().Be(0);
            dictionary.ToList().Count.Should().Be(0);
            dictionary.IsReadOnly.Should().Be(false);
            dictionary.Keys.Count.Should().Be(0);
            dictionary.Values.Count.Should().Be(0);
        }

        [TestMethod]
        public void TestAdd()
        {
            var dictionary = new SortedNameValueDictionary<object>();

            object objZ = new object();
            dictionary.Add("Z", objZ);
            dictionary.Count.Should().Be(1);
            dictionary.ToList().Count.Should().Be(1);
            dictionary.Keys.Count.Should().Be(1);
            dictionary.Values.Count.Should().Be(1);
            dictionary["Z"].Should().Be(objZ);

            object objA = new object();
            dictionary.Add("A", objA);
            dictionary.Count.Should().Be(2);
            dictionary.ToList().Count.Should().Be(2);
            dictionary.Keys.Count.Should().Be(2);
            dictionary.Values.Count.Should().Be(2);
            dictionary["A"].Should().Be(objA);

            var keyList = dictionary.Keys.ToList();
            keyList[0].Should().Be("A");
            keyList[1].Should().Be("Z");

            var valueList = dictionary.Values.ToList();
            valueList[0].Should().Be(objA);
            valueList[1].Should().Be(objZ);

            dictionary.ContainsKey("A").Should().BeTrue();
            dictionary.ContainsKey("Z").Should().BeTrue();
            dictionary.ContainsKey("B").Should().BeFalse();

            dictionary.Contains(new KeyValuePair<string, object>("A", objA)).Should().BeTrue();
            dictionary.Contains(new KeyValuePair<string, object>("Z", objZ)).Should().BeTrue();
            dictionary.Contains(new KeyValuePair<string, object>("B", objA)).Should().BeFalse();
            dictionary.Contains(new KeyValuePair<string, object>("A", objZ)).Should().BeFalse();

            object value = null;
            dictionary.TryGetValue("A", out value).Should().BeTrue();
            value.Should().Be(objA);

            dictionary.TryGetValue("B", out value).Should().BeFalse();

            // Value should be default(TValue) per reference source.
            value.Should().Be(default(object));
        }

        [TestMethod]
        public void TestInitializeFromAnotherDictionary()
        {
            object objA = new object();
            object objB = new object();
            object objC = new object();
            var unsortedDictionary = new Dictionary<string, object>();
            unsortedDictionary["C"] = objC;
            unsortedDictionary["B"] = objB;
            unsortedDictionary["A"] = objA;

            var dictionary = new SortedNameValueDictionary<object>(unsortedDictionary);

            dictionary.Count.Should().Be(3);
            dictionary.ToList().Count.Should().Be(3);
            dictionary.Keys.Count.Should().Be(3);
            dictionary.Values.Count.Should().Be(3);
            dictionary["A"].Should().Be(objA);
            dictionary["B"].Should().Be(objB);
            dictionary["C"].Should().Be(objC);

            var keyList = dictionary.Keys.ToList();
            keyList[0].Should().Be("A");
            keyList[1].Should().Be("B");
            keyList[2].Should().Be("C");

            var valueList = dictionary.Values.ToList();
            valueList[0].Should().Be(objA);
            valueList[1].Should().Be(objB);
            valueList[2].Should().Be(objC);
        }

        [TestMethod]
        public void TestCopyTo()
        {
            object objA = new object();
            object objB = new object();
            object objC = new object();
            var unsortedDictionary = new Dictionary<string, object>();
            unsortedDictionary["C"] = objC;
            unsortedDictionary["B"] = objB;
            unsortedDictionary["A"] = objA;

            var dictionary = new SortedNameValueDictionary<object>(unsortedDictionary);

            dictionary.Count.Should().Be(3);

            dictionary.Invoking(d => d.CopyTo(null, 0)).ShouldThrow<ArgumentNullException>();

            var array = new KeyValuePair<string, object>[3];
            dictionary.Invoking(d => d.CopyTo(array, -1)).ShouldThrow<ArgumentOutOfRangeException>();
            dictionary.Invoking(d => d.CopyTo(array, array.Length)).ShouldThrow<ArgumentException>();

            var smallArray = new KeyValuePair<string, object>[1];
            dictionary.Invoking(d => d.CopyTo(smallArray, 0)).ShouldThrow<ArgumentException>();

            dictionary.CopyTo(array, 0);
            array[0].Key.Should().Be("A");
            array[0].Value.Should().Be(objA);
            array[1].Key.Should().Be("B");
            array[1].Value.Should().Be(objB);
            array[2].Key.Should().Be("C");
            array[2].Value.Should().Be(objC);
        }

        [TestMethod]
        public void TestRemove()
        {
            object objA = new object();
            object objB = new object();
            object objC = new object();

            var dictionary = new SortedNameValueDictionary<object>();

            dictionary.Add("A", objA);
            dictionary.Add("B", objB);
            dictionary.Add("C", objC);

            dictionary.Count.Should().Be(3);

            dictionary.Remove("X").Should().BeFalse();
            dictionary.Remove("B").Should().BeTrue();
            dictionary.Count.Should().Be(2);
            dictionary.Keys.Count.Should().Be(2);
            dictionary.Values.Count.Should().Be(2);
            dictionary.ContainsKey("B").Should().BeFalse();

            dictionary.Remove("A").Should().BeTrue();
            dictionary.Remove("A").Should().BeFalse();
            dictionary.Count.Should().Be(1);
            dictionary.Keys.Count.Should().Be(1);
            dictionary.Values.Count.Should().Be(1);
            dictionary.ContainsKey("A").Should().BeFalse();

            dictionary.Clear();
            dictionary.Count.Should().Be(0);
            dictionary.Keys.Count.Should().Be(0);
            dictionary.Values.Count.Should().Be(0);
            dictionary.ContainsKey("C").Should().BeFalse();
        }

        [TestMethod]
        public void TestGetKeysGreaterThan()
        {
            object objA = new object();
            object objB = new object();
            object objC = new object();

            var dictionary = new SortedNameValueDictionary<object>();

            dictionary.Add("A", objA);
            dictionary.Add("B", objB);
            dictionary.Add("C", objC);

            dictionary.Count.Should().Be(3);

            dictionary.GetKeysGreaterThan(null).Count().Should().Be(3);
            dictionary.GetKeysGreaterThan(string.Empty).Count().Should().Be(3);

            dictionary.GetKeysGreaterThan("A").Count().Should().Be(2);
            dictionary.GetKeysGreaterThan("B").Count().Should().Be(1);
            dictionary.GetKeysGreaterThan("C").Count().Should().Be(0);
        }
    }
}
