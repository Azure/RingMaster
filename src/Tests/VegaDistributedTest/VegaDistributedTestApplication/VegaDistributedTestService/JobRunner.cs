// <copyright file="JobRunner.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.DistributedTest
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using DistTestCommonProto;
    using Grpc.Core;
    using Microsoft.Vega.JobRunnerProto;

    /// <summary>
    /// Runs the specified test job on a single service instance
    /// </summary>
    public sealed class JobRunner : JobRunnerSvc.JobRunnerSvcBase, IDisposable
    {
        /// <summary>
        /// Service context
        /// </summary>
        private readonly StatelessServiceContext serviceContext;

        /// <summary>
        /// Job control parameters and result data
        /// </summary>
        private JobState jobState;

        /// <summary>
        /// job metrics
        /// </summary>
        private Dictionary<string, double[]> jobMetrics;

        /// <summary>
        /// For cancelling the running job
        /// </summary>
        private CancellationTokenSource cancellationSource;

        /// <summary>
        /// If the object has been disposed
        /// </summary>
        private bool disposedValue = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="JobRunner"/> class.
        /// </summary>
        /// <param name="context">Service context</param>
        public JobRunner(StatelessServiceContext context)
        {
            this.serviceContext = context;
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
        public override Task<Empty> CancelRunningJob(Empty request, ServerCallContext context)
        {
            this.cancellationSource?.Cancel();

            var jobState = this.jobState;
            if (jobState != null)
            {
                jobState.Completed = true;
                jobState.Status = "Cancelled";
            }

            VegaDistTestEventSource.Log.JobCancelRequested();
            return Task.FromResult(new Empty());
        }

        /// <summary>
        /// Gets the job state
        /// </summary>
        /// <returns>async task resolving to the job state</returns>
        public override Task<GetJobStateReply> GetJobState(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new GetJobStateReply()
            {
                JobState = this.jobState
            });
        }

        /// <summary>
        /// get the job metrics, not like JobState, this might contain huge number of metrics data.
        /// </summary>
        /// <param name="metricName">the name of metric</param>
        /// <param name="startIndex">start index of the page</param>
        /// <param name="pageSize">the size of page</param>
        /// <returns>job metrics, by page</returns>
        public override Task<GetJobMetricsReply> GetJobMetrics(GetJobMetricsRequest request, ServerCallContext context)
        {
            double[] result = null;

            if (this.jobMetrics != null && this.jobMetrics.ContainsKey(request.MetricName) && this.jobMetrics[request.MetricName].Length > request.StartIndex)
            {
                result = this.jobMetrics[request.MetricName].Skip(request.StartIndex).Take(Math.Min(request.PageSize, this.jobMetrics[request.MetricName].Length - request.StartIndex)).ToArray();
            }

            var reply = new GetJobMetricsReply();
            reply.JobMetrics.AddRange(result);
            return Task.FromResult(reply);
        }

        /// <summary>
        /// Gets the identity of the service instance
        /// </summary>
        /// <returns>async task resolving to the service identity</returns>
        public override Task<GetServiceInstanceIdentityReply> GetServiceInstanceIdentity(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new GetServiceInstanceIdentityReply()
            {
                ServiceInstanceIdentity = string.Join(
                "/",
                this.serviceContext.NodeContext.NodeName,
                this.serviceContext.ReplicaOrInstanceId)
            });   
        }

        /// <summary>
        /// Starts the given job on the service instance
        /// </summary>
        /// <param name="scenario">Name of the test scenario</param>
        /// <param name="parameters">Test parameters in key-value pairs</param>
        /// <returns>async task</returns>
        public override async Task<Empty> StartJob(StartJobRequest request, ServerCallContext context)
        {
            if (this.jobState != null && !this.jobState.Completed)
            {
                return new Empty();
            }

            this.jobState = new JobState
            {
                Scenario = request.Scenario,
            };
            this.cancellationSource = new CancellationTokenSource();

            var paramString = string.Join(", ", request.Parameters.Select(kv => string.Concat(kv.Key, "=", kv.Value)));

            VegaDistTestEventSource.Log.StartJob(request.Scenario, paramString);
            try
            {
                var job = await TestJobFactory.Create(request.Scenario, GrpcHelper.GetJobParametersFromRepeatedField(request.Parameters), this.serviceContext).ConfigureAwait(false);
                var unused = Task.Run(
                    async () =>
                    {
                        try
                        {
                            this.jobMetrics = null;
                            await job.Start(this.jobState, this.cancellationSource.Token).ConfigureAwait(false);

                            this.jobMetrics = job.GetJobMetrics();
                        }
                        catch (TaskCanceledException)
                        {
                            VegaDistTestEventSource.Log.StartJobCancelled(request.Scenario);
                            this.jobState.Status += "Task cancelled";
                        }
                        catch (Exception ex)
                        {
                            VegaDistTestEventSource.Log.StartJobFailed(request.Scenario, ex.ToString());
                            this.jobState.Status += ex.ToString();
                        }

                        this.jobState.Completed = true;
                    });
            }
            catch (Exception ex)
            {
                VegaDistTestEventSource.Log.ScheduleJobFailed(request.Scenario, ex.ToString());
                this.jobState.Completed = true;
                this.jobState.Status = ex.ToString();
            }

            VegaDistTestEventSource.Log.JobScheduled(request.Scenario);

            return new Empty();
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
                    if (this.cancellationSource != null)
                    {
                        this.cancellationSource.Dispose();
                        this.cancellationSource = null;
                    }
                }

                this.disposedValue = true;
            }
        }
    }
}
