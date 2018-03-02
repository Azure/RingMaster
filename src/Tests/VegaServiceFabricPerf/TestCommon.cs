// <copyright file="TestCommon.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Performance
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web.Script.Serialization;
    using Azure.Networking.Infrastructure.RingMaster;

    /// <summary>
    /// Common functions in the project.
    /// </summary>
    public static class TestCommon
    {
        /// <summary>
        /// The ring master service name
        /// </summary>
        private const string RingMasterServiceName = "/RINGMASTERSERVICE";

        /// <summary>
        /// Gets the backend Service endpoint
        /// </summary>
        /// <returns>The endpoint</returns>
        public static async Task<string> GetVegaServiceEndpoint()
        {
            var fabricClient = new FabricClient();
            var appList = await fabricClient.QueryManager.GetApplicationListAsync();
            foreach (var app in appList)
            {
                foreach (var svc in await fabricClient.QueryManager.GetServiceListAsync(app.ApplicationName))
                {
                    if (!svc.ServiceName.AbsoluteUri.ToUpperInvariant().Contains(RingMasterServiceName))
                    {
                        continue;
                    }

                    var resolvedPartition = await fabricClient.ServiceManager.ResolveServicePartitionAsync(svc.ServiceName);
                    var endpoint = resolvedPartition.Endpoints
                        .Where(ep => ep.Role == ServiceEndpointRole.StatefulPrimary)
                        .Select(ep => ep.Address)
                        .FirstOrDefault();

                    var serializer = new JavaScriptSerializer();
                    var endpoints = (serializer.DeserializeObject(endpoint) as Dictionary<string, object>)["Endpoints"]
                        as IEnumerable<KeyValuePair<string, object>>;
                    var serviceEndpoint = endpoints.First(kvp => kvp.Key == "ServiceEndpoint").Value as string;

                    return new Uri(serviceEndpoint).Authority;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Make a random data payload
        /// </summary>
        /// <param name="rnd">Random object</param>
        /// <param name="dataLength">Length of payload</param>
        /// <returns>Byte array</returns>
        public static byte[] MakeRandomData(Random rnd, int dataLength)
        {
            if (rnd == null)
            {
                return null;
            }

            var data = new byte[dataLength];
            rnd.NextBytes(data);
            return data;
        }

        /// <summary>
        /// Make a sequential data payload
        /// </summary>
        /// <param name="dataLength">Length of payload</param>
        /// <returns>Byte array</returns>
        public static byte[] MakeSequentialData(int dataLength)
        {
            return Enumerable.Range(0, dataLength).Select(n => (byte)n).ToArray();
        }

        /// <summary>
        /// Run a list of async tasks in parallel (and not to schedule too many async tasks at once)
        /// </summary>
        /// <typeparam name="T">Type name of elements in the enumerable</typeparam>
        /// <param name="source">Source enumerator</param>
        /// <param name="body">async method body for each element</param>
        /// <param name="partitionCount">Number of partition to parallelize the source</param>
        /// <returns>Async task</returns>
        public static Task ForEachAsync<T>(IEnumerable<T> source, Func<T, Task> body, int partitionCount = -1)
        {
            if (partitionCount <= 0)
            {
                partitionCount = Environment.ProcessorCount;
            }

            return Task.WhenAll(
                from partition in Partitioner.Create(source).GetPartitions(partitionCount)
                select Task.Run(async () =>
                {
                    using (partition)
                    {
                        while (partition.MoveNext())
                        {
                            await body(partition.Current);
                        }
                    }
                }));
        }

        /// <summary>
        /// Use the RingMaster watcher via the callback in the test method
        /// </summary>
        internal class CallbackWatcher : IWatcher
        {
            /// <summary>
            /// Gets the unique id of this watcher.
            /// </summary>
            public ulong Id
            {
                get
                {
                    return 0;
                }
            }

            /// <summary>
            /// Gets a value indicating whether the watcher is for a single use only.
            /// </summary>
            /// <value>
            /// <c>true</c> if this is a single-use watcher; otherwise, <c>false</c>.
            /// </value>
            public bool OneUse
            {
                get
                {
                    return false;
                }
            }

            /// <summary>
            /// Gets or sets the delegate for processing the watcher event
            /// </summary>
            /// <value>
            /// The on process.
            /// </value>
            internal Action<WatchedEvent> OnProcess { get; set; } = null;

            /// <summary>
            /// Processes the specified event.
            /// </summary>
            /// <param name="evt">The event</param>
            public void Process(WatchedEvent evt)
            {
                this.OnProcess?.Invoke(evt);
            }
        }
    }
}
