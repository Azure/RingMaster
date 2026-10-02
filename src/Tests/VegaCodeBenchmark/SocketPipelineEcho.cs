// <copyright file="SocketPipelineEcho.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Test
{
    using System.IO.Pipelines;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Echo server and client using high-performance pipeline
    /// </summary>
    internal sealed class SocketPipelineEcho
    {
        private const int MinimumBufferSize = 1024;

        private int receiveCount;

        /// <summary>
        /// Gets the port that the server is listeneing on
        /// </summary>
        public int ServerPort { get; private set; }

        /// <summary>
        /// Gets the number of packets received by the server
        /// </summary>
        public int ReceiveCount => this.receiveCount;

        /// <summary>
        /// Resets the receive count
        /// </summary>
        public void ResetCount()
        {
            Interlocked.Exchange(ref this.receiveCount, 0);
        }

        /// <summary>
        /// Starts the echo server
        /// </summary>
        /// <param name="cancellation">Cancellation token</param>
        /// <returns>async task to indicate the completion of socket server</returns>
        public async Task StartServer(CancellationToken cancellation)
        {
            var listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(new IPEndPoint(IPAddress.Any, 0));
            listenSocket.Listen(1024);

            this.ServerPort = ((IPEndPoint)listenSocket.LocalEndPoint).Port;

            cancellation.Register(() => listenSocket.Close());
            while (!cancellation.IsCancellationRequested)
            {
                var acceptedSocket = await listenSocket.AcceptAsync().ConfigureAwait(false);
                _ = this.ProcessMessageAsync(acceptedSocket);
            }
        }

        /// <summary>
        /// Starts the echo client
        /// </summary>
        /// <param name="port">Port of the server</param>
        /// <param name="cancellation">Cancellation token</param>
        /// <returns>async task to indicate the completion of the socket client</returns>
        public async Task StartClient(int port, CancellationToken cancellation)
        {
            var clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            await clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Loopback, port));
            _ = Task.Run(async () => await this.ProcessMessageAsync(clientSocket));

            cancellation.Register(() => clientSocket.Close());

            await clientSocket.SendAsync(new byte[1024], SocketFlags.None);
        }

        private async Task ProcessMessageAsync(Socket socket)
        {
            var pipe = new Pipe();
            await Task.WhenAll(
                this.FillPipeAsync(socket, pipe.Writer),
                this.ReadPipeAsync(socket, pipe.Reader))
                .ConfigureAwait(false);
        }

        private async Task FillPipeAsync(Socket socket, PipeWriter writer)
        {
            while (true)
            {
                try
                {
                    var memory = writer.GetMemory(MinimumBufferSize);
                    int bytesRead = await socket.ReceiveAsync(memory, SocketFlags.None);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    writer.Advance(bytesRead);
                }
                catch
                {
                    break;
                }

                if ((await writer.FlushAsync()).IsCompleted)
                {
                    break;
                }
            }

            writer.Complete();
        }

        private async Task ReadPipeAsync(Socket socket, PipeReader reader)
        {
            while (true)
            {
                var result = await reader.ReadAsync();
                var buffer = result.Buffer;

                Interlocked.Increment(ref this.receiveCount);

                // Send everything back to the other party
                foreach (var segment in buffer)
                {
                    await socket.SendAsync(segment, SocketFlags.None);
                }

                // Indicate that the buffer has been consumed
                reader.AdvanceTo(buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }

            reader.Complete();
        }
    }
}
