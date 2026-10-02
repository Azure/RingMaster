// <copyright file="DistributedJobController.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.DistributedTest
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Fabric.Query;
    using System.Linq;
    using System.Threading.Tasks;

    using Grpc.Core;

    using Microsoft.ServiceFabric.Services.Communication;
    using Microsoft.Vega.DistributedJobControllerProto;
    using Microsoft.Vega.DistTestCommonProto;
    using Microsoft.Vega.Test.Helpers;

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
            this.fabricClient = Helpers.CreateFabricClient();
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
        /// <param name="request">The request.</param>
        /// <param name="context">The context.</param>
        /// <returns>
        /// async task
        /// </returns>
        public override async Task<Empty> CancelRunningJob(Empty request, ServerCallContext context)
        {
            await this.RunOnAllClients(
                async (c, nodeContext) =>
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
        /// <param name="request">The request.</param>
        /// <param name="context">The context.</param>
        /// <returns>
        /// async task resolving to the job state
        /// </returns>
        public override async Task<GetJobStatesReply> GetJobStates(Empty request, ServerCallContext context)
        {
            var results = await this.RunOnAllClients(
                async (c, nodeContext) => await c.GetJobStateAsync(request))
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
                 async (c, nodeContext) => await c.GetJobMetricsAsync(new JobRunnerProto.GetJobMetricsRequest()
                 {
                     MetricName = request.MetricName,
                     StartIndex = request.StartIndex,
                     PageSize = request.PageSize,
                 }))
                .ConfigureAwait(false);

            var reply = new GetJobMetricsReply();
            reply.JobMetrics.AddRange(temp.Where(t => t != null).SelectMany(t => t.JobMetrics));

            return reply;
        }

        /// <summary>
        /// Gets the identity of the service instance
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="context">The context.</param>
        /// <returns>
        /// async task resolving to the list of service identities
        /// </returns>
        public override async Task<GetServiceInstanceIdentitiesReply> GetServiceInstanceIdentities(Empty request, ServerCallContext context)
        {
            var temp = await this.RunOnAllClients(
                async (c, nodeContext) => await c.GetServiceInstanceIdentityAsync(request))
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
                async (c, nodeContext) =>
                {
                    var initJobRequest = new JobRunnerProto.InitJobRequest()
                    {
                        Scenario = request.Scenario,
                        TestNodeContext = new JobRunnerProto.TestNodeContext()
                        {
                            TestNodeId = nodeContext.TestNodeId,
                            TargetServiceIndex = nodeContext.TargetServiceIndex,
                        },
                    };

                    initJobRequest.Parameters.AddRange(request.Parameters);
                    return await c.InitializeJobAsync(initJobRequest);
                },
                request.ServiceInstanceCount)
                .ConfigureAwait(false);

            var unused = Task.Run(
                async () =>
                {
                    var emptyMessage = new Empty();
                    await this.RunOnAllClients(
                        async (c, nodeContext) => await c.StartJobAsync(emptyMessage),
                        request.ServiceInstanceCount)
                        .ConfigureAwait(false);

                    await this.RunOnAllClients(
                        async (c, nodeContext) => await c.CleanupJobAsync(emptyMessage),
                        request.ServiceInstanceCount)
                        .ConfigureAwait(false);
                });

            return new Empty();
        }

        /// <summary>
        /// Run test on all clients
        /// </summary>
        /// <typeparam name="TResult">Type name of the result value list</typeparam>
        /// <param name="asyncAction">Async action to run on all clients</param>
        /// <param name="serviceCount">The ring master service count.</param>
        /// <returns>
        /// Async task
        /// </returns>
        private async Task<TResult[]> RunOnAllClients<TResult>(Func<JobRunnerSvcClient, TestNodeContext, Task<TResult>> asyncAction, int serviceCount = 1)
        {
            var results = new List<TResult>();

            var serviceName = this.serviceContext.ServiceName;
            var partitionList = await this.fabricClient.QueryManager.GetPartitionListAsync(serviceName).ConfigureAwait(false);
            var allReplica = new List<Replica>();
            foreach (var partition in partitionList)
            {
                var serviceReplicaList = await this.fabricClient.QueryManager.GetReplicaListAsync(partition.PartitionInformation.Id).ConfigureAwait(false);
                allReplica.AddRange(serviceReplicaList);
            }

            // make sure replicas always in the same order so they get the same node context number every time.
            allReplica.Sort((r1, r2) => r1.Id.CompareTo(r2.Id));

            var allTasks = new List<Task<TResult>>();
            var allChannels = new List<Channel>();
            for (int i = 0; i < allReplica.Count; i++)
            {
                if (!ServiceEndpointCollection.TryParseEndpointsString(allReplica[i].ReplicaAddress, out ServiceEndpointCollection endpoints))
                {
                    VegaDistTestEventSource.Log.ParseEndpointsStringFailed(allReplica[i].ReplicaAddress);
                    continue;
                }

                if (!endpoints.TryGetEndpointAddress("GrpcEndpoint", out string jobControlEndpoint))
                {
                    VegaDistTestEventSource.Log.GetEndpointAddressFailed("GrpcEndpoint", allReplica[i].ReplicaAddress);
                    continue;
                }

                var channel = new Channel(jobControlEndpoint.Replace(@"http://", string.Empty), ChannelCredentials.Insecure);
                JobRunnerSvcClient client = new JobRunnerSvcClient(channel);
                var testNodeContext = new TestNodeContext(i, i % serviceCount);
                allTasks.Add(asyncAction(client, testNodeContext));

                allChannels.Add(channel);
            }

            try
            {
                await Task.WhenAll(allTasks);
                allTasks.ForEach(t => results.Add(t.Result));
            }
            catch (Exception ex)
            {
                VegaDistTestEventSource.Log.RunJobOnClientFailed(ex.ToString());
            }
            finally
            {
                allChannels.ForEach(async channel =>
                {
                    if (channel != null && channel.State != ChannelState.Shutdown)
                    {
                        await channel.ShutdownAsync();
                    }
                });
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
