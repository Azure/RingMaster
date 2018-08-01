// <copyright file="ITestJob.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.DistributedTest
{
    using DistTestCommonProto;
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Test job runs in a service instance
    /// </summary>
    internal interface ITestJob
    {
        /// <summary>
        /// Initializes a new test job from the given parameters
        /// </summary>
        /// <param name="parameters">Test execution parameters</param>
        /// <param name="serviceContext">service context</param>
        /// <returns>async task</returns>
        Task Initialize(Dictionary<string, string> parameters, StatelessServiceContext serviceContext);

        /// <summary>
        /// Starts the test job
        /// </summary>
        /// <param name="jobState">Job state object</param>
        /// <param name="cancellation">Cancellation token for cancelling the execution</param>
        /// <returns>async task</returns>
        Task Start(JobState jobState, CancellationToken cancellation);

        /// <summary>
        /// return the job metrics
        /// </summary>
        /// <returns>job metrics</returns>
        Dictionary<string, double[]> GetJobMetrics();
    }

    /// <summary>
    /// Factory to create test job instance
    /// </summary>
    internal static class TestJobFactory
    {
        /// <summary>
        /// List of all test job classes
        /// </summary>
        private static readonly Dictionary<string, Type> TypesOfTestJob;

        static TestJobFactory()
        {
            TypesOfTestJob = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

            var assembly = Assembly.GetExecutingAssembly();
            foreach (var type in assembly.GetTypes())
            {
                if (type.GetInterface(nameof(ITestJob)) == null)
                {
                    continue;
                }

                TypesOfTestJob[type.Name] = type;
            }
        }

        /// <summary>
        /// Creates a new test job from the given parameters
        /// </summary>
        /// <param name="name">Test job class name without namespace</param>
        /// <param name="parameters">Test execution parameters</param>
        /// <param name="serviceContext">the service context</param>
        /// <returns>Test job object</returns>
        internal static async Task<ITestJob> Create(string name, Dictionary<string, string> parameters, StatelessServiceContext serviceContext)
        {
            Type jobType;
            if (TypesOfTestJob.TryGetValue(name, out jobType))
            {
                var job = Activator.CreateInstance(jobType) as ITestJob;
                await job.Initialize(parameters, serviceContext);
                return job;
            }
            else
            {
                return null;
            }
        }
    }
}
