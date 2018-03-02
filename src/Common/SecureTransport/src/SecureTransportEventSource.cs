// <copyright file="SecureTransportEventSource.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport
{
    using System.Diagnostics;
    using System.Diagnostics.Tracing;

    /// <summary>
    /// Event Source
    /// </summary>
    [EventSource(Name = "Microsoft-Azure-Networking-Infrastructure-RingMaster-SecureTransport")]
    internal sealed class SecureTransportEventSource : EventSource
    {
        private static readonly SecureTransportEventSource LogInstance = new SecureTransportEventSource();

        public SecureTransportEventSource()
        {
            this.TraceLevel = TraceLevel.Off;
        }

        public static bool LogPacketReceives { get; set; } = false;

        public static SecureTransportEventSource Log
        {
            get { return LogInstance; }
        }

        // Note: TraceLevel has EventId=1 as compiler will auto-generate a method for the property so we
        // must start at 2. Pay attention to fix the event ids if more properties are added in future.
        public TraceLevel TraceLevel { get; set; }

        [Event(2, Level = EventLevel.LogAlways, Version = 2)]
        public void StartServer(long transportId, string endpoint)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"SecureTransport.StartServer transportId={transportId},endPoint={endpoint}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(2, transportId, endpoint);
            }
        }

        [Event(3, Level = EventLevel.LogAlways, Version = 2)]
        public void SignallingStop(long transportId, bool isListening, int activeConnections)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"SecureTransport.SignallingStop transportId={transportId}, isListening={isListening}, activeConnections={activeConnections}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(3, transportId, isListening, activeConnections);
            }
        }

        [Event(4, Level = EventLevel.LogAlways, Version = 2)]
        public void Stopped(long transportId)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"SecureTransport.Stopped transportId={transportId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(4, transportId);
            }
        }

        [Event(5, Level = EventLevel.Informational, Version = 3)]
        public void EstablishConnection(long transportId, int iteration, string serverEndpoint, long timeoutMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"SecureTransport.EstablishConnection transportId={transportId}, iteration={iteration}, serverEndpoint={serverEndpoint}, timeoutMilliseconds={timeoutMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(5, transportId, iteration, serverEndpoint, timeoutMilliseconds);
            }
        }

        [Event(6, Level = EventLevel.Informational, Version = 3)]
        public void AcceptConnection(long transportId, int iteration, string remoteEndpoint)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"SecureTransport.AcceptConnection transportId={transportId}, iteration={iteration}, remoteEndpoint={remoteEndpoint}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(6, transportId, iteration, remoteEndpoint);
            }
        }

        [Event(7, Level = EventLevel.Informational, Version = 4)]
        public void OnNewConnection(long transportId, long connectionId, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"SecureTransport.OnNewConnection transportId={transportId}, connectionId={connectionId}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(7, transportId, connectionId, elapsedMilliseconds);
            }
        }

        [Event(8, Level = EventLevel.Informational, Version = 4)]
        public void OnConnectionLost(long transportId, long connectionId, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"SecureTransport.OnConnectionLost transportId={transportId}, connectionId={connectionId}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(8, transportId, connectionId, elapsedMilliseconds);
            }
        }

        [Event(9, Level = EventLevel.Informational, Version = 3)]
        public void NegotiateProtocol(long transportId, long connectionId, uint localProtocolVersion, uint remoteProtocolVersion, uint acceptedProtocolVersion)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"SecureTransport.NegotiateProtocol transportId={transportId}, connectionId={connectionId}, localProtocolVersion={localProtocolVersion}, remoteProtocolVersion={remoteProtocolVersion}, acceptedProtocolVersion={acceptedProtocolVersion}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(9, transportId, connectionId, localProtocolVersion, remoteProtocolVersion, acceptedProtocolVersion);
            }
        }

        [Event(10, Level = EventLevel.Verbose, Version = 4)]
        public void Send(long transportId, long connectionId, long packetId, int packetLength)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"SecureTransport.Connection.Send transportId={transportId}, connectionId={connectionId}, packetId={packetId}, packetLength={packetLength}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(10, transportId, connectionId, packetId, packetLength);
            }
        }

        [Event(11, Level = EventLevel.Informational, Version = 3)]
        public void ConnectionClose(long transportId, long connectionId)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"SecureTransport.Connection.Close transportId={transportId}, connectionId={connectionId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(11, transportId, connectionId);
            }
        }

        [Event(12, Level = EventLevel.Verbose, Version = 3)]
        public void OnPacketReceived(long transportId, long connectionId, int packetLength)
        {
            if (SecureTransportEventSource.LogPacketReceives)
            {
                if (this.TraceLevel >= TraceLevel.Verbose)
                {
                    Trace.TraceInformation($"SecureTransport.Connection.OnPacketReceived transportId={transportId}, connectionId={connectionId}, packetLength={packetLength}");
                }

                if (this.IsEnabled())
                {
                    this.WriteEvent(12, transportId, connectionId, packetLength);
                }
            }
        }

        [Event(13, Level = EventLevel.Error, Version = 3)]
        public void PullPacketsFailed(long transportId, long connectionId, string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"SecureTransport.Connection.PullPackets-Failed transportId={transportId}, connectionId={connectionId}, exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(13, transportId, connectionId, exception);
            }
        }

        [Event(14, Level = EventLevel.Informational, Version = 4)]
        public void PullPacketsCompleted(long transportId, long connectionId)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"SecureTransport.Connection.PullPackets-Completed transportId={transportId}, connectionId={connectionId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(14, transportId, connectionId);
            }
        }

        [Event(15, Level = EventLevel.Informational, Version = 3)]
        public void ReceiveIncomplete(long transportId, long connectionId, int expectedLength, int actualLength)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"SecureTransport.Connection.PullPackets-ReceiveIncomplete transportId={transportId}, connectionId={connectionId}, expectedLength={expectedLength}, actualLength={actualLength}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(15, transportId, connectionId, expectedLength, actualLength);
            }
        }

        [Event(16, Level = EventLevel.Error, Version = 2)]
        public void ListenerFailed(long transportId, string message)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"SecureTransport Listener failed transportId={transportId} message={message}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(16, transportId, message);
            }
        }

        [Event(17, Level = EventLevel.Informational, Version = 3)]
        public void ConnectFailed(long transportId, int iteration, string host, int port, string message, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"SecureTransport Connect failed transportId={transportId}, iteration={iteration}, host={host}, port={port}, message={message}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(17, transportId, iteration, host, port, message, elapsedMilliseconds);
            }
        }

        [Event(18, Level = EventLevel.Error, Version = 4)]
        public void EstablishConnectionFailed(long transportId, int iteration, int endpointCount, string message, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"SecureTransport EstablishConnection failed transportId={transportId}, iteration={iteration}, endpointCount={endpointCount}, message={message}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(18, transportId, iteration, endpointCount, message, elapsedMilliseconds);
            }
        }

        [Event(19, Level = EventLevel.Error, Version = 2)]
        public void StartClientFailed_AlreadyStarted(long transportId)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"SecureTransport.StartClient failed.  The transport has already been started transportId={transportId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(19, transportId);
            }
        }

        [Event(20, Level = EventLevel.Error, Version = 2)]
        public void StartServerFailed_AlreadyStarted(long transportId)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"SecureTransport.StartServer failed.  The transport has already been started transportId={transportId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(20, transportId);
            }
        }

        [Event(21, Level = EventLevel.Error, Version = 2)]
        public void StopFailed_NotStarted(long transportId)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"SecureTransport.Stop failed.  The transport has not been started transportId={transportId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(21, transportId);
            }
        }

        [Event(22, Level = EventLevel.Informational, Version = 3)]
        public void CloseActiveConnection(long transportId, long connectionId)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"SecureTransport.CloseActiveConnection transportId={transportId} connectionId={connectionId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(22, transportId, connectionId);
            }
        }

        [Event(23, Level = EventLevel.Informational, Version = 2)]
        public void SecureTransportClose(long transportId)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"SecureTransport.Close transportId={transportId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(23, transportId);
            }
        }

        [Event(24, Level = EventLevel.Informational, Version = 4)]
        public void ValidateServerCertificateSucceeded(long transportId, string serialNumber, string issuer, string subject, string thumbprint)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"SecureTransport.ValidateServerCertificate transportId={transportId} serialNumber={serialNumber} issuer={issuer} subject={subject} thumbprint={thumbprint}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(24, transportId, serialNumber, issuer, subject, thumbprint);
            }
        }

        [Event(25, Level = EventLevel.Warning, Version = 2)]
        public void ValidateServerCertificateSkipped(long transportId)
        {
            if (this.TraceLevel >= TraceLevel.Warning)
            {
                Trace.TraceWarning($"SecureTransport.ValidateServerCertificate Skipped transportId={transportId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(25, transportId);
            }
        }

        [Event(26, Level = EventLevel.Error, Version = 3)]
        public void ValidateServerCertificateFailed(long transportId, string serialNumber, string issuer, string subject, string thumbprint)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"SecureTransport.ValidateServerCertificate-Failed transportId={transportId} serialNumber={serialNumber} issuer={issuer} subject={subject} thumbprint={thumbprint}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(26, transportId, serialNumber, issuer, subject, thumbprint);
            }
        }

        [Event(27, Level = EventLevel.Informational, Version = 4)]
        public void ValidateClientCertificateSucceeded(long transportId, string serialNumber, string issuer, string subject, string thumbprint)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"SecureTransport.ValidateClientCertificateSucceeded transportId={transportId} serialNumber={serialNumber} issuer={issuer} subject={subject} thumbprint={thumbprint}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(27, transportId, serialNumber, issuer, subject, thumbprint);
            }
        }

        [Event(28, Level = EventLevel.Warning, Version = 2)]
        public void ValidateClientCertificateSkipped(long transportId)
        {
            if (this.TraceLevel >= TraceLevel.Warning)
            {
                Trace.TraceWarning($"SecureTransport.ValidateClientCertificate Skipped transportId={transportId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(28, transportId);
            }
        }

        [Event(29, Level = EventLevel.Error, Version = 3)]
        public void ValidateClientCertificateFailed(long transportId, string serialNumber, string issuer, string subject, string thumbprint)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"SecureTransport.ValidateClientCertificate-Failed transportId={transportId} serialNumber={serialNumber} issuer={issuer} subject={subject} thumbprint={thumbprint}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(29, transportId, serialNumber, issuer, subject, thumbprint);
            }
        }

        [Event(30, Level = EventLevel.Error, Version = 2)]
        public void ServerCertificatesWereNotProvided(long transportId)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"SecureTransport.ServerCertificatesWereNotProvided transportId={transportId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(30, transportId);
            }
        }

        [Event(31, Level = EventLevel.Warning, Version = 2)]
        public void ClientCertificatesWereNotProvided(long transportId)
        {
            if (this.TraceLevel >= TraceLevel.Warning)
            {
                Trace.TraceWarning($"SecureTransport.ClientCertificatesWereNotProvided transportId={transportId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(31, transportId);
            }
        }

        [Event(32, Level = EventLevel.Informational, Version = 3)]
        public void SupportedClientCertificate(long transportId, string serialNumber)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"SecureTransport.SupportedClientCertificate transportId={transportId} serialNumber={serialNumber}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(32, transportId, serialNumber);
            }
        }

        [Event(33, Level = EventLevel.Informational, Version = 3)]
        public void SupportedServerCertificate(long transportId, string serialNumber)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"SecureTransport.SupportedServerCertificate transportId={transportId}, serialNumber={serialNumber}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(33, transportId, serialNumber);
            }
        }

        [Event(34, Level = EventLevel.Error, Version = 2)]
        public void GetCertificatesFromThumbprintOrFileNameFailed(string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"SecureTransport.GetCertificatesFromThumbPrintOrFileName-Failed exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(34, exception);
            }
        }

        [Event(35, Level = EventLevel.Informational, Version = 4)]
        public void AuthenticateAsClient(long transportId, int timeoutInMilliseconds, bool mustCheckCertificateRevocation, bool mustCheckCertificateTrustChain)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"SecureTransport.AuthenticateAsClient transportId={transportId}, timeoutInMilliseconds={timeoutInMilliseconds} mustCheckCertificateRevocation={mustCheckCertificateRevocation} mustCheckCertificateTrustChain={mustCheckCertificateTrustChain}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(35, transportId, timeoutInMilliseconds);
            }
        }

        [Event(36, Level = EventLevel.Error, Version = 3)]
        public void AuthenticateAsClientFailed(long transportId, string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"SecureTransport.AuthenticateAsClient-Failed transportId={transportId} exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(36, transportId, exception);
            }
        }

        [Event(37, Level = EventLevel.Informational, Version = 5)]
        public void AuthenticateAsServer(long transportId, int timeoutInMilliseconds, bool mustCheckCertificateRevocation, bool mustCheckCertificateTrustChain)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"SecureTransport.AuthenticateAsServer transportId={transportId} timeoutInMilliseconds={timeoutInMilliseconds} mustCheckCertificateRevocation={mustCheckCertificateRevocation} mustCheckCertificateTrustChain={mustCheckCertificateTrustChain}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(37, transportId, timeoutInMilliseconds, mustCheckCertificateRevocation, mustCheckCertificateTrustChain);
            }
        }

        [Event(38, Level = EventLevel.Error, Version = 3)]
        public void AuthenticateAsServerFailed(long transportId, string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"SecureTransport.AuthenticateAsServer-Failed transportId={transportId}, exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(38, transportId, exception);
            }
        }

        [Event(39, Level = EventLevel.Error, Version = 4)]
        public void AcceptConnectionFailed(long transportId, int iteration, string remoteEndpoint, long elapsedMilliseconds, string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"SecureTransport.AcceptConnection-Failed transportId={transportId}, iteration={iteration}, remoteEndpoint={remoteEndpoint}, elapsedMilliseconds={elapsedMilliseconds}, exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(39, transportId, iteration, remoteEndpoint, elapsedMilliseconds, exception);
            }
        }

        [Event(40, Level = EventLevel.Warning, Version = 2)]
        public void ListenerStopped(long transportId)
        {
            if (this.TraceLevel >= TraceLevel.Warning)
            {
                Trace.TraceWarning($"SecureTransport.ListenerStopped transportId={transportId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(40, transportId);
            }
        }

        [Event(41, Level = EventLevel.Error, Version = 2)]
        public void StartTimedout(long transportId)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"SecureTransport.StartFailed-Timedout transportId={transportId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(41, transportId);
            }
        }

        [Event(42, Level = EventLevel.Error, Version = 3)]
        public void StopTimedout(long transportId)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"SecureTransport.StopFailed-Timedout transportId={transportId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(42, transportId);
            }
        }

        [Event(43, Level = EventLevel.Error, Version = 3)]
        public void AcceptTcpClientFailed(long transportId, int iteration, int consecutiveFailureCount, string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"SecureTransport.AcceptTcpClient failed  transportId={transportId}, iteration={iteration}, consecutiveFailureCount={consecutiveFailureCount}, exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(43, transportId, iteration, consecutiveFailureCount, exception);
            }
        }

        [Event(44, Level = EventLevel.LogAlways, Version = 2)]
        public void StartClient(long transportId, int endpointCount)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"SecureTransport.StartClient  transportId={transportId}, endpointCount={endpointCount}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(44, transportId, endpointCount);
            }
        }

        [Event(45, Level = EventLevel.Informational, Version = 2)]
        public void SetConnectionLifetimeLimit(long transportId, long connectionId, long maxConnectionLifetimeInMs)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"SecureTransport.SetConnectionLifetimeLimit  transportId={transportId}, connectionId={connectionId}, maxConnectionLifetimeInMs={maxConnectionLifetimeInMs}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(45, transportId, connectionId, maxConnectionLifetimeInMs);
            }
        }

        [Event(46, Level = EventLevel.Informational, Version = 3)]
        public void ConnectionLifetimeLimitExpired(long transportId, long connectionId, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"SecureTransport.ConnectionLifetimeLimitExpired  transportId={transportId}, connectionId={connectionId}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(46, transportId, connectionId, elapsedMilliseconds);
            }
        }

        [Event(47, Level = EventLevel.Error, Version = 2)]
        public void PushPacketsFailed(long transportId, long connectionId, string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"SecureTransport.Connection.PushPackets-Failed  transportId={transportId}, connectionId={connectionId}, exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(47, transportId, connectionId, exception);
            }
        }

        [Event(48, Level = EventLevel.Informational, Version = 3)]
        public void PushPacketsCompleted(long transportId, long connectionId)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"SecureTransport.Connection.PushPackets-Completed  transportId={transportId}, connectionId={connectionId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(48, transportId, connectionId);
            }
        }

        [Event(49, Level = EventLevel.Informational, Version = 1)]
        public void SetConnectionIdleTimeLimit(long transportId, long connectionId, long maxConnectionIdleTimeInMs)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"SecureTransport.SetConnectionIdleTimeLimit transportId={transportId}, connectionId={connectionId}, maxConnectionIdleTimeInMs={maxConnectionIdleTimeInMs}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(49, transportId, connectionId, maxConnectionIdleTimeInMs);
            }
        }

        [Event(50, Level = EventLevel.Informational, Version = 1)]
        public void ConnectionIdleTimeLimitExpired(long transportId, long connectionId, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"SecureTransport.ConnectionIdleTimeLimitExpired  transportId={transportId}, connectionId={connectionId}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(50, transportId, connectionId, elapsedMilliseconds);
            }
        }

        [Event(51, Level = EventLevel.Informational, Version = 1)]
        public void Disconnect(long transportId, long connectionId)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"SecureTransport.Connection.Disconnect  transportId={transportId}, connectionId={connectionId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(51, transportId, connectionId);
            }
        }

        [Event(52, Level = EventLevel.Error, Version = 1)]
        public void SendQueueFull(long transportId, long connectionId, long packetId, int packetLength)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"SecureTransport.Connection.SendFailed QueueFull transportId={transportId}, connectionId={connectionId}, packetId={packetId}, packetLength={packetLength}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(52, transportId, connectionId, packetId, packetLength);
            }
        }

        [Event(53, Level = EventLevel.Error, Version = 1)]
        public void CertificateIsNotAValidX509Certificate2(string serialNumber)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError("SecureTransport.CertificateIsNotAValidX509Certificate2 serialNumber={0}", serialNumber);
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(53, serialNumber);
            }
        }

        [Event(54, Level = EventLevel.Error, Version = 1)]
        public void CertificateIsNotValid(string serialNumber, string notValidBefore, string notValidAfter)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError("SecureTransport.CertificateIsNotValidYet serialNumber={0}, notValidBefore={1}, notValidAfter={2}", serialNumber, notValidBefore, notValidAfter);
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(54, serialNumber, notValidBefore, notValidAfter);
            }
        }

        [Event(55, Level = EventLevel.Informational, Version = 1)]
        public void ConnectSucceeded(long transportId, int iteration, string host, int port, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"SecureTransport Connect succeeded transportId={transportId}, iteration={iteration}, host={host}, port={port}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(55, transportId, iteration, host, port, elapsedMilliseconds);
            }
        }

        [Event(56, Level = EventLevel.Error, Version = 1)]
        public void EstablishConnectionSucceeded(long transportId, int iteration, string host, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"SecureTransport.EstablishConnection succeeded transportId={transportId}, iteration={iteration}, host={host}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(56, transportId, iteration, host, elapsedMilliseconds);
            }
        }

        [Event(57, Level = EventLevel.Error, Version = 1)]
        public void EstablishConnectionCloseUnsuccessfulConnection(long transportId, int iteration, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"SecureTransport.EstablishConnection CloseUnsuccessfulConnection transportId={transportId}, iteration={iteration}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(57, transportId, iteration, elapsedMilliseconds);
            }
        }

        [Event(58, Level = EventLevel.Error, Version = 1)]
        public void AcceptConnectionSucceeded(long transportId, int iteration, string remoteEndpoint, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"SecureTransport.AcceptConnection Succeeded transportId={transportId}, iteration={iteration}, remoteEndpoint={remoteEndpoint}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(58, transportId, iteration, remoteEndpoint, elapsedMilliseconds);
            }
        }

        [Event(59, Level = EventLevel.Error, Version = 1)]
        public void AcceptConnectionCloseUnsuccessfulConnection(long transportId, int iteration, string remoteEndpoint, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"SecureTransport.AcceptConnection CloseUnsuccessfulConnection transportId={transportId}, iteration={iteration}, remoteEndpoint={remoteEndpoint}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(59, transportId, iteration, remoteEndpoint, elapsedMilliseconds);
            }
        }

        [Event(60, Level = EventLevel.Error, Version = 2)]
        public void CertificateHasNoChainStatus(long transportId, string issuer, string subject, string serialNumber, string thumbprint)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"SecureTransport.CertificateHasNoChainStatus transportId={transportId}, issuer={issuer}, subject={subject}, serialNumber={serialNumber}, thumbprint={thumbprint}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(60, transportId, issuer, subject, serialNumber, thumbprint);
            }
        }

        [Event(61, Level = EventLevel.Warning, Version = 2)]
        public void CertificateCrlOffline(long transportId, string issuer, string subject, string serialNumber, string thumbprint)
        {
            if (this.TraceLevel >= TraceLevel.Warning)
            {
                Trace.TraceError($"SecureTransport.CertificateCrlOffline transportId={transportId}, issuer={issuer}, subject={subject}, serialNumber={serialNumber}, thumbprint={thumbprint}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(61, transportId, issuer, subject, serialNumber, thumbprint);
            }
        }

        [Event(62, Level = EventLevel.Error, Version = 2)]
        public void CertificateSslPolicyErrors(long transportId, string issuer, string subject, string serialNumber, string thumbprint, string policyErrors)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"SecureTransport.CertificateSslPolicyErrors transportId={transportId}, issuer={issuer}, subject={subject}, serialNumber={serialNumber}, thumbprint={thumbprint}, policyErrors={policyErrors}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(62, transportId, issuer, subject, serialNumber, thumbprint, policyErrors);
            }
        }

        [Event(63, Level = EventLevel.Warning, Version = 2)]
        public void CertificateTrustChainValidationSkipped(long transportId, string issuer, string subject, string serialNumber, string thumbprint)
        {
            if (this.TraceLevel >= TraceLevel.Warning)
            {
                Trace.TraceWarning($"SecureTransport.CertificateTrustChainValidationSkipped transportId={transportId}, issuer={issuer}, subject={subject}, serialNumber={serialNumber}, thumbprint={thumbprint}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(63, transportId, issuer, subject, serialNumber, thumbprint);
            }
        }

        [Event(64, Level = EventLevel.Informational, Version = 1)]
        public void OnConnectionLostNotificationCompleted(long transportId, long connectionId, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"SecureTransport.Connection.OnConnectionLostNotificationCompleted transportId={transportId}, connectionId={connectionId}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(64, transportId, connectionId, elapsedMilliseconds);
            }
        }
    }
}