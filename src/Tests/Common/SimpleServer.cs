// <copyright file="SimpleServer.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Test
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;


    /// <summary>
    /// SimpleServer is a server that accepts requests through a transport
    /// and invokes the provided <see cref="IRingMasterRequestHandler"/> to handle
    /// the request.
    /// </summary>
    public class SimpleServer : IDisposable
    {
        private readonly ICommunicationProtocol protocol;
        private readonly uint protocolVersion;
        private readonly IRingMasterRequestHandler requestHandler;
        private ITransport transport;

        public SimpleServer(IRingMasterRequestHandler requestHandler, ICommunicationProtocol protocol, uint protocolVersion = RingMasterCommunicationProtocol.MaximumSupportedVersion)
        {
            this.requestHandler = requestHandler;
            this.protocol = protocol;
            this.protocolVersion = protocolVersion;
        }

        public void RegisterTransport(ITransport transport)
        {
            if (transport == null)
            {
                throw new ArgumentNullException(nameof(transport));
            }

            this.transport = transport;
            this.transport.OnNewConnection = this.OnNewConnection;
        }

        public void Dispose()
        {
            this.requestHandler.Dispose();
            this.transport.Dispose();
            GC.SuppressFinalize(this);
        }

        private void OnNewConnection(IConnection connection)
        {
            connection.OnPacketReceived = packet =>
            {
                Task.Run(async () =>
                {
                    RequestCall call = protocol.DeserializeRequest(packet, packet.Length, this.protocolVersion);
                    RequestResponse response;

                    switch (call.Request.RequestType)
                    {
                        case RingMasterRequestType.Init:
                            {
                                RequestInit initRequest = (RequestInit)call.Request;
                                response = new RequestResponse();
                                response.Content = new string[] { string.Empty + initRequest.SessionId, Guid.NewGuid().ToString() };
                                response.ResultCode = (int)RingMasterException.Code.Ok;
                                break;
                            }

                        default:
                            {
                                response = await this.requestHandler.Request(call.Request);
                                break;
                            }
                    }

                    response.CallId = call.CallId;
                    connection.Send(this.protocol.SerializeResponse(response, this.protocolVersion));
                });
            };

            connection.OnConnectionLost = () => { };
        }
    }
}