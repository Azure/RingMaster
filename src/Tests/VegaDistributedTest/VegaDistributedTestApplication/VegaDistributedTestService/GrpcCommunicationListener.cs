// <copyright file="GrpcCommunicationListener.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.DistributedTest
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Grpc.Core;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;

    /// <summary>
    /// The Grpc communication listerner.
    /// </summary>
    /// <seealso cref="Microsoft.ServiceFabric.Services.Communication.Runtime.ICommunicationListener" />
    internal class GrpcCommunicationListener : ICommunicationListener
    {
        /// <summary>
        /// The service definitions
        /// </summary>
        private readonly IEnumerable<ServerServiceDefinition> serviceDefinitions;

        /// <summary>
        /// The grpc server
        /// </summary>
        private Server server;

        /// <summary>
        /// The server's hostname
        /// </summary>
        private readonly string hostName;

        /// <summary>
        /// The port number 
        /// </summary>
        private readonly int port;

        /// <summary>
        /// Initializes a new instance of the <see cref="GrpcCommunicationListener" /> class.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="hostName">Name of the host.</param>
        /// <param name="port">The port.</param>
        public GrpcCommunicationListener(
            IEnumerable<ServerServiceDefinition> services,
            string hostName,
            int port)
        {
            this.serviceDefinitions = services ?? throw new ArgumentNullException(nameof(services));
            this.hostName = hostName;
            this.port = port;
        }

        /// <summary>
        /// Aborts this instance.
        /// </summary>
        public void Abort()
        {
            VegaDistTestEventSource.Log.GeneralMessage("aborting grpc server");
            StopServerAsync().Wait();
        }

        /// <summary>
        /// Closes the asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>async task</returns>
        public Task CloseAsync(CancellationToken cancellationToken)
        {
            VegaDistTestEventSource.Log.GeneralMessage("closing grpc server");
            return StopServerAsync();
        }

        /// <summary>
        /// Opens the asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>the server address</returns>
        public async Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            try
            {
                this.server = new Server
                {
                    Ports = {new ServerPort(this.hostName, this.port, ServerCredentials.Insecure)}
                };

                foreach (var service in this.serviceDefinitions)
                {
                    this.server.Services.Add(service);
                }

                server.Start();

                VegaDistTestEventSource.Log.GeneralMessage($"GRPC server started on http://{this.hostName}:{this.port}");

                return $"http://{this.hostName}:{this.port}";
            }
            catch (Exception ex)
            {
                VegaDistTestEventSource.Log.GeneralMessage($"Error when starting GRPC server: {ex}");
                await this.StopServerAsync();

                throw;
            }
        }

        private async Task StopServerAsync()
        {
            if (this.server != null)
            {
                try
                {
                    await this.server.ShutdownAsync();
                }
                catch (Exception ex)
                {
                    VegaDistTestEventSource.Log.GeneralMessage($"Error in StopServerAsync: {ex}");
                }
            }
        }
    }

}
