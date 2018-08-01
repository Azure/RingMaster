// <copyright file="VegaDistributedTestService.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.DistributedTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Fabric;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Cloud.InstrumentationFramework;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;
    using Extensions.Configuration;
    using System.IO;
    using DistTestCommonProto;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Distributed test micro-service
    /// </summary>
    internal sealed class VegaDistributedTestService : StatelessService, IDisposable
    {
        /// <summary>
        /// Distributed job controller
        /// </summary>
        private DistributedJobController jobControllerService;

        /// <summary>
        /// Job runner service for running tests on individual service instance
        /// </summary>
        private JobRunner jobRunnerService;

        /// <summary>
        /// If the object has been disposed
        /// </summary>
        private bool disposedValue = false;

        /// <summary>
        /// The application settings
        /// </summary>
        private static IConfiguration appSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="VegaDistributedTestService"/> class.
        /// </summary>
        /// <param name="serviceContext">Service context</param>
        public VegaDistributedTestService(StatelessServiceContext serviceContext)
            : base(serviceContext)
        {
        }

        private static int GrpcServerPort
        {
            get
            {
                return string.Equals(Environment.GetEnvironmentVariable("Fabric_NodeIPOrFQDN"), "localhost")
                    ? GetRandomPort() // hack for localhost
                    : 18600;
            }
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
        /// Runs background task in the service, currently only the cancellation registration
        /// </summary>
        /// <param name="cancellationToken">Cancelled if the service is being terminated</param>
        /// <returns>async task</returns>
        protected override Task RunAsync(CancellationToken cancellationToken)
        {
            cancellationToken.Register(
                () =>
                {
                    VegaDistTestEventSource.Log.RunAsyncCanceled();
                    this.Dispose();
                });

            VegaDistTestEventSource.Log.RunAsyncCompleted();
            return Task.FromResult(0);
        }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            try
            {
                if (this.jobControllerService == null)
                {
                    this.jobControllerService = new DistributedJobController(this.Context);
                }

                if (this.jobRunnerService == null)
                {
                    this.jobRunnerService = new JobRunner(this.Context);
                }

                var host = this.Context.NodeContext.IPAddressOrFQDN;
                var listeners = new[]
                {
                    new ServiceInstanceListener(serviceContext =>
                        new GrpcCommunicationListener(new []
                            {
                                DistributedJobControllerProto.DistributedJobControllerSvc.BindService(this.jobControllerService),
                                JobRunnerProto.JobRunnerSvc.BindService(this.jobRunnerService)
                            },
                            host,
                            GrpcServerPort),
                        "GrpcEndpoint")
                };

                VegaDistTestEventSource.Log.RegisterListenersSucceeded();
                return listeners;
            }
            catch (Exception ex)
            {
                VegaDistTestEventSource.Log.RegisterListenersFailed(ex.ToString());
            }

            return null;
        }

        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            VegaDistributedTestService vegaDistributedTestService = null;
            try
            {
                var path = Assembly.GetExecutingAssembly().Location;
                var builder = new ConfigurationBuilder().SetBasePath(Path.GetDirectoryName(path)).AddJsonFile("appSettings.json");
                appSettings = builder.Build();

                var tenantName = GetClusterNameFromMsa().GetAwaiter().GetResult();

                var nodeName = FabricRuntime.GetNodeContext().NodeName;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var errorContext = default(ErrorContext);

                    var defaultDimensions = new Dictionary<string, string>
                    {
                        { "Tenant", tenantName },
                        { "Node", nodeName },
                    };

                    if (!DefaultConfiguration.SetDefaultDimensionNamesValues(
                        ref errorContext,
                        (uint)defaultDimensions.Count,
                        defaultDimensions.Keys.ToArray(),
                        defaultDimensions.Values.ToArray()))
                    {
                        VegaDistTestEventSource.Log.SetMdmDimensionFailed(errorContext.ErrorCode, errorContext.ErrorMessage);
                    }
                }

                // Ensure the event source is loaded
                Assembly.GetAssembly(typeof(ITestJob))
                .GetType("Microsoft.Vega.DistributedTest.VegaDistTestEventSource")
                ?.GetProperty("Log")
                ?.GetValue(null);

                LogFileEventTracing.Start(Path.Combine(appSettings["LogFolder"], "VegaDistributedTestService.LogPath"));
                Trace.Listeners.Add(new LogFileTraceListener());
                LogFileEventTracing.AddEventSource("Microsoft-Azure-Networking-Infrastructure-RingMaster-DistributedTestService");

                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                AppDomain.CurrentDomain.ProcessExit +=
                    (sender, eventArgs) =>
                    {
                        LogFileEventTracing.Stop();
                    };
            
                // The ServiceManifest.XML file defines one or more service type names.
                // Registering a service maps a service type name to a .NET type.
                // When Service Fabric creates an instance of this service type,
                // an instance of the class is created in this host process.
                ServiceRuntime.RegisterServiceAsync(
                    "VegaDistTestSvc",
                    context => vegaDistributedTestService = new VegaDistributedTestService(context))
                    .GetAwaiter()
                    .GetResult();

                VegaDistTestEventSource.Log.RegisterServiceSucceeded();

                // Prevents this host process from terminating so services keep running.
                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                VegaDistTestEventSource.Log.RegisterServiceFailed(ex.ToString());
                throw;
            }
            finally
            {
                if (vegaDistributedTestService != null)
                {
                    vegaDistributedTestService.Dispose();
                }
            }
        }

        /// <summary>
        /// Discovers the cluster name from MSA app in the same service fabric cluster and returns it as the tenant name
        /// </summary>
        /// <returns>Cluster name of the MSA app</returns>
        private static async Task<string> GetClusterNameFromMsa()
        {
            var msaTypeName = appSettings["MsaTypeName"];

            using (var fabricClient = new FabricClient())
            {
                foreach (var app in await fabricClient.QueryManager.GetApplicationListAsync().ConfigureAwait(false))
                {
                    if (app.ApplicationTypeName != msaTypeName)
                    {
                        continue;
                    }

                    return app.ApplicationParameters["ClusterName"].Value;
                }
            }

            return string.Empty;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Trace.TraceError($"VegaDistTestSvc.UnhandledException exception={e.ExceptionObject}, isTerminating={e.IsTerminating}");
        }

        private static int GetRandomPort()
        {
            var rnd = new Random();
            return rnd.Next(20000, 30000);
        }

        /// <summary>
        /// Disposes this object
        /// </summary>
        /// <param name="disposing">If disposing managed fields</param>
        private void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    if (this.jobRunnerService != null)
                    {
                        this.jobRunnerService.CancelRunningJob(new Empty(), null);
                        this.jobRunnerService.Dispose();
                        this.jobRunnerService = null;
                    }

                    if (this.jobControllerService != null)
                    {
                        this.jobControllerService.Dispose();
                        this.jobControllerService = null;
                    }
                }

                this.disposedValue = true;
            }
        }
    }
}
