// <copyright file="TestTcpPing.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.DistributedTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Fabric;
    using System.Linq;
    using System.Net.Sockets;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Vega.Test.Helpers;
    using DistTestCommonProto;

    /// <summary>
    /// TCP ping the specified hosts
    /// </summary>
    internal sealed class TestTcpPing : ITestJob
    {
        /// <summary>
        /// List of host names to ping
        /// </summary>
        private string[] hosts;

        /// <summary>
        /// List of ports to ping
        /// </summary>
        private int[] ports;

        /// <summary>
        /// Initializes a new test job from the given parameters
        /// </summary>
        /// <param name="parameters">Test execution parameters</param>
        /// <param name="serviceContext">service context</param>
        /// <returns>async task</returns>
        public Task Initialize(Dictionary<string, string> parameters, StatelessServiceContext serviceContext)
        {
            var regex = new Regex(@"[\s,;]+");
            this.hosts = regex.Split(parameters["Hosts"]);
            this.ports = regex.Split(parameters["Ports"]).Select(p => int.Parse(p)).ToArray();

            if (string.IsNullOrEmpty(parameters["Hosts"]))
            {
                var serviceInfo = Helpers.GetVegaServiceInfo().Result;
                this.hosts = new string[] { serviceInfo.Item1.Split(':')[0] };
            }

            return Task.FromResult(0);
        }

        /// <summary>
        /// Starts the test job
        /// </summary>
        /// <param name="jobState">Job state object</param>
        /// <param name="cancellation">Cancellation token for cancelling the execution</param>
        /// <returns>async task</returns>
        public async Task Start(JobState jobState, CancellationToken cancellation)
        {
            var durations = new List<TimeSpan>();

            jobState.Started = true;

            foreach (var host in this.hosts)
            {
                foreach (var port in this.ports)
                {
                    durations.Add(await PingHost(host, port, cancellation).ConfigureAwait(false));
                }
            }

            jobState.DurationMs.AddRange(durations.Select(d => d.TotalMilliseconds));
            if (durations.All(d => d != TimeSpan.MaxValue))
            {
                jobState.Passed = true;
            }

            jobState.Completed = true;

            VegaDistTestEventSource.Log.JobCompleted(this.GetType().Name);
        }

        /// <summary>
        /// return the jobMetrics if the job needs to report them to test client.
        /// </summary>
        /// <returns>job metrics</returns>
        public Dictionary<string, double[]> GetJobMetrics()
        {
            return null;
        }

        /// <summary>
        /// Runs TCP ping to a given host
        /// </summary>
        /// <param name="host">Host name</param>
        /// <param name="port">port number</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>Duration of the ping</returns>
        private static async Task<TimeSpan> PingHost(string host, int port, CancellationToken cancel)
        {
            var stopWatch = Stopwatch.StartNew();

            if (cancel.IsCancellationRequested)
            {
                return TimeSpan.MaxValue;
            }

            using (var tcpClient = new TcpClient())
            {
                try
                {
                    await tcpClient.ConnectAsync(host, port).ConfigureAwait(false);
                }
                catch (SocketException)
                {
                    return TimeSpan.MaxValue;
                }
            }

            return stopWatch.Elapsed;
        }
    }
}
