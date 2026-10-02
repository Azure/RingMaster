// <copyright file="JobRunner.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.DistributedTest
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    using Grpc.Core;

    using Microsoft.Vega.DistTestCommonProto;
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
        /// The current running job
        /// </summary>
        private ITestJob currentJob;

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
        /// Initializes a new instance of the <see cref="JobRunner" /> class.
        /// </summary>
        /// <param name="context">Service context</param>
        /// <param name="testCodeAssembly">The test code assembly.</param>
        public JobRunner(StatelessServiceContext context, Assembly testCodeAssembly = null)
        {
            this.serviceContext = context;

            TestJobFactory.Initialize(testCodeAssembly);
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
        /// <param name="request">The request.</param>
        /// <param name="context">The context.</param>
        /// <returns>
        /// async task resolving to the job state
        /// </returns>
        public override Task<GetJobStateReply> GetJobState(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new GetJobStateReply()
            {
                JobState = this.jobState,
            });
        }

        /// <summary>
        /// get the job metrics, not like JobState, this might contain huge number of metrics data.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="context">The context.</param>
        /// <returns>
        /// job metrics, by page
        /// </returns>
        public override Task<GetJobMetricsReply> GetJobMetrics(GetJobMetricsRequest request, ServerCallContext context)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            List<double> result = new List<double>();
            if (this.jobMetrics != null && this.jobMetrics.ContainsKey(request.MetricName) && this.jobMetrics[request.MetricName].Length > request.StartIndex)
            {
                result = this.jobMetrics[request.MetricName].Skip(request.StartIndex).Take(Math.Min(request.PageSize, this.jobMetrics[request.MetricName].Length - request.StartIndex)).ToList();
            }

            var reply = new GetJobMetricsReply();
            reply.JobMetrics.AddRange(result);

            return Task.FromResult(reply);
        }

        /// <summary>
        /// Gets the identity of the service instance
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="context">The context.</param>
        /// <returns>
        /// async task resolving to the service identity
        /// </returns>
        public override Task<GetServiceInstanceIdentityReply> GetServiceInstanceIdentity(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new GetServiceInstanceIdentityReply()
            {
                ServiceInstanceIdentity = string.Join(
                "/",
                this.serviceContext.NodeContext.NodeName,
                this.serviceContext.ReplicaOrInstanceId),
            });
        }

        /// <summary>
        /// Initializes the job.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="context">The context.</param>
        /// <returns>async task</returns>
        public override async Task<Empty> InitializeJob(InitJobRequest request, ServerCallContext context)
        {
            if (this.jobState != null && !this.jobState.Completed)
            {
                return new Empty();
            }

            this.jobState = new JobState
            {
                Scenario = request.Scenario,
            };

            var paramString = string.Join(", ", request.Parameters.Select(kv => string.Concat(kv.Key, "=", kv.Value)));
            VegaDistTestEventSource.Log.InitializeJob(request.Scenario, paramString);

            this.currentJob = await TestJobFactory.Create(
                request.Scenario,
                GrpcHelper.GetJobParametersFromRepeatedField(request.Parameters),
                this.serviceContext,
                new TestNodeContext(request.TestNodeContext.TestNodeId, request.TestNodeContext.TargetServiceIndex))
                .ConfigureAwait(false);

            return new Empty();
        }

        /// <summary>
        /// Cleanups the job.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="context">The context.</param>
        /// <returns>async task</returns>
        public override async Task<Empty> CleanupJob(Empty request, ServerCallContext context)
        {
            await this.currentJob.Cleanup();

            return new Empty();
        }

        /// <summary>
        /// Starts the given job on the service instance
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="context">The context.</param>
        /// <returns>
        /// async task
        /// </returns>
        public override async Task<Empty> StartJob(Empty request, ServerCallContext context)
        {
            try
            {
                this.cancellationSource = new CancellationTokenSource();
                this.jobMetrics = null;
                await this.currentJob.Start(this.jobState, this.cancellationSource.Token).ConfigureAwait(false);

                this.jobMetrics = this.currentJob.GetJobMetrics();
            }
            catch (TaskCanceledException)
            {
                VegaDistTestEventSource.Log.StartJobCancelled();
                this.jobState.Status += "Task cancelled";
            }
            catch (Exception ex)
            {
                VegaDistTestEventSource.Log.StartJobFailed(ex.ToString());
                this.jobState.Status += ex.ToString();
            }
            finally
            {
                this.jobState.Completed = true;
            }

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
