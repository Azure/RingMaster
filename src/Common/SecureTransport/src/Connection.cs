// <copyright file="Connection.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Represents a connection established by the <see cref="SecureTransport"/>.
    /// </summary>
    internal class Connection : IConnection
    {
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
        /// Initializes a new instance of the <see cref="Connection"/> class.
        /// </summary>
        /// <param name="transportId">Unique Id of the transport that created this connection</param>
        /// <param name="connectionId">Unique Id of the connection</param>
        /// <param name="client">Socket connection with the remote client</param>
        /// <param name="secureStream">Stream that represents data in the connection</param>
        /// <param name="configuration">Configuration parameters</param>
        /// <param name="cancellationToken">Cancellation token that will be observed by this connection</param>
        public Connection(long transportId, long connectionId, TcpClient client, Stream secureStream, Configuration configuration, CancellationToken cancellationToken)
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

            // Disable the Nagle algorithm.  When NoDelay is set to true,
            // TcpClient does not wait until it has connected a significant
            // amount of outgoing data before sending a packet.
            // This ensures that requests are sent out to the server immediately
            // and helps reduce latency.
            this.client.NoDelay = true;
            this.client.ReceiveBufferSize = this.configuration.ReceiveBufferSize;
            this.client.SendBufferSize = this.configuration.SendBufferSize;

            // Outgoing packets is the queue of packets that have not been sent yet. The semaphore
            // outgoingPacketsAvailable is signalled when a packet is queued. This wakes up the
            // PushPackets task which actually sends the packet to the other side.
            this.outgoingPackets = new BlockingCollection<Packet>(configuration.SendQueueLength);
            this.outgoingPacketsAvailable = new SemaphoreSlim(0, configuration.SendQueueLength);

            this.RemoteIdentity = configuration.RemoteIdentity;
        }

        /// <summary>
        /// Gets the unique id of this connection.
        /// </summary>
        public ulong Id
        {
            get { return (ulong)this.connectionId; }
        }

        /// <summary>
        /// Gets the remote endpoint of this connection.
        /// </summary>
        public EndPoint RemoteEndPoint
        {
            get
            {
                return this.client.Client.RemoteEndPoint;
            }
        }

        /// <summary>
        /// Gets the identity of the remote endpoint if mutual authentication was used
        /// </summary>
        public string RemoteIdentity { get; private set; }

        /// <summary>
        /// Gets the negotiated protocol version.
        /// </summary>
        public uint ProtocolVersion { get; private set; }

        /// <summary>
        /// Gets or sets the callback that must be invoked whenever a packet is received.
        /// </summary>
        public Action<byte[]> OnPacketReceived { get; set; }

        /// <summary>
        /// Gets or sets the callback that should be invoked if the incoming packat is not in the standard
        /// RingMaster format of length + data.
        /// </summary>
        public PacketReceiveDelegate DoPacketReceive { get; set; }

        /// <summary>
        /// Gets or sets the callback that should be invoked fro protocol negotiation. This allows protocols like Zookeeper to
        /// supply their own which is different than the ringmaster protocol's negotiation.
        /// </summary>
        public ProtocolNegotiatorDelegate DoProtocolNegotiation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether tells connection to use Network byte order to send data.
        /// </summary>
        public bool UseNetworkByteOrder { get; set; } = false;

        /// <summary>
        /// Gets or sets the callback that must be invoked when this connection is lost.
        /// </summary>
        public Action OnConnectionLost { get; set; }

        /// <summary>
        /// Start the connection.
        /// </summary>
        /// <param name="protocolVersion">Protocol version to negotiate with the other side</param>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        public async Task Start(uint protocolVersion)
        {
            this.ProtocolVersion = await this.DoProtocolNegotiation(protocolVersion);
            this.pushPacketsTask = Task.Run(this.PushPackets);
        }

        /// <summary>
        /// Send a packet to the other side of the connection.
        /// </summary>
        /// <param name="data">Data to send</param>
        public void Send(byte[] data)
        {
            Task notInUse = this.SendAsync(data);
        }

        /// <summary>
        /// Send a packet to the other side of the connection asynchronously.
        /// </summary>
        /// <param name="data">Data to send</param>
        /// <returns>A <see cref="Task"/> that tracks completion of the send operation</returns>
        public Task SendAsync(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            // Do not send the packet if the outgoing packets queue
            // is marked complete for adding.
            if (this.disposed || this.outgoingPackets.IsAddingCompleted)
            {
                return Task.FromResult<object>(null);
            }

            var packet = default(Packet);
            packet.Id = Interlocked.Increment(ref this.nextPacketId);
            packet.Data = data;
            packet.CompletionSource = new TaskCompletionSource<object>();

            SecureTransportEventSource.Log.Send(this.transportId, this.connectionId, packet.Id, packet.Data.Length);
            if (this.outgoingPackets.TryAdd(packet))
            {
                // Signal that a packet is avaliable to send. This wakes up the PushPackets task
                // which actually sends the packet to the other side.
                this.outgoingPacketsAvailable.Release();
            }
            else
            {
                SecureTransportEventSource.Log.SendQueueFull(this.transportId, this.connectionId, packet.Id, packet.Data.Length);
                throw SecureTransportException.SendQueueFull();
            }

            return packet.CompletionSource.Task;
        }

        /// <summary>
        /// Disconnect this connection.
        /// </summary>
        public void Disconnect()
        {
            SecureTransportEventSource.Log.Disconnect(this.transportId, this.connectionId);
            lock (this)
            {
                if (!this.isDisconnected)
                {
                    this.outgoingPackets.CompleteAdding();
                    this.cancellationTokenSource.Cancel();
                    this.client.Client.Disconnect(reuseSocket: false);
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
            this.pushPacketsTask?.Wait();
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
                        this.OnPacketReceived(packet);
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
                    if (await this.outgoingPacketsAvailable.WaitAsync(timeToWait, this.cancellationToken))
                    {
                        Packet packet = this.outgoingPackets.Take(this.cancellationToken);
                        writer.Write(this.UseNetworkByteOrder ? System.Net.IPAddress.HostToNetworkOrder((int)packet.Data.Length) : (int)packet.Data.Length);
                        writer.Write(packet.Data);
                        unflushedPacketsCount++;

                        packet.CompletionSource.SetResult(null);

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
                this.secureStream.WriteAsync(BitConverter.GetBytes(localProtocolVersion), 0, sizeof(int)).ContinueWith(_ => this.secureStream.FlushAsync()),
                this.secureStream.ReadAsync(versionBytes, 0, sizeof(int)));

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
            public byte[] Data;
            public TaskCompletionSource<object> CompletionSource;
        }
    }
}
