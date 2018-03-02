// <copyright file="SimpleTransport.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Test
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol;

    /// <summary>
    /// SimpleTransport is an implementation of the <see cref="ITransport"/> interface
    /// for use in tests. It provides communication between two endpoints that are in
    /// the same process.
    /// </summary>
    public class SimpleTransport : ITransport
    {
        private static long lastAssignedTransportId;
        private static long lastAssignedConnectionId;

        private readonly List<Connection> connections = new List<Connection>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleTransport"/> class.
        /// </summary>
        public SimpleTransport()
        {
            this.Id = Interlocked.Increment(ref lastAssignedTransportId);
        }

        /// <summary>
        /// Gets the unique id of this transport.
        /// </summary>
        public long Id { get; private set; }

        /// <summary>
        /// Gets or sets the callback that must be invoked whenever a new connection is established.
        /// </summary>
        public Action<IConnection> OnNewConnection { get; set; }

        /// <summary>
        /// Gets or sets the callback that must be invoked whenever a connection is lost.
        /// </summary>
        public Action OnConnectionLost { get; set; }

        /// <summary>
        /// Gets or sets the callback that must be invoked for protocol negotiation
        /// </summary>
        public ProtocolNegotiatorDelegate OnProtocolNegotiation { get; set; }

        /// <summary>
        /// Tells the connection to use Network byte order
        /// </summary>
        public bool UseNetworkByteOrder { get; set; } = false;

        /// <summary>
        /// Connect to the given <see cref="SimpleTransport"/>.
        /// </summary>
        /// <param name="otherTransport">Transport to connect with</param>
        public void Connect(SimpleTransport otherTransport)
        {
            if (otherTransport == null)
            {
                throw new ArgumentNullException(nameof(otherTransport));
            }

            var thisConnection = this.CreateConnection();
            var otherConnection = otherTransport.CreateConnection();

            thisConnection.ConnectTo(otherConnection);
            otherConnection.ConnectTo(thisConnection);

            this.OnNewConnection?.Invoke(thisConnection);
            otherTransport.OnNewConnection?.Invoke(otherConnection);

            thisConnection.Start();
            otherConnection.Start();

            Trace.TraceInformation($"SimpleTransport.Connect transportId={this.Id}, otherTransportId={otherTransport.Id}");
        }

        /// <summary>
        /// Close the transport
        /// </summary>
        public void Close()
        {
            Trace.TraceInformation($"SimpleTransport.Close transportId={this.Id}, connections={connections.Count}");
            while (connections.Count > 0)
            {
                var connection = connections[0];
                connection.Close();
            }
        }

        private Connection CreateConnection()
        {
            Connection connection = null;
            try
            {
                ulong id = (ulong)Interlocked.Increment(ref lastAssignedConnectionId);
                connection = new Connection(id, this);
                lock (connections)
                {
                    this.connections.Add(connection);
                }

                Trace.TraceInformation($"SimpleTransport.CreateConnection transportId={this.Id}, connectionId={connection.Id}");

                Connection temp = connection;
                connection = null;
                return temp;
            }
            finally
            {
                connection?.Dispose();
            }
        }

        private void RemoveConnection(Connection connection)
        {
            Trace.TraceInformation($"SimpleTransport.RemoveConnection transportId={this.Id}, connectionId={connection.Id}");
            lock (connections)
            {
                this.connections.Remove(connection);
            }
        }

        public void Dispose()
        {
            this.Close();
            GC.SuppressFinalize(this);
        }

        private class Connection : IConnection
        {
            private readonly SimpleTransport transport;
            private readonly BlockingCollection<Packet> packets = new BlockingCollection<Packet>();
            private readonly SemaphoreSlim packetsAvailable = new SemaphoreSlim(0);
            private readonly CancellationTokenSource cancellationSource;

            private Connection otherEnd;
            private Task connectionLifetimeTask;
            private long lastAssignedPacketId;
            private bool isDisconnected;

            public Connection(ulong id, SimpleTransport transport)
            {
                this.Id = id;
                this.transport = transport;
                this.cancellationSource = new CancellationTokenSource();
            }

            public Action OnConnectionLost { get; set; }

            public Action<byte[]> OnPacketReceived { get; set; }

            public ulong Id { get; private set; }

            public EndPoint RemoteEndPoint { get; private set; }

            public string RemoteIdentity
            {
                get { return this.RemoteEndPoint.ToString(); }
            }

            public uint ProtocolVersion
            {
                get
                {
                    return RingMasterCommunicationProtocol.MaximumSupportedVersion;
                }
            }

            public PacketReceiveDelegate DoPacketReceive
            {
                get;
                set;
            }

            public ProtocolNegotiatorDelegate DoProtocolNegotiation
            {
                get;
                set;
            }

            public void ConnectTo(Connection otherEnd)
            {
                this.otherEnd = otherEnd;
                this.RemoteEndPoint = new NullEndPoint(this.otherEnd.transport.Id, this.otherEnd.Id);
            }

            public void Start()
            {
                this.connectionLifetimeTask = Task.Run(this.ManageConnectionLifetime);
            }

            public void Disconnect()
            {
                if (!this.isDisconnected)
                {
                    Trace.TraceInformation($"SimpleTransport.Connection.Disconnect transportId={this.transport.Id}, connectionId={this.Id}");
                    this.packets.CompleteAdding();
                    this.cancellationSource.Cancel();
                    this.isDisconnected = true;
                    this.otherEnd.Disconnect();
                }
            }

            public void Close()
            {
                Trace.TraceInformation($"SimpleTransport.Connection.Close transportId={this.transport.Id}, connectionId={this.Id}");
                this.Disconnect();
                this.connectionLifetimeTask?.Wait();
            }

            public void Dispose()
            {
                this.Close();

                this.packets.Dispose();
                this.packetsAvailable.Dispose();
                this.cancellationSource.Dispose();
            }

            public void Send(byte[] data)
            {
                Task _ = this.SendAsync(data);
            }

            public async Task SendAsync(byte[] data)
            {
                if (data == null)
                {
                    throw new ArgumentNullException(nameof(data));
                }

                var packet = new Packet(Interlocked.Increment(ref this.lastAssignedPacketId), data);
                this.otherEnd.QueuePacket(packet);
                await packet.Completed;
            }

            private void QueuePacket(Packet packet)
            {
                this.packets.Add(packet);
                this.packetsAvailable.Release();
            }

            private async Task ManageConnectionLifetime()
            {
                try
                {
                    CancellationToken cancellationToken = this.cancellationSource.Token;
                    while (!cancellationToken.IsCancellationRequested && !this.packets.IsCompleted)
                    {
                        await this.packetsAvailable.WaitAsync(cancellationToken);
                        Packet packet = this.packets.Take();
                        this.OnPacketReceived(packet.Data);
                        packet.SetCompleted();
                    }
                }
                catch (OperationCanceledException)
                {
                    Trace.TraceError($"SimpleTransport.ManageConnectionLifetime-Canceled transportId={this.transport.Id}, connectionId={this.Id}");
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"SimpleTransport.ManageConnectionLifetime-Failed transportId={this.transport.Id}, connectionId={this.Id}, exception={ex.ToString()}");
                }
                finally
                {
                    // Yield here to ensure that OnConnectionLost() runs as a separate callback.
                    await Task.Yield();
                    Trace.TraceError($"SimpleTransport.ManageConnectionLifetime-NotifyConnectionLoss transportId={this.transport.Id}, connectionId={this.Id}");
                    this.OnConnectionLost?.Invoke();
                    Trace.TraceError($"SimpleTransport.ManageConnectionLifetime-RemoveConnection transportId={this.transport.Id}, connectionId={this.Id}");
                    this.transport.RemoveConnection(this);
                }
            }

            private class Packet
            {
                private readonly TaskCompletionSource<object> completionSource;

                public Packet(long packetId, byte[] data)
                {
                    this.Id = packetId;
                    this.Data = data;
                    this.completionSource = new TaskCompletionSource<object>();
                }

                public long Id { get; private set; }

                public byte[] Data { get; private set; }

                public Task Completed
                {
                    get { return this.completionSource.Task; }
                }

                public void SetCompleted()
                {
                    this.completionSource.SetResult(null);
                }
            };

            private class NullEndPoint : EndPoint
            {
                private readonly long transportId;
                private readonly ulong connectionId;

                public NullEndPoint(long transportId, ulong connectionId)
                {
                    this.transportId = transportId;
                    this.connectionId = connectionId;
                }

                public override string ToString()
                {
                    return $"[TransportId={this.transportId}, ConnectionId={this.connectionId}]";
                }
            }
        }
    }
}