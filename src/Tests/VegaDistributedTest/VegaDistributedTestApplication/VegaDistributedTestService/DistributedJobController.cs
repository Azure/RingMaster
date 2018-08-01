// <copyright file="DistributedJobController.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.DistributedTest
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Linq;
    using System.Threading.Tasks;
    using DistTestCommonProto;
    using Grpc.Core;
    using Microsoft.Vega.DistributedJobControllerProto;
    using ServiceFabric.Services.Communication;
    using static Microsoft.Vega.DistributedJobControllerProto.DistributedJobControllerSvc;
    using static Microsoft.Vega.JobRunnerProto.JobRunnerSvc;

    /// <summary>
    /// Controls the test job running on distributed service instances
    /// </summary>
    public sealed class DistributedJobController : DistributedJobControllerSvcBase, IDisposable
    {
        /// <summary>
        /// Service context
        /// </summary>
        private readonly StatelessServiceContext serviceContext;

        /// <summary>
        /// Service fabric client for querying various properties of the service
        /// </summary>
        private readonly FabricClient fabricClient;

        /// <summary>
        /// If the object has been disposed
        /// </summary>
        private bool disposedValue = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="DistributedJobController"/> class.
        /// </summary>
        /// <param name="context">Service context</param>
        public DistributedJobController(StatelessServiceContext context)
        {
            this.serviceContext = context;
            this.fabricClient = new FabricClient();
        }

        /// <summary>
        /// Disposes this object
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Cancels the currently running job
        /// </summary>
        /// <returns>async task</returns>
        public override async Task<Empty> CancelRunningJob(Empty request, ServerCallContext context)
        {
            await this.RunOnAllClients(
                async (c) =>
                {
                    await c.CancelRunningJobAsync(request);
                    return 0;
                })
                .ConfigureAwait(false);

            return new Empty();
        }

        /// <summary>
        /// Gets the job state
        /// </summary>
        /// <returns>async task resolving to the job state</returns>
        public override async Task<GetJobStatesReply> GetJobStates(Empty request, ServerCallContext context)
        {
            var results = await this.RunOnAllClients(
                async (c) => await c.GetJobStateAsync(request))
                .ConfigureAwait(false);

            var reply = new GetJobStatesReply();
            reply.JobStates.Add(results.Select(r => r.JobState));

            return reply;
        }

        /// <summary>
        /// get the job metrics, not like JobState, this might contain huge number of metrics data.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="context">The context.</param>
        /// <returns>job metrics, by page</returns>
        public override async Task<GetJobMetricsReply> GetJobMetrics(GetJobMetricsRequest request, ServerCallContext context)
        {
            var temp = await this.RunOnAllClients(
                 async (c) => await c.GetJobMetricsAsync(new JobRunnerProto.GetJobMetricsRequest()
                 {
                     MetricName = request.MetricName,
                     StartIndex = request.StartIndex,
                     PageSize = request.PageSize
                 }))
                .ConfigureAwait(false);

            var reply = new GetJobMetricsReply();
            reply.JobMetrics.AddRange(temp.Where(t => t != null).SelectMany(t => t.JobMetrics));

            return reply;
        }

        /// <summary>
        /// Gets the identity of the service instance
        /// </summary>
        /// <returns>async task resolving to the list of service identities</returns>
        public override async Task<GetServiceInstanceIdentitiesReply> GetServiceInstanceIdentities(Empty request, ServerCallContext context)
        {
            var temp = await this.RunOnAllClients(
                async (c) => await c.GetServiceInstanceIdentityAsync(request))
                .ConfigureAwait(false);

            var reply = new GetServiceInstanceIdentitiesReply();
            reply.ServiceInstanceIdentities.AddRange(temp.Select(t => t.ServiceInstanceIdentity));

            return reply;
        }

        /// <summary>
        /// Starts the given job on the service instance
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="context">The context.</param>
        /// <returns>async task</returns>
        public override async Task<Empty> StartJob(StartJobRequest request, ServerCallContext context)
        {
            await this.RunOnAllClients(
                async (c) =>
                {
                    var startJobRequest = new JobRunnerProto.StartJobRequest()
                    {
                        Scenario = request.Scenario,
                    };

                    startJobRequest.Parameters.AddRange(request.Parameters);
                    await c.StartJobAsync(startJobRequest);
                    return 0;
                })
                .ConfigureAwait(false);

            return new Empty();
        }

        /// <summary>
        /// Run test on all clients
        /// </summary>
        /// <typeparam name="TResult">Type name of the result value list</typeparam>
        /// <param name="asyncAction">Async action to run on all clients</param>
        /// <returns>Async task</returns>
        private async Task<TResult[]> RunOnAllClients<TResult>(Func<JobRunnerSvcClient, Task<TResult>> asyncAction)
        {
            var results = new List<TResult>();

            var serviceName = this.serviceContext.ServiceName;
            var partitionList = await this.fabricClient.QueryManager.GetPartitionListAsync(serviceName).ConfigureAwait(false);
            foreach (var partition in partitionList)
            {
                var serviceReplicaList = await this.fabricClient.QueryManager.GetReplicaListAsync(partition.PartitionInformation.Id).ConfigureAwait(false);
                foreach (var replica in serviceReplicaList)
                {
                    if (!ServiceEndpointCollection.TryParseEndpointsString(replica.ReplicaAddress, out ServiceEndpointCollection endpoints))
                    {
                        VegaDistTestEventSource.Log.ParseEndpointsStringFailed(replica.ReplicaAddress);
                        continue;
                    }

                    if (!endpoints.TryGetEndpointAddress("GrpcEndpoint", out string jobControlEndpoint))
                    {
                        VegaDistTestEventSource.Log.GetEndpointAddressFailed("GrpcEndpoint", replica.ReplicaAddress);
                        continue;
                    }

                    Channel channel = null;
                    try
                    {
                        channel = new Channel(jobControlEndpoint.Replace(@"http://", string.Empty), ChannelCredentials.Insecure);
                        JobRunnerSvcClient client = new JobRunnerSvcClient(channel);

                        results.Add(await asyncAction(client).ConfigureAwait(false));
                    }
                    catch (Exception ex)
                    {
                        VegaDistTestEventSource.Log.RunJobOnClientFailed(jobControlEndpoint, ex.ToString());
                    }
                    finally
                    {
                        if (channel != null)
                        {
                            await channel.ShutdownAsync();
                        }
                    }
                }
            }

            return results.ToArray();
        }

        /// <summary>
        /// Disposes this object
        /// </summary>
        /// <param name="disposing">True if dispose managed, false if otherwise</param>
        private void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.fabricClient.Dispose();
                }

                this.disposedValue = true;
            }
        }
    }
}
