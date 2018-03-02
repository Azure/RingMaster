// <copyright file="DictionaryTests.cs" company="Microsoft">
//     Copyright ©  2016
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterBackendCoreUnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using Backend.Native;
    using FluentAssertions;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend;
    using VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests for different IDictionary implementations
    /// </summary>
    [TestClass]
    public sealed class DictionaryTests
    {
        private const int LargeDictionaryItemCount = 1000000;
        private const int SmallDictionaryItemCount = 100;

        private const int ConcurrentAddItemCount = 1;
        private const int ConcurrentRemoveItemCount = 1;
        private const int ConcurrentGetItemCount = 4;

        private long addCount;
        private long addFailureCount;
        private long removeCount;
        private long removeFailureCount;
        private long getCount;
        private long getFailureCount;

        [TestInitialize]
        public void TestInitialize()
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new ConsoleTraceListener());
        }

        [TestMethod]
        [TestCategory("Stress")]
        public void TestGetItemsFromUnchangingDictionary()
        {
            this.StressGetItemsFromDictionary(new Dictionary<string, string>(), LargeDictionaryItemCount, 0, 0, ConcurrentGetItemCount);
        }

        [TestMethod]
        [TestCategory("Stress")]
        public void TestGetItemsFromUnchangingSortedDictionary()
        {
            this.StressGetItemsFromDictionary(new SortedDictionary<string, string>(), LargeDictionaryItemCount, 0, 0, ConcurrentGetItemCount);
        }

        [TestMethod]
        [TestCategory("Stress")]
        public void TestGetItemsFromUnchangingSortedNameValueDictionary()
        {
            this.StressGetItemsFromDictionary(new SortedNameValueDictionary<string>(), LargeDictionaryItemCount, 0, 0, ConcurrentGetItemCount);
        }

        [TestMethod]
        [TestCategory("Stress")]
        public void TestGetItemsFromChangingDictionary()
        {
            this.StressGetItemsFromDictionary(new Dictionary<string, string>(), LargeDictionaryItemCount, ConcurrentAddItemCount, ConcurrentRemoveItemCount, ConcurrentGetItemCount);
        }

        [TestMethod]
        [TestCategory("Stress")]
        public void TestGetItemsFromChangingSortedDictionary()
        {
            this.StressGetItemsFromDictionary(new SortedDictionary<string, string>(), LargeDictionaryItemCount, ConcurrentAddItemCount, ConcurrentRemoveItemCount, ConcurrentGetItemCount);
        }

        [TestMethod]
        [TestCategory("Stress")]
        public void TestGetItemsFromChangingSortedNameValueDictionary()
        {
            this.StressGetItemsFromDictionary(new SortedNameValueDictionary<string>(), LargeDictionaryItemCount, ConcurrentAddItemCount, ConcurrentRemoveItemCount, ConcurrentGetItemCount);
        }

        [TestMethod]
        [TestCategory("Stress")]
        public void TestGetItemsFromRapidlyChangingDictionary()
        {
            this.StressGetItemsFromDictionary(new Dictionary<string, string>(), SmallDictionaryItemCount, ConcurrentAddItemCount, ConcurrentRemoveItemCount, ConcurrentGetItemCount);
        }

        [TestMethod]
        [TestCategory("Stress")]
        public void TestGetItemsFromRapidlyChangingSortedDictionary()
        {
            this.StressGetItemsFromDictionary(new SortedDictionary<string, string>(), SmallDictionaryItemCount, ConcurrentAddItemCount, ConcurrentRemoveItemCount, ConcurrentGetItemCount);
        }

        [TestMethod]
        [TestCategory("Stress")]
        public void TestGetItemsFromRapidlyChangingSortedNameValueDictionary()
        {
            this.StressGetItemsFromDictionary(new SortedNameValueDictionary<string>(), SmallDictionaryItemCount, ConcurrentAddItemCount, ConcurrentRemoveItemCount, ConcurrentGetItemCount);
        }

        private void StressGetItemsFromDictionary(IDictionary<string, string> underlyingDictionary, int itemCount, int addItemTaskCount, int removeItemTaskCount, int getItemTaskCount)
        {
            const int MaxIterations = 10;

            AtomicDictionaryFacade<string, string> dictionary = new AtomicDictionaryFacade<string, string>(underlyingDictionary);

            CancellationTokenSource source = new CancellationTokenSource();
            this.ResetStatistics();

            var tasks = new List<Task>();

            for (int x = 0; x < itemCount; x++)
            {
                dictionary.Add(x.ToString(), x.ToString());
            }

            for (int i = 0; i < addItemTaskCount; i++)
            {
                tasks.Add(Task.Run(() => this.AddToDictionary(dictionary, itemCount, source.Token)));
            }

            for (int i = 0; i < removeItemTaskCount; i++)
            {
                tasks.Add(Task.Run(() => this.RemoveFromDictionary(dictionary, itemCount, source.Token)));
            }

            for (int i = 0; i < getItemTaskCount; i++)
            {
                tasks.Add(Task.Run(() => this.GetFromDictionary(dictionary, dictionary.Count, source.Token)));
            }

            int iterationCount = 0;
            while (iterationCount < MaxIterations && (Task.WaitAny(tasks.ToArray(), TimeSpan.FromSeconds(1)) == -1))
            {
                Trace.TraceInformation($"{iterationCount}: Adds={this.addCount}, Removes={this.removeCount}, Gets={this.getCount}");
                Trace.TraceInformation($"{iterationCount}: Failure Adds={this.addFailureCount}, Removes={this.removeFailureCount}, Gets={this.getFailureCount}");

                this.addCount = this.addFailureCount = 0;
                this.removeCount = this.removeFailureCount = 0;
                this.getCount = this.getFailureCount = 0;
                iterationCount++;
            }

            source.Cancel();

            Task.WaitAll(tasks.ToArray());
        }

        private void ResetStatistics()
        {
            this.addCount = this.addFailureCount = 0;
            this.removeCount = this.removeFailureCount = 0;
            this.getCount = this.getFailureCount = 0;
        }

        private void AddToDictionary(IDictionary<string, string> dictionary, int maxElements, CancellationToken cancellationToken)
        {
            Random randomValueGenerator = new Random();
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    string keyName = randomValueGenerator.Next(0, maxElements).ToString();
                    dictionary.Add(keyName, keyName);

                    Interlocked.Increment(ref this.addCount);
                }
                catch (ArgumentException)
                {
                    Interlocked.Increment(ref this.addCount);
                }
                catch (Exception)
                {
                    Interlocked.Increment(ref this.addFailureCount);
                }
            }
        }

        private void RemoveFromDictionary(IDictionary<string, string> dictionary, int maxElements, CancellationToken cancellationToken)
        {
            Random randomValueGenerator = new Random();
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    string keyName = randomValueGenerator.Next(0, maxElements).ToString();
                    dictionary.Remove(keyName);

                    Interlocked.Increment(ref this.removeCount);
                }
                catch (KeyNotFoundException)
                {
                    Interlocked.Increment(ref this.removeCount);
                }
                catch (Exception)
                {
                    Interlocked.Increment(ref this.removeFailureCount);
                }
            }
        }

        private void GetFromDictionary(IDictionary<string, string> dictionary, int maxElements, CancellationToken cancellationToken)
        {
            Random randomValueGenerator = new Random();
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    string keyName = randomValueGenerator.Next(0, maxElements).ToString();

                    string value = dictionary[keyName];

                    Interlocked.Increment(ref this.getCount);
                }
                catch (KeyNotFoundException)
                {
                    Interlocked.Increment(ref this.getCount);
                }
                catch (Exception)
                {
                    Interlocked.Increment(ref this.getFailureCount);
                }
            }
        }
    }
}
