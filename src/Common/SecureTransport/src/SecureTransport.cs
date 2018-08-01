// <copyright file="SecureTransport.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Net;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.CertificateRules;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication;

    /// <summary>
    /// Secure transport for SSL or non-SSL TCP connection management
    /// </summary>
    public sealed class SecureTransport : ITransport
    {
        private const int ConsecutiveAcceptFailuresLimit = 25;

        private static readonly TimeSpan DefaultStartTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan DefaultStopTimeout = TimeSpan.FromSeconds(5);
        private static readonly char[] ConnectionStringSeparators = new char[] { ',', ';' };
        private static readonly char[] HostPortSeparator = new char[] { ':' };

        private static long lastAssignedTransportId = 0;

        private readonly ISecureConnectionPolicy secureConnectionPolicy;
        private readonly ISecureTransportInstrumentation instrumentation;
        private readonly CancellationToken rootCancellationToken;
        private readonly ConcurrentDictionary<long, Connection> activeConnections = new ConcurrentDictionary<long, Connection>();
        private readonly SemaphoreSlim acceptConnectionsSemaphore;
        private readonly ManualResetEventSlim hasStarted = new ManualResetEventSlim(initialState: false);
        private readonly ManualResetEventSlim hasStopped = new ManualResetEventSlim(initialState: true);
        private readonly Configuration configuration;
        private readonly long transportId;

        private TcpListener listener;
        private CancellationTokenSource cancellationTokenSource;
        private long lastAssignedConnectionId = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureTransport"/> class.
        /// </summary>
        /// <param name="configuration">Configuration of the transport</param>
        public SecureTransport(Configuration configuration)
            : this(configuration, null, CancellationToken.None)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureTransport"/> class.
        /// </summary>
        /// <param name="configuration">Configuration of the transport</param>
        /// <param name="instrumentation">Instrumentation object for getting notification of the internal state</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public SecureTransport(Configuration configuration, ISecureTransportInstrumentation instrumentation, CancellationToken cancellationToken)
        {
            this.transportId = Interlocked.Increment(ref SecureTransport.lastAssignedTransportId);

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (configuration.CommunicationProtocolVersion == 0)
            {
                throw new ArgumentException("CommunicationProtocolVersion must be specified");
            }

            this.configuration = configuration;

            if (configuration.UseSecureConnection)
            {
                var sslConfiguration = new SslConnection.Configuration()
                {
                    ClientCertificates = configuration.ClientCertificates,
                    ServerCertificates = configuration.ServerCertificates,
                    RemoteCertificateValidationCallback = configuration.RemoteCertificateValidationCallback,
                    LocalCertificateSelectionCallback = configuration.LocalCertificateSelectionCallback,
                    IsClientCertificateRequired = configuration.IsClientCertificateRequired,
                    MustCheckCertificateRevocation = configuration.MustCheckCertificateRevocation,
                    MustCheckCertificateTrustChain = configuration.MustCheckCertificateTrustChain,
                    ExplicitRule = configuration.ExplicitRule,
                    Identities = configuration.Identities,
                    SubjectValidations = configuration.SubjectValidations,
                    StartAsClient = configuration.AuthAsClient,
                    BlacklistedThumbprints = configuration.BlacklistedThumbprints,
                };

                this.secureConnectionPolicy = new SslConnection(this.transportId, sslConfiguration);
            }
            else
            {
                this.secureConnectionPolicy = new NoSslConnection();
            }

            this.acceptConnectionsSemaphore = new SemaphoreSlim(this.configuration.MaxConnections, this.configuration.MaxConnections);
            this.MaxConnectionLifeSpan = configuration.MaxConnectionLifespan;
            this.rootCancellationToken = cancellationToken;
            this.instrumentation = instrumentation ?? NoInstrumentation.Instance;
        }

        /// <summary>
        /// Gets or sets the callback that must be invoked whenever a new connection is established.
        /// </summary>
        public Action<IConnection> OnNewConnection { get; set; }

        /// <summary>
        /// Gets or sets the Callback that must be invoked to negotiate protocol
        /// </summary>
        public ProtocolNegotiatorDelegate OnProtocolNegotiation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether tells the connection to use Network byte order when sending data
        /// </summary>
        public bool UseNetworkByteOrder { get; set; } = false;

        /// <summary>
        /// Gets or sets the callback that must be invoked when a connection is lost.
        /// </summary>
        public Action OnConnectionLost { get; set; }

        /// <summary>
        /// Gets a value indicating whether the transport is active, meaning started and not stopped yet.
        /// </summary>
        public bool IsActive
        {
            get
            {
                return this.hasStarted.IsSet && !this.hasStopped.IsSet;
            }
        }

        /// <summary>
        /// Gets or sets the maximum lifespan of a connection established by this transport.
        /// </summary>
        public TimeSpan MaxConnectionLifeSpan
        {
            get; set;
        }

        /// <summary>
        /// Gets the local endpoint of the connection of the listening server
        /// </summary>
        public EndPoint LocalEndpoint
        {
            get
            {
                return this.listener != null
                    ? this.listener.LocalEndpoint
                    : null;
            }
        }

        /// <summary>
        /// Parses the connection string to a list of endpoints
        /// </summary>
        /// <param name="connectionString">Connection string</param>
        /// <returns>Array of IP endpoints</returns>
        public static IPEndPoint[] ParseConnectionString(string connectionString)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            string[] pieces = connectionString.Split(ConnectionStringSeparators, StringSplitOptions.RemoveEmptyEntries);

            var endpoints = new List<IPEndPoint>();
            foreach (var piece in pieces)
            {
                string[] hostAndPort = piece.Split(HostPortSeparator, StringSplitOptions.RemoveEmptyEntries);

                IPAddress host;
                if (!IPAddress.TryParse(hostAndPort[0], out host))
                {
                    // If it cannot be parsed to a valid IP address, it should be resolvable. Otherwise just let it throw.
                    host = Dns.GetHostAddresses(hostAndPort[0])[0];
                }

                ushort port = ushort.Parse(hostAndPort[1]);

                endpoints.Add(new IPEndPoint(host, port));
            }

            return endpoints.ToArray();
        }

        /// <summary>
        /// Gets a list of <see cref="X509Certificate"/>s that corresponds to the given thumbprint or file paths.
        /// </summary>
        /// <param name="paths">List of thumbprint or file paths</param>
        /// <returns> A list of <see cref="X509Certificate"/>s that correspond to the given paths</returns>
        [SuppressMessage(
            "Microsoft.Reliability",
            "CA2000:DisposeObjectsBeforeLosingScope",
            Scope = "method",
            Target = "Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport.SecureTransport.GetCertificatesFromThumbPrintOrFileName()",
            Justification = "X509 certificates will be disposed in the finally block")]
        public static X509Certificate[] GetCertificatesFromThumbPrintOrFileName(string[] paths)
        {
            List<X509Certificate> certificates = new List<X509Certificate>();

            try
            {
                if (paths != null)
                {
                    for (int i = 0; i < paths.Length; i++)
                    {
                        try
                        {
                            string path = paths[i].ToUpper();
                            if (path.StartsWith("FILE:"))
                            {
                                certificates[i] = X509Certificate.CreateFromCertFile(path.Substring("FILE:".Length));
                                continue;
                            }

                            StoreName name;
                            StoreLocation location;
                            string thumbprint;

                            string[] pieces = path.Split('/');
                            if (pieces.Length == 1)
                            {
                                // No store name in the cert. Use default
                                name = StoreName.My;
                                location = StoreLocation.LocalMachine;
                                thumbprint = path;
                            }
                            else
                            {
                                name = (StoreName)Enum.Parse(typeof(StoreName), pieces[0], true);
                                location = (StoreLocation)Enum.Parse(typeof(StoreLocation), pieces[1], true);
                                thumbprint = pieces[2];
                            }

                            using (X509Store store = new X509Store(name, location))
                            {
                                store.Open(OpenFlags.ReadOnly);
                                X509Certificate2 found = null;
                                foreach (X509Certificate2 result in store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false))
                                {
                                    if (found == null || found.Equals(result))
                                    {
                                        found = result;
                                    }
                                    else
                                    {
                                        throw SecureTransportException.DuplicateCertificates(thumbprint);
                                    }
                                }

                                if (found == null)
                                {
                                    throw SecureTransportException.MissingCertificate(thumbprint);
                                }

                                certificates.Add(found);
                            }
                        }
                        catch (Exception ex)
                        {
                            SecureTransportEventSource.Log.GetCertificatesFromThumbprintOrFileNameFailed(ex.ToString());
                        }
                    }
                }

                var certificatesToReturn = new X509Certificate[certificates.Count];
                for (int i = 0; i < certificates.Count; i++)
                {
                    certificatesToReturn[i] = certificates[i];
                    certificates[i] = null;
                }

                return certificatesToReturn;
            }
            finally
            {
                foreach (var cert in certificates)
                {
                    cert?.Dispose();
                }
            }
        }

        /// <summary>
        /// Starts client with default server SSL validation timeout of 5 seconds
        /// </summary>
        /// <param name="endpoints">Endpoints to connect to</param>
        /// <returns>Client task</returns>
        public Task StartClient(params IPEndPoint[] endpoints)
        {
            return this.StartClient(TimeSpan.FromSeconds(5), endpoints);
        }

        /// <summary>
        /// Starts client with a specified server SSL validation timeout
        /// </summary>
        /// <param name="validationTimeout">Connection SSL validation timeout</param>
        /// <param name="endpoints">Endpoints to connect to</param>
        /// <returns>Client task</returns>
        public Task StartClient(TimeSpan validationTimeout, params IPEndPoint[] endpoints)
        {
            if (this.cancellationTokenSource != null)
            {
                SecureTransportEventSource.Log.StartClientFailed_AlreadyStarted(this.transportId);
                throw SecureTransportException.AlreadyStarted();
            }

            this.cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.rootCancellationToken);
            this.hasStarted.Reset();
            this.hasStopped.Reset();
            SecureTransportEventSource.Log.StartClient(this.transportId, endpoints.Length);
            var task = Task.Run(() => this.StartConnecting(endpoints, validationTimeout), this.cancellationTokenSource.Token);
            if (!this.hasStarted.Wait(DefaultStartTimeout))
            {
                SecureTransportEventSource.Log.StartTimedout(this.transportId);
                throw SecureTransportException.StartTimedout();
            }

            return task;
        }

        /// <summary>
        /// Starts server with default client SSL validation timeout of 5 seconds
        /// </summary>
        /// <param name="port">Port to start server</param>
        /// <returns>Server task</returns>
        public Task StartServer(int port)
        {
            return this.StartServer(port, TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// Starts server with a specified client SSL validation timeout
        /// </summary>
        /// <param name="port">Port to start server</param>
        /// <param name="validationTimeout">Client SSL validation timeout</param>
        /// <returns>Server task</returns>
        public Task StartServer(int port, TimeSpan validationTimeout)
        {
            var endpoint = new IPEndPoint(IPAddress.Any, port);
            return this.StartServer(endpoint, validationTimeout);
        }

        /// <summary>
        /// Starts server with a specified client SSL validation timeout
        /// </summary>
        /// <param name="endpoint">local endpoint start server</param>
        /// <param name="validationTimeout">Client SSL validation timeout</param>
        /// <returns>Server task</returns>
        public Task StartServer(IPEndPoint endpoint, TimeSpan validationTimeout)
        {
            if (this.cancellationTokenSource != null)
            {
                SecureTransportEventSource.Log.StartServerFailed_AlreadyStarted(this.transportId);
                throw SecureTransportException.AlreadyStarted();
            }

            this.cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.rootCancellationToken);
            this.hasStarted.Reset();
            this.hasStopped.Reset();
            SecureTransportEventSource.Log.StartServer(this.transportId, endpoint.ToString());
            var task = Task.Run(() => this.StartListening(endpoint, validationTimeout), this.cancellationTokenSource.Token);
            if (!this.hasStarted.Wait(DefaultStartTimeout))
            {
                SecureTransportEventSource.Log.StartTimedout(this.transportId);
                throw SecureTransportException.StartTimedout();
            }

            return task;
        }

        /// <summary>
        /// Stops the server with the default 5 seconds timeout
        /// </summary>
        public void Stop()
        {
            this.Stop(DefaultStopTimeout);
        }

        /// <summary>
        /// Stops the server with a specified timeout
        /// </summary>
        /// <param name="timeout">Timeout waiting for server stop</param>
        public void Stop(TimeSpan timeout)
        {
            if (this.cancellationTokenSource == null)
            {
                SecureTransportEventSource.Log.StopFailed_NotStarted(this.transportId);
                throw SecureTransportException.NotStarted();
            }

            this.StopAndCloseConnections();

            if (!this.hasStopped.Wait(timeout))
            {
                SecureTransportEventSource.Log.StopTimedout(this.transportId);
                throw SecureTransportException.StopTimedout();
            }

            this.cancellationTokenSource = null;
            SecureTransportEventSource.Log.Stopped(this.transportId);
        }

        /// <summary>
        /// Close this transport.
        /// </summary>
        public void Close()
        {
            SecureTransportEventSource.Log.SecureTransportClose(this.transportId);
            this.StopAndCloseConnections();
        }

        /// <summary>
        /// Dispose this client.
        /// </summary>
        public void Dispose()
        {
            this.Close();
            this.hasStopped.Dispose();
            this.hasStarted.Dispose();
            this.acceptConnectionsSemaphore.Dispose();
        }

        private static IPEndPoint GetRemoteEndPoint(TcpClient client)
        {
            return (IPEndPoint)client.Client.RemoteEndPoint;
        }

        private static string GetRemoteHostAddress(TcpClient client)
        {
            IPEndPoint remoteEndPoint = GetRemoteEndPoint(client);
            return remoteEndPoint.Address.ToString();
        }

        private async Task StartListening(IPEndPoint localEndpoint, TimeSpan validationTimeout)
        {
            this.hasStarted.Set();
            try
            {
                this.listener = new TcpListener(localEndpoint);
                this.listener.Start();

                int iteration = 0;
                int consecutiveFailureCount = 0;
                while ((!this.cancellationTokenSource.Token.IsCancellationRequested) && (consecutiveFailureCount < ConsecutiveAcceptFailuresLimit))
                {
                    bool mustReleaseSemaphore = false;
                    try
                    {
                        if (!await this.acceptConnectionsSemaphore.WaitAsync(validationTimeout, this.cancellationTokenSource.Token))
                        {
                            throw SecureTransportException.AcceptConnectionTimedout();
                        }

                        mustReleaseSemaphore = true;

                        iteration++;
                        Task<TcpClient> acceptTask = this.listener.AcceptTcpClientAsync();

                        using (var cancelSource = CancellationTokenSource.CreateLinkedTokenSource(this.cancellationTokenSource.Token))
                        {
                            // AcceptTcpClientAsync does not have an overload that takes a cancellationToken, so it
                            // does not return even if cancellation is in progress.  The following task will observe
                            // the cancellation token and go into the canceled state when cancellation is requested.
                            Task delayTask = Task.Delay(Timeout.InfiniteTimeSpan, cancelSource.Token);

                            await Task.WhenAny(acceptTask, delayTask);

                            if (acceptTask.IsCompleted)
                            {
                                cancelSource.Cancel();
                                TcpClient client = await acceptTask;
                                consecutiveFailureCount = 0;

                                mustReleaseSemaphore = false;
                                var ignoredTask = this.AcceptConnection(iteration, client, validationTimeout)
                                    .ContinueWith(t => this.acceptConnectionsSemaphore.Release());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        consecutiveFailureCount++;
                        SecureTransportEventSource.Log.AcceptTcpClientFailed(this.transportId, iteration, consecutiveFailureCount, ex.ToString());
                    }
                    finally
                    {
                        if (mustReleaseSemaphore)
                        {
                            this.acceptConnectionsSemaphore.Release();
                        }
                    }
                }

                this.listener.Stop();
            }
            finally
            {
                SecureTransportEventSource.Log.ListenerStopped(this.transportId);
                this.hasStopped.Set();
            }
        }

        private async Task StartConnecting(IPEndPoint[] endpoints, TimeSpan validationTimeout)
        {
            this.hasStarted.Set();
            try
            {
                int iteration = 0;
                while (!this.cancellationTokenSource.Token.IsCancellationRequested)
                {
                    iteration++;
                    var timer = Stopwatch.StartNew();
                    string host;
                    TcpClient client = null;
                    try
                    {
                        client = await this.EstablishConnection(iteration, endpoints, validationTimeout);
                        host = GetRemoteHostAddress(client);
                        var secureStream = await this.secureConnectionPolicy.AuthenticateAsClient(host, client, validationTimeout, this.cancellationTokenSource.Token);

                        string serverIdentity = string.Empty;
                        SslStream sslStream = secureStream as SslStream;
                        if (sslStream != null && sslStream.RemoteCertificate != null)
                        {
                            serverIdentity = sslStream.RemoteCertificate.Subject;
                        }

                        timer.Stop();
                        SecureTransportEventSource.Log.EstablishConnectionSucceeded(this.transportId, iteration, host, timer.ElapsedMilliseconds);
                        this.instrumentation.ConnectionEstablished(GetRemoteEndPoint(client), serverIdentity, timer.Elapsed);

                        // Lifetime of the client will be managed by the Connection object created in HandleConnection
                        TcpClient connectionClient = client;
                        client = null;
                        await this.HandleConnection(connectionClient, secureStream, serverIdentity);
                    }
                    catch (Exception ex)
                    {
                        if (timer.IsRunning)
                        {
                            timer.Stop();
                            SecureTransportEventSource.Log.EstablishConnectionFailed(
                                this.transportId,
                                iteration,
                                endpoints.Length,
                                ex.Message,
                                timer.ElapsedMilliseconds);
                            this.instrumentation.EstablishConnectionFailed(timer.Elapsed);
                        }
                    }
                    finally
                    {
                        if (client != null)
                        {
                            SecureTransportEventSource.Log.EstablishConnectionCloseUnsuccessfulConnection(this.transportId, iteration, timer.ElapsedMilliseconds);
                            client.Close();
                        }
                    }
                }
            }
            finally
            {
                this.hasStopped.Set();
            }
        }

        private async Task<TcpClient> EstablishConnection(int iteration, IPEndPoint[] endpoints, TimeSpan timeout)
        {
            using (var cancelEstablishConnection = CancellationTokenSource.CreateLinkedTokenSource(this.cancellationTokenSource.Token))
            {
                cancelEstablishConnection.CancelAfter(timeout);

                var connectionTasks = new List<Task<TcpClient>>();
                for (int i = 0; i < endpoints.Length; i++)
                {
                    connectionTasks.Add(this.EstablishConnection(iteration, endpoints[i], timeout, cancelEstablishConnection.Token));
                }

                TcpClient successfulClient = null;
                while (connectionTasks.Count > 0)
                {
                    var resolvedTask = await Task.WhenAny(connectionTasks.ToArray());
                    if (resolvedTask.IsCompleted && !resolvedTask.IsFaulted && !resolvedTask.IsCanceled)
                    {
                        cancelEstablishConnection.Cancel();
                        successfulClient = resolvedTask.Result;
                    }

                    connectionTasks.Remove(resolvedTask);
                }

                if (successfulClient != null)
                {
                    return successfulClient;
                }

                throw SecureTransportException.ConnectionFailed();
            }
        }

        private async Task<TcpClient> EstablishConnection(int iteration, IPEndPoint endpoint, TimeSpan timeout, CancellationToken cancellationToken)
        {
            TcpClient client = new TcpClient();

            // Disable the Nagle algorithm. When NoDelay is set to true, TcpClient does not wait
            // until it has collected a significant amount of outgoing data before sending a packet.
            // This ensures that requests are sent out to the server immediately and helps reduce latency.
            client.NoDelay = true;
            var timer = Stopwatch.StartNew();
            try
            {
                SecureTransportEventSource.Log.EstablishConnection(this.transportId, iteration, endpoint.ToString(), (long)timeout.TotalMilliseconds);
                Task connectTask = client.ConnectAsync(endpoint.Address, endpoint.Port);
                Task cancelTask = Task.Delay(timeout, cancellationToken);

                await Task.WhenAny(connectTask, cancelTask);

                if (connectTask.IsCompleted)
                {
                    await connectTask;
                    TcpClient connectedClient = client;
                    client = null;
                    SecureTransportEventSource.Log.ConnectSucceeded(this.transportId, iteration, endpoint.Address.ToString(), endpoint.Port, timer.ElapsedMilliseconds);
                    return connectedClient;
                }

                throw SecureTransportException.CancellationRequested($"Connection attempt to {endpoint} was cancelled");
            }
            catch (Exception ex)
            {
                SecureTransportEventSource.Log.ConnectFailed(this.transportId, iteration, endpoint.Address.ToString(), endpoint.Port, ex.Message, timer.ElapsedMilliseconds);
                throw;
            }
            finally
            {
                client?.Close();
            }
        }

        private async Task AcceptConnection(int iteration, TcpClient client, TimeSpan validationTimeout)
        {
            string remoteHostAddress = GetRemoteHostAddress(client);
            var timer = Stopwatch.StartNew();
            try
            {
                SecureTransportEventSource.Log.AcceptConnection(this.transportId, iteration, remoteHostAddress);
                var secureStream = await this.secureConnectionPolicy.AuthenticateAsServer(client, validationTimeout, this.cancellationTokenSource.Token);

                SslStream sslStream = secureStream as SslStream;
                string clientIdentity = string.Empty;
                if (sslStream != null && sslStream.RemoteCertificate != null)
                {
                    clientIdentity = sslStream.RemoteCertificate.Subject;
                }

                timer.Stop();
                SecureTransportEventSource.Log.AcceptConnectionSucceeded(this.transportId, iteration, remoteHostAddress, timer.ElapsedMilliseconds);
                this.instrumentation.ConnectionAccepted(GetRemoteEndPoint(client), clientIdentity, timer.Elapsed);

                // Lifetime of the client will be managed by the Connection object created in HandleConnection
                TcpClient connectionClient = client;
                client = null;
                await this.HandleConnection(connectionClient, secureStream, clientIdentity);
            }
            catch (Exception ex)
            {
                if (timer.IsRunning)
                {
                    timer.Stop();
                    this.instrumentation.AcceptConnectionFailed(GetRemoteEndPoint(client), timer.Elapsed);
                    SecureTransportEventSource.Log.AcceptConnectionFailed(this.transportId, iteration, remoteHostAddress, timer.ElapsedMilliseconds, ex.ToString());
                }
            }
            finally
            {
                if (client != null)
                {
                    SecureTransportEventSource.Log.AcceptConnectionCloseUnsuccessfulConnection(this.transportId, iteration, remoteHostAddress, timer.ElapsedMilliseconds);
                    client.Close();
                }
            }
        }

        private async Task HandleConnection(TcpClient client, Stream secureStream, string remoteIdentity)
        {
            string remoteHostAddress = GetRemoteHostAddress(client);
            long connectionId = Interlocked.Increment(ref this.lastAssignedConnectionId);
            var configuration = new Connection.Configuration
            {
                RemoteIdentity = remoteIdentity,
                MaxLifeSpan = this.MaxConnectionLifeSpan,
                MaxConnectionIdleTime = this.configuration.MaxConnectionIdleTime,
                SendBufferSize = this.configuration.SendBufferSize,
                ReceiveBufferSize = this.configuration.ReceiveBufferSize,
                SendQueueLength = this.configuration.SendQueueLength,
                MaxUnflushedPacketsCount = this.configuration.MaxUnflushedPacketsCount,
            };

            using (var connection = new Connection(this.transportId, connectionId, client, secureStream, configuration, this.cancellationTokenSource.Token))
            {
                if (!this.activeConnections.TryAdd(connectionId, connection))
                {
                    Debug.Assert(false, "Failed to add connection to the active connections dictionary");
                    throw SecureTransportException.Unexpected("Failed to add connection to the active connections dictionary");
                }

                try
                {
                    if (this.OnProtocolNegotiation != null)
                    {
                        connection.DoProtocolNegotiation = this.OnProtocolNegotiation;
                    }

                    connection.UseNetworkByteOrder = this.UseNetworkByteOrder;

                    await connection.Start(this.configuration.CommunicationProtocolVersion);
                    this.instrumentation.ConnectionCreated(connectionId, GetRemoteEndPoint(client), remoteIdentity);

                    if (this.OnNewConnection != null)
                    {
                        var timer = Stopwatch.StartNew();
                        this.OnNewConnection(connection);
                        SecureTransportEventSource.Log.OnNewConnection(this.transportId, connectionId, timer.ElapsedMilliseconds);
                    }

                    await connection.PullPackets();

                    if (this.OnConnectionLost != null)
                    {
                        var timer = Stopwatch.StartNew();
                        this.OnConnectionLost();
                        SecureTransportEventSource.Log.OnConnectionLost(this.transportId, connectionId, timer.ElapsedMilliseconds);
                    }
                }
                catch (Exception ex)
                {
                    SecureTransportEventSource.Log.HandleConnectionFailed(this.transportId, connectionId, ex.ToString());
                    throw;
                }
                finally
                {
                    this.instrumentation.ConnectionClosed(connectionId, GetRemoteEndPoint(client), remoteIdentity);

                    Connection removedConnection;
                    if (!this.activeConnections.TryRemove(connectionId, out removedConnection))
                    {
                        Debug.Assert(false, "Failed to remove connection from the active connections dictionary");
                        throw SecureTransportException.Unexpected("Failed to remove connection from the active connections dictionary");
                    }
                }
            }
        }

        private void StopAndCloseConnections()
        {
            SecureTransportEventSource.Log.SignallingStop(this.transportId, isListening: this.listener != null, activeConnections: this.activeConnections.Count);
            if (this.cancellationTokenSource != null)
            {
                this.cancellationTokenSource.Cancel();
            }

            this.hasStarted.Reset();

            // If this is a server, there will be an outstanding AcceptTcpClientAsync request
            // close the listener to cancel that request.
            if (this.listener != null)
            {
                this.listener.Stop();
            }

            foreach (var pair in this.activeConnections)
            {
                SecureTransportEventSource.Log.CloseActiveConnection(this.transportId, pair.Key);
                pair.Value.Close();
            }
        }

        /// <summary>
        /// Specification of a Subject + Thumbprint rule
        /// </summary>
        public class SubjectValidation
        {
            /// <summary>
            /// Gets or sets thumbprints (separated by ;) of the intermediate signing certificate
            /// </summary>
            public IReadOnlyList<string> SigningCertThumbprints { get; set; }

            /// <summary>
            /// Gets or sets subject of the certificate
            /// </summary>
            public string CertificateSubject { get; set; }
        }

        /// <summary>
        /// Specification of subject rule
        /// </summary>
        public sealed class SubjectRuleValidation
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="SubjectRuleValidation"/> class.
            /// </summary>
            /// <param name="role">Role to apply</param>
            /// <param name="subjectValidation">Subject plus thumbprint rule</param>
            public SubjectRuleValidation(AbstractCertificateRule.RoleToApply role, SecureTransport.SubjectValidation subjectValidation)
            {
                this.Role = role;
                this.SubjectValidation = subjectValidation;
            }

            /// <summary>
            /// Gets the role to apply
            /// </summary>
            public AbstractCertificateRule.RoleToApply Role { get; }

            /// <summary>
            /// Gets the subject validaton
            /// </summary>
            public SecureTransport.SubjectValidation SubjectValidation { get; }
        }

        /// <summary>
        /// Configuration of the <see cref="SecureTransport"/>
        /// </summary>
        public class Configuration
        {
            /// <summary>
            /// Gets or sets a value indicating whether secure connection must be used.
            /// </summary>
            public bool UseSecureConnection { get; set; } = false;

            /// <summary>
            /// Gets or sets the list of client certificates.
            /// </summary>
            public X509Certificate[] ClientCertificates { get; set; } = null;

            /// <summary>
            /// Gets or sets the list of server certificates.
            /// </summary>
            public X509Certificate[] ServerCertificates { get; set; } = null;

            /// <summary>
            /// Gets or sets the delegate responsible for validating the certificate supplied by the remote party.
            /// </summary>
            public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }

            /// <summary>
            /// Gets or sets the delegate responsible for selecting the certificate used for authentication.
            /// </summary>
            public LocalCertificateSelectionCallback LocalCertificateSelectionCallback { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the client is asked for a certificate for authentication.
            /// </summary>
            public bool IsClientCertificateRequired { get; set; } = true;

            /// <summary>
            /// Gets or sets a value indicating whether the certificate revocation list is checked during authentication.
            /// </summary>
            public bool MustCheckCertificateRevocation { get; set; } = false;

            /// <summary>
            /// Gets or sets a value indicating whether the certificate trust chain must be validated.
            /// </summary>
            public bool MustCheckCertificateTrustChain { get; set; } = true;

            /// <summary>
            /// Gets or sets the blacklisted thumbprints
            /// </summary>
            public IReadOnlyList<string> BlacklistedThumbprints { get; set; } = null;

            /// <summary>
            /// Gets or sets if present, the certificate validation rule to be used
            /// </summary>
            public AbstractCertificateRule ExplicitRule { get; set; } = null;

            /// <summary>
            /// Gets or sets if present identities are processed from here instead of from the members ClientCertificates and ServerCertificates
            /// </summary>
            public CertIdentities Identities { get; set; } = null;

            /// <summary>
            /// Gets or sets if present, it provides the subject validation rules
            /// </summary>
            public IReadOnlyList<SubjectRuleValidation> SubjectValidations { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether it should authenticate as client
            /// </summary>
            public bool AuthAsClient { get; set; }

            /// <summary>
            /// Gets or sets the protocol version to negotiate.
            /// </summary>
            public uint CommunicationProtocolVersion { get; set; } = 0;

            /// <summary>
            /// Gets or sets the maximum lifetime of a connection.
            /// </summary>
            public TimeSpan MaxConnectionLifespan { get; set; } = TimeSpan.FromDays(1);

            /// <summary>
            /// Gets or sets the maximum timespan a connection can remain idle without receiving any packets.
            /// </summary>
            public TimeSpan MaxConnectionIdleTime { get; set; } = TimeSpan.FromDays(1);

            /// <summary>
            /// Gets or sets the send buffer size.
            /// </summary>
            /// <remarks>
            /// The network buffer size should be at least as large as the size of a typical packet
            /// that will be sent through this transport to ensure that the packet will be stored and sent
            /// in one operation.  The default value is 8192 which is the same as the default value for
            /// TcpClient.SendBufferSize.
            /// </remarks>
            public int SendBufferSize { get; set; } = 8192;

            /// <summary>
            /// Gets or sets the receive buffer size.
            /// </summary>
            /// /// <remarks>
            /// The network buffer size should be at least as large as the size of a typical packet
            /// that will be received by this transport to ensure that the packet will be available
            /// when read is performed.  The default value is 8192 which is the same as the default value for
            /// TcpClient.ReceiveBufferSize.
            /// </remarks>
            public int ReceiveBufferSize { get; set; } = 8192;

            /// <summary>
            /// Gets or sets the send queue length.
            /// </summary>
            public int SendQueueLength { get; set; } = 10000;

            /// <summary>
            /// Gets or sets the maximum number of packets that are allowed to accumulate
            /// before the send buffer is flushed.
            /// </summary>
            public int MaxUnflushedPacketsCount { get; set; } = 10000;

            /// <summary>
            /// Gets or sets the maximum number of connections that can be established at the same time.
            /// </summary>
            public int MaxConnections { get; set; } = 1000;
        }

        private sealed class NoInstrumentation : ISecureTransportInstrumentation
        {
            public static NoInstrumentation Instance { get; } = new NoInstrumentation();

            public void AcceptConnectionFailed(IPEndPoint clientEndPoint, TimeSpan processingTime)
            {
            }

            public void ConnectionAccepted(IPEndPoint clientEndPoint, string clientIdentity, TimeSpan setupTime)
            {
            }

            public void ConnectionCreated(long connectionId, IPEndPoint remoteEndPoint, string remoteIdentity)
            {
            }

            public void ConnectionClosed(long connectionId, IPEndPoint remoteEndPoint, string remoteIdentity)
            {
            }

            public void ConnectionEstablished(IPEndPoint serverEndPoint, string serverIdentity, TimeSpan setupTime)
            {
            }

            public void EstablishConnectionFailed(TimeSpan processingTime)
            {
            }
        }
    }
}
