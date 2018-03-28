// <copyright file="ServiceDiscovery.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ServiceFabric
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Fabric;
    using System.Fabric.Query;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Script.Serialization;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport;

    /// <summary>
    /// Service discovery using <see cref="FabricClient"/>
    /// </summary>
    public sealed class ServiceDiscovery : IDisposable
    {
        private readonly FabricClient fabricClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceDiscovery"/> class.
        /// </summary>
        /// <param name="fabricClient">Fabric client object</param>
        public ServiceDiscovery(FabricClient fabricClient = null)
        {
            this.fabricClient = fabricClient ?? new FabricClient();
        }

        /// <summary>
        /// Gets the endpoint URI of the specified replica
        /// </summary>
        /// <param name="replicaAddress">replica address</param>
        /// <param name="endpointName">Name of the endpoint</param>
        /// <returns>Replica URI</returns>
        public static Uri GetReplicaEndpointUri(string replicaAddress, string endpointName)
        {
            if (replicaAddress == null)
            {
                throw new ArgumentNullException(nameof(replicaAddress));
            }

            if (endpointName == null)
            {
                throw new ArgumentNullException(nameof(endpointName));
            }

            var serializer = new JavaScriptSerializer();
            var endpoints = (serializer.DeserializeObject(replicaAddress) as Dictionary<string, object>)["Endpoints"]
                as IEnumerable<KeyValuePair<string, object>>;
            var endpointUri = endpoints.First(kvp => kvp.Key == endpointName).Value as string;

            return new Uri(endpointUri);
        }

        /// <summary>
        /// Gets a list of service URI
        /// </summary>
        /// <param name="serviceUri">Service URI</param>
        /// <param name="endpointName">Name of endpoint</param>
        /// <returns>Async task that resolves to a list of URI</returns>
        public async Task<IReadOnlyList<Uri>> GetServiceEndpoints(Uri serviceUri, string endpointName)
        {
            var endpointList = new List<Uri>();

            await this.ForEachPartition(serviceUri, async partition =>
            {
                await this.ForEachReplica(partition.PartitionInformation.Id, async replica =>
                {
                    if (string.IsNullOrEmpty(replica.ReplicaAddress))
                    {
                        return;
                    }

                    Uri uri = GetReplicaEndpointUri(replica.ReplicaAddress, endpointName);

                    endpointList.Add(uri);
                    await Task.FromResult<object>(null);
                });
            });

            return endpointList;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.fabricClient.Dispose();
        }

        private async Task ForEachPartition(Uri serviceUri, Func<Partition, Task> func)
        {
            ServicePartitionList partitionList;
            string continuationToken = null;
            do
            {
                partitionList = await this.fabricClient.QueryManager.GetPartitionListAsync(serviceUri, continuationToken);
                continuationToken = partitionList.ContinuationToken;
                foreach (var partition in partitionList)
                {
                    await func(partition);
                }
            }
            while (continuationToken != null);
        }

        private async Task ForEachReplica(Guid partitionId, Func<Replica, Task> func)
        {
            ServiceReplicaList replicaList;
            string continuationToken = null;
            do
            {
                replicaList = await this.fabricClient.QueryManager.GetReplicaListAsync(partitionId, continuationToken);
                continuationToken = replicaList.ContinuationToken;
                foreach (var replica in replicaList)
                {
                    await func(replica);
                }
            }
            while (continuationToken != null);
        }
    }
}
