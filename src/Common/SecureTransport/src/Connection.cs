// <copyright file="Connection.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication;

    /// <summary>
    /// Represents a connection established by the <see cref="SecureTransport"/>.
    /// </summary>
    internal class Connection : IConnection
    {
        /// <summary>
        /// The send queue full maximum last time before we kill the process.
        /// </summary>
        private static readonly TimeSpan SendQueueFullMaxLastTime = TimeSpan.FromMinutes(10);

        /// <summary>
        /// A last successful time when any client was able to successfully send the data the the clients.
        /// It is used to detect that the current instance is completely disconnected from the world and crash to help Service Fabric to elect a new leader.
        /// </summary>
        private static DateTime lastSuccessfulAddToSendQueueTime = DateTime.UtcNow;

        /// <summary>
        /// Socket connection with the remote client.
        /// </summary>
        private readonly TcpClient client;

        /// <summary>
        /// Stream that represents data in the connection.
        /// </summary>
        private readonly Stream secureStream;

        /// <summary>
        /// <see cref="CancellationTokenSource"/> that can be used to cancel this connection.
        /// </summary>
        private readonly CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// <see cref="CancellationToken"/> that is observed by this connection.
        /// </summary>
        private readonly CancellationToken cancellationToken;

        /// <summary>
        /// UniqueId of the transport that created this connection.
        /// </summary>
        private readonly long transportId;

        /// <summary>
        /// Unique Id of the connection.
        /// </summary>
        private readonly long connectionId;

        /// <summary>
        /// Configuration settings.
        /// </summary>
        private readonly Configuration configuration;

        /// <summary>
        /// Queue of outgoing requests.
        /// </summary>
        private readonly BlockingCollection<Packet> outgoingPackets;

        /// <summary>
        /// Semaphore used to signal that outgoing requests are available.
        /// </summary>
        private readonly SemaphoreSlim outgoingPacketsAvailable;

        /// <summary>
        /// A Stopwatch that is reset whenever a packet is received by this connection.
        /// </summary>
        private readonly Stopwatch timeSinceLastActivity = Stopwatch.StartNew();

        private readonly ISecureTransportInstrumentation instrumentation;

        /// <summary>
        /// Task that pushes packets to the other side of the connection.
        /// </summary>
        private Task pushPacketsTask;

        /// <summary>
        /// Id that will be assigned to the next packet that is queued for send.
        /// </summary>
        private long nextPacketId = 0;

        /// <summary>
        /// This is <c>true</c> once connection is disconnected.
        /// </summary>
        private bool isDisconnected = false;

        /// <summary>
        /// If this object has been disposed
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Connection" /> class.
        /// </summary>
        /// <param name="transportId">Unique Id of the transport that created this connection</param>
        /// <param name="connectionId">Unique Id of the connection</param>
        /// <param name="client">Socket connection with the remote client</param>
        /// <param name="secureStream">Stream that represents data in the connection</param>
        /// <param name="configuration">Configuration parameters</param>
        /// <param name="cancellationToken">Cancellation token that will be observed by this connection</param>
        /// <param name="instrumentation">The instrumentation.</param>
        public Connection(long transportId, long connectionId, TcpClient client, Stream secureStream, Configuration configuration, CancellationToken cancellationToken, ISecureTransportInstrumentation instrumentation)
        {
            this.transportId = transportId;
            this.connectionId = connectionId;
            this.client = client;
            this.secureStream = secureStream;
            this.cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            this.cancellationToken = this.cancellationTokenSource.Token;
            this.configuration = configuration;
            this.DoPacketReceive = this.ReceivePacket;
            this.DoProtocolNegotiation = this.NegotiateProtocol;

            if (this.configuration.MaxLifeSpan < TimeSpan.MaxValue)
            {
                SecureTransportEventSource.Log.SetConnectionLifetimeLimit(transportId, connectionId, (long)configuration.MaxLifeSpan.TotalMilliseconds);
            }

            if (this.configuration.MaxConnectionIdleTime < TimeSpan.MaxValue)
            {
                SecureTransportEventSource.Log.SetConnectionIdleTimeLimit(transportId, connectionId, (long)configuration.MaxConnectionIdleTime.TotalMilliseconds);
            }

            this.client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            // Discard any pending data when the connection is closed.
            this.client.LingerState = new LingerOption(true, 0);

            // Disable the Nagle algorithm.  When NoDelay is set to true,
            // TcpClient does not wait until it has connected a significant
            // amount of outgoing data before sending a packet.
            // This ensures that requests are sent out to the server immediately
            // and helps reduce latency.
            this.client.NoDelay = true;
            this.client.ReceiveBufferSize = this.configuration.ReceiveBufferSize;
            this.client.SendBufferSize = this.configuration.SendBufferSize;

            // Outgoing packets is the queue of packets that have not been sent yet. The semaphore
            // outgoingPacketsAvailable is signaled when a packet is queued. This wakes up the
            // PushPackets task which actually sends the packet to the other side.
            this.outgoingPackets = new BlockingCollection<Packet>(configuration.SendQueueLength);
            this.outgoingPacketsAvailable = new SemaphoreSlim(0, configuration.SendQueueLength);

            this.RemoteIdentity = configuration.RemoteIdentity;
            this.instrumentation = instrumentation;
        }

        /// <inheritdoc />
        public ulong Id => (ulong)this.connectionId;

        /// <inheritdoc />
        public EndPoint RemoteEndPoint => this.client.Client.RemoteEndPoint;

        /// <inheritdoc />
        public string RemoteIdentity { get; }

        /// <inheritdoc />
        public uint ProtocolVersion { get; private set; }

        /// <inheritdoc />
        public Func<IMemoryBuffer, Task> OnPacketReceived { get; set; }

        /// <inheritdoc />
        public PacketReceiveDelegate DoPacketReceive { get; set; }

        /// <inheritdoc />
        public ProtocolNegotiatorDelegate DoProtocolNegotiation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether tells connection to use Network byte order to send data.
        /// </summary>
        public bool UseNetworkByteOrder { get; set; } = false;

        /// <inheritdoc />
        public Action OnConnectionLost { get; set; }

        /// <summary>
        /// Start the connection.
        /// </summary>
        /// <param name="protocolVersion">Protocol version to negotiate with the other side</param>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        public async Task Start(uint protocolVersion)
        {
            this.ProtocolVersion = await this.DoProtocolNegotiation(protocolVersion).ConfigureAwait(false);
            this.pushPacketsTask = Task.Run(this.PushPackets);
        }

        /// <inheritdoc />
        public void Send(IMemoryBuffer data)
        {
            data.ThrowIfNull();

            // Send is best-effort unless the outgoing queue stays full long enough
            // that the caller must tear down the connection.
            _ = this.SendAsync(data);
        }

        /// <inheritdoc />
        public Task SendAsync(IMemoryBuffer data)
        {
            const int AddingPacketTimeoutMs = 5000;

            data.ThrowIfNull();

            // Do not send the packet if the outgoing packets queue
            // is marked complete for adding.
            if (this.disposed || this.outgoingPackets.IsAddingCompleted)
            {
                return Task.CompletedTask;
            }

            var packet = default(Packet);
            packet.Id = Interlocked.Increment(ref this.nextPacketId);
            packet.Data = data;
            packet.CompletionSource = new TaskCompletionSource<object>();
            var packetLength = packet.Data.GetBuffer().Length;

            SecureTransportEventSource.Log.Send(this.transportId, this.connectionId, packet.Id, packetLength);
            if (this.outgoingPackets.TryAdd(packet, AddingPacketTimeoutMs))
            {
                // Signal that a packet is available to send. This wakes up the PushPackets task
                // which actually sends the packet to the other side.
                this.instrumentation.OutgoingPacketQueued(this.transportId, this.connectionId, this.outgoingPackets.Count, packetLength);
                this.outgoingPacketsAvailable.Release();
            }
            else
            {
                if (this.outgoingPackets.IsAddingCompleted)
                {
                    // We have a race condition here: even though we checked IsAddingCompleted before,
                    // its possible that the flag was set between the check and the call to outgoingPackets.TryAdd.
                    // In this case we still can't do anything but return a completed task.
                    SecureTransportEventSource.Log.SendIsSkippedDueToCompletion(this.transportId, this.connectionId, packetId: packet.Id);
                    packet.Data.Dispose();
                    return Task.CompletedTask;
                }

                int count = this.outgoingPackets.Count;
                this.instrumentation.OutgoingQueueFull(this.transportId, this.connectionId, count);
                SecureTransportEventSource.Log.SendQueueFull(this.transportId, this.connectionId, packet.Id, packetLength, count);

                // We probably completely lost the network. Failing fast allowing Service Fabric to fail over to another instance.
                var timeSinceLastSuccessfulMessageDelivery = DateTime.UtcNow - lastSuccessfulAddToSendQueueTime;
                if (timeSinceLastSuccessfulMessageDelivery > SendQueueFullMaxLastTime)
                {
                    Environment.FailFast(
                         $"Send queue full lasted for more than {SendQueueFullMaxLastTime}. Time since the last successful delivery: {timeSinceLastSuccessfulMessageDelivery}. " +
                                $"Now: {DateTime.UtcNow}, {nameof(lastSuccessfulAddToSendQueueTime)}: {lastSuccessfulAddToSendQueueTime}. Remote Endpoint: {this.RemoteEndPoint}" +
                                $" Connection Id: {this.connectionId}, Transport Id: {this.transportId}");
                }

                packet.Data.Dispose();
                throw SecureTransportException.SendQueueFull(outgoingPacketsCount: count);
            }

            return packet.CompletionSource.Task;
        }

        /// <inheritdoc />
        public void Disconnect()
        {
            SecureTransportEventSource.Log.Disconnect(this.transportId, this.connectionId);
            lock (this)
            {
                if (!this.isDisconnected)
                {
                    this.outgoingPackets.CompleteAdding();
                    this.cancellationTokenSource.Cancel();
                    try
                    {
                        this.client.Client.Shutdown(SocketShutdown.Both);
                    }
                    catch (Exception ex)
                    {
                        SecureTransportEventSource.Log.HandleConnectionFailed(this.transportId, this.connectionId, ex.ToString());
                    }

                    try
                    {
                        this.client.Client.Disconnect(reuseSocket: false);
                    }
                    catch (Exception ex)
                    {
                        SecureTransportEventSource.Log.HandleConnectionFailed(this.transportId, this.connectionId, ex.ToString());
                    }

                    this.isDisconnected = true;
                }
            }
        }

        /// <summary>
        /// Close this connection.
        /// </summary>
        public void Close()
        {
            SecureTransportEventSource.Log.ConnectionClose(this.transportId, this.connectionId);
            this.Disconnect();
            this.secureStream.Close();
            this.client.Close();
            this.pushPacketsTask?.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Dispose this client.
        /// </summary>
        public void Dispose()
        {
            if (!this.disposed)
            {
                this.disposed = true;

                this.Close();
                this.outgoingPackets.Dispose();
                this.outgoingPacketsAvailable.Dispose();
                this.pushPacketsTask?.Dispose();

                // Disposing the cancellation token source to avoid a memory leak, since otherwise
                // the linked source will be kept in the registration list forever.
                this.cancellationTokenSource?.Dispose();
            }
        }

        /// <summary>
        /// Repeatedly consume packets that are sent by the remote client.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        internal async Task PullPackets()
        {
            try
            {
                // The size of the receive buffer is configurable. A bigger buffer improves throughput, but could affect latency
                // because requests could be pending in the buffer for some time before being processed. The application that uses
                // SecureTransport must configure the buffer size that achieves the best balance of throughput and latency for its
                // scenario.  The default buffer size should be reasonable for most applications.
                var bufferedStream = (this.configuration.ReceiveBufferSize > 0)
                    ? new BufferedStream(this.secureStream, this.configuration.ReceiveBufferSize)
                    : this.secureStream;

                while (!this.cancellationToken.IsCancellationRequested)
                {
                    byte[] packet = await this.DoPacketReceive(bufferedStream).ConfigureAwait(false);

                    if (packet == null)
                    {
                        SecureTransportEventSource.Log.PullPacketsCompleted(this.transportId, this.connectionId);
                        break;
                    }

                    // Every time a packet is received from the other side, reset the timeSinceLastActivity stopwatch
                    // to indicate that this connection is active.
                    this.timeSinceLastActivity.Restart();

                    SecureTransportEventSource.Log.OnPacketReceived(this.transportId, this.connectionId, packet.Length);
                    if (this.OnPacketReceived != null)
                    {
                        await this.OnPacketReceived(new ByteArrayBackedBuffer(packet)).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                SecureTransportEventSource.Log.PullPacketsFailed(this.transportId, this.connectionId, ex.Message);
            }

            if (this.OnConnectionLost != null)
            {
                var timer = Stopwatch.StartNew();
                this.OnConnectionLost();
                SecureTransportEventSource.Log.OnConnectionLostNotificationCompleted(this.transportId, this.connectionId, timer.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Monitor the outgoing packets queue and push packets in the queue to the client.
        /// </summary>
        /// <returns>A Task that tracks execution of this method</returns>
        private async Task PushPackets()
        {
            try
            {
                var lifeTime = Stopwatch.StartNew();

                // The size of the send buffer is configurable. A bigger buffer improves throughput, but could affect latency
                // because requests could be pending in the buffer for some time before being sent. The application that uses
                // SecureTransport must configure the buffer size that achieves the best balance of throughput and latency for its
                // scenario.  The default buffer size should be reasonable for most applications.
                var bufferedStream = (this.configuration.SendBufferSize > 0)
                    ? new BufferedStream(this.secureStream, this.configuration.SendBufferSize)
                    : this.secureStream;

                var writer = new BinaryWriter(bufferedStream);
                int unflushedPacketsCount = 0;
                while (!this.cancellationToken.IsCancellationRequested)
                {
                    TimeSpan lifetimeElapsed = lifeTime.Elapsed;
                    TimeSpan timeToWait = this.configuration.MaxLifeSpan;

                    // Wait for no more than the remaining life span of this connection.
                    if (this.configuration.MaxLifeSpan < TimeSpan.MaxValue)
                    {
                        timeToWait = (lifetimeElapsed < this.configuration.MaxLifeSpan) ? this.configuration.MaxLifeSpan - lifetimeElapsed : TimeSpan.Zero;
                    }

                    // If the maximum time that this connection is allowed to be idle is lesser than the remaining life span of this
                    // connection then wait for the lower time span.
                    if (this.configuration.MaxConnectionIdleTime < timeToWait)
                    {
                        timeToWait = this.configuration.MaxConnectionIdleTime;
                    }

                    // If there are lingering packets in the stream, wait for zero time for the next packet
                    // and flush if the timeout expires.  If there are no lingering packets, then wait until a packet is
                    // available.
                    timeToWait = (unflushedPacketsCount > 0) ? TimeSpan.Zero : timeToWait;

                    if (timeToWait == TimeSpan.MaxValue)
                    {
                        timeToWait = Timeout.InfiniteTimeSpan;
                    }

                    // As long as there are packets available to send, keep adding them to the stream, otherwise
                    // flush the stream.
                    if (await this.outgoingPacketsAvailable.WaitAsync(timeToWait, this.cancellationToken).ConfigureAwait(false))
                    {
                        Packet packet = this.outgoingPackets.Take(this.cancellationToken);
                        try
                        {
                            this.instrumentation.OutgoingPacketSent(this.transportId, this.connectionId, packet.Data.Length);

                            writer.Write(this.UseNetworkByteOrder ? System.Net.IPAddress.HostToNetworkOrder(packet.Data.Length) : packet.Data.Length);
                            writer.Write(packet.Data.GetBuffer(), 0, packet.Data.Length);
                            unflushedPacketsCount++;

                            // We can assume that we sent the data successfully, even though technically we could've just added them to the output buffer.
                            lastSuccessfulAddToSendQueueTime = DateTime.UtcNow;

                            // The current design is tricky, since we have a buffering stream, so we don't really know when the message is delivered or not.
                            // For instance, when we write we could have caused the flush, or not depending on the SendBufferSize configuration.

                            // We should do the following:
                            // 1. Call packet.CompletionSource.SetException in the catch block.
                            // 2. Flush on timer
                            // 3. Somehow track if the packet was flushed or not to mark the packets completed **only** when they were actually delivered.
                            packet.CompletionSource.TrySetResult(null);
                        }
                        finally
                        {
                            packet.Data.Dispose();
                        }

                        if (unflushedPacketsCount > this.configuration.MaxUnflushedPacketsCount)
                        {
                            writer.Flush();
                            unflushedPacketsCount = 0;
                        }
                    }
                    else
                    {
                        writer.Flush();
                        unflushedPacketsCount = 0;
                    }

                    if (this.timeSinceLastActivity.Elapsed > this.configuration.MaxConnectionIdleTime)
                    {
                        SecureTransportEventSource.Log.ConnectionIdleTimeLimitExpired(this.transportId, this.connectionId, this.timeSinceLastActivity.ElapsedMilliseconds);
                        break;
                    }

                    if (lifetimeElapsed > this.configuration.MaxLifeSpan)
                    {
                        SecureTransportEventSource.Log.ConnectionLifetimeLimitExpired(this.transportId, this.connectionId, (long)lifetimeElapsed.TotalMilliseconds);
                        break;
                    }
                }

                SecureTransportEventSource.Log.PushPacketsCompleted(this.transportId, this.connectionId);
            }
            catch (OperationCanceledException)
            {
                SecureTransportEventSource.Log.PushPacketsCompleted(this.transportId, this.connectionId);
            }
            catch (Exception ex)
            {
                SecureTransportEventSource.Log.PushPacketsFailed(this.transportId, this.connectionId, ex.ToString());
            }

            this.Disconnect();
        }

        /// <summary>
        /// Negotiate the protocol version to be used for communication.
        /// </summary>
        /// <param name="localProtocolVersion">Maximum protocol version supported by this transport</param>
        /// <returns>A <see cref="Task"/> that resolves to the negotiated protocol version</returns>
        private async Task<uint> NegotiateProtocol(uint localProtocolVersion)
        {
            byte[] versionBytes = new byte[sizeof(int)];

            await Task.WhenAll(
                this.secureStream.WriteAsync(BitConverter.GetBytes(localProtocolVersion), 0, sizeof(int), this.cancellationToken).ContinueWith(_ => this.secureStream.FlushAsync(this.cancellationToken)),
                this.secureStream.ReadAsync(versionBytes, 0, sizeof(int), this.cancellationToken))
                .ConfigureAwait(false);

            uint remoteProtocolVersion = BitConverter.ToUInt32(versionBytes, 0);

            uint acceptedProtocolVersion = Math.Min(localProtocolVersion, remoteProtocolVersion);
            SecureTransportEventSource.Log.NegotiateProtocol(this.transportId, this.connectionId, localProtocolVersion, remoteProtocolVersion, acceptedProtocolVersion);

            return acceptedProtocolVersion;
        }

        /// <summary>
        /// Receive a packet from the client.
        /// </summary>
        /// <param name="stream">Stream from which the packet must be read</param>
        /// <returns>The packet that was received or null if there are no more packets</returns>
        private async Task<byte[]> ReceivePacket(Stream stream)
        {
            byte[] packetLengthBytes = await this.ReadBytes(stream, 4).ConfigureAwait(false);
            if (packetLengthBytes != null)
            {
                int packetLength = BitConverter.ToInt32(packetLengthBytes, 0);

                return await this.ReadBytes(stream, packetLength).ConfigureAwait(false);
            }

            return null;
        }

        /// <summary>
        /// Read the specified number of bytes from the stream that represents the connection.
        /// </summary>
        /// <param name="stream">Stream from which the bytes must be read</param>
        /// <param name="length">Number of bytes to read</param>
        /// <returns>The data that was read or null if there is no more data</returns>
        private async Task<byte[]> ReadBytes(Stream stream, int length)
        {
            int totalRead = 0;
            int bytesRemaining = length;
            byte[] buffer = new byte[length];

            while (bytesRemaining > 0)
            {
                // ReadAsync could return 0 if end of stream has been reached.
                int bytesRead = await stream.ReadAsync(buffer, totalRead, bytesRemaining, this.cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    break;
                }

                totalRead += bytesRead;
                bytesRemaining -= bytesRead;
            }

            if (totalRead == buffer.Length)
            {
                return buffer;
            }

            return null;
        }

        /// <summary>
        /// Configuration of secure transport connection
        /// </summary>
        internal struct Configuration
        {
            /// <summary>
            /// Transport ID
            /// </summary>
            public long TransportId;

            /// <summary>
            /// Remote identity
            /// </summary>
            public string RemoteIdentity;

            /// <summary>
            /// Max life span of the connection
            /// </summary>
            public TimeSpan MaxLifeSpan;

            /// <summary>
            /// Max connection idle time
            /// </summary>
            public TimeSpan MaxConnectionIdleTime;

            /// <summary>
            /// Size of the send buffer
            /// </summary>
            public int SendBufferSize;

            /// <summary>
            /// Size of the receive buffer
            /// </summary>
            public int ReceiveBufferSize;

            /// <summary>
            /// Length of the send queue
            /// </summary>
            public int SendQueueLength;

            /// <summary>
            /// Count of max unflushed packets
            /// </summary>
            public int MaxUnflushedPacketsCount;
        }

        private struct Packet
        {
            public long Id;
            public IMemoryBuffer Data;
            public TaskCompletionSource<object> CompletionSource;
        }
    }
}
