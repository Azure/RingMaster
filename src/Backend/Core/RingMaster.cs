// <copyright file="RingMaster.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using Azure.Networking.Infrastructure.RingMaster.Communication;
    using Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol;
    using Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport;

    using RedirectionPolicy = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestInit.RedirectionPolicy;
    using RingMasterRequestType = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RingMasterRequestType;

    /// <summary>
    /// Operation code used by set data operation
    /// </summary>
    internal enum SetDataOperationCode : ushort
    {
#pragma warning disable SA1602
        None = 0,
        InterlockedAddIfVersion = 1,
        InterlockedXORIfVersion = 2,
#pragma warning restore
    }

    /// <summary>
    /// Concrete implementation of <see cref="AbstractRingMaster" />.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Backward compatibility")]
    internal sealed class RingMaster : AbstractRingMaster
    {
        /// <summary>
        /// <see cref="RingMasterCommunicationProtocol"/> is used as the communication protocol.
        /// </summary>
        private readonly ICommunicationProtocol protocol = new RingMasterCommunicationProtocol();

        private readonly SecureTransport.Configuration transportConfiguration = new SecureTransport.Configuration();

        private int sessionTimeout;
        private int requestTimeout;
        private RingMasterClient client;
        private SecureTransport secureTransport;

        /// <summary>
        /// Initializes a new instance of the <see cref="RingMaster"/> class.
        /// To create a RingMaster client object, the application needs to pass a connection string containing a comma separated list of host:port pairs, each corresponding to a ZooKeeper server
        /// </summary>
        /// <param name="connectString">The connect string.</param>
        /// <param name="sessionTimeout">The session timeout.</param>
        /// <param name="watcher">The watcher.</param>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="sessionPasswd">The session passwd.</param>
        /// <param name="requestTimeout">Request timeout in millisecond</param>
        public RingMaster(string connectString, int sessionTimeout, IWatcher watcher, long sessionId = 0, byte[] sessionPasswd = null, int requestTimeout = 15000)
            : base(connectString, sessionTimeout, watcher, sessionId, sessionPasswd, requestTimeout)
        {
            this.transportConfiguration.UseSecureConnection = false;
            this.transportConfiguration.CommunicationProtocolVersion = RingMasterCommunicationProtocol.MaximumSupportedVersion;
        }

        /// <inheritdoc />
        public override int SessionTimeout => this.sessionTimeout;

        /// <inheritdoc />
        public override int RequestTimeout => this.requestTimeout;

        /// <inheritdoc />
        public override void Initialize(int sessionTimeout, int requestTimeout)
        {
            this.sessionTimeout = sessionTimeout;
            this.requestTimeout = requestTimeout;
        }

        /// <summary>
        /// Set the SSL configuration.
        /// </summary>
        /// <param name="ssl">SslWrapping object</param>
        public void SetSsl(SslWrapping ssl)
        {
            if (ssl != null)
            {
                this.transportConfiguration.UseSecureConnection = true;
                this.transportConfiguration.Identities = ssl.Identities;
                this.transportConfiguration.MustCheckCertificateRevocation = ssl.MustCheckCertificateRevocation;
                this.transportConfiguration.MustCheckCertificateTrustChain = ssl.MustCheckCertificateTrustChain;
            }
            else
            {
                this.transportConfiguration.UseSecureConnection = false;
            }

            this.client?.Close();
            this.client = null;
        }

        /// <inheritdoc />
        public override void Close()
        {
            this.client?.Close();
        }

        /// <inheritdoc />
        public override void Send(IRingMasterBackendRequest req)
        {
            if (req == null)
            {
                throw new ArgumentNullException("req");
            }

            if (this.client == null)
            {
                SecureTransport transport = null;
                try
                {
                    var endpoints = SecureTransport.ParseConnectionString(this.ConnectString);
                    transport = new SecureTransport(this.transportConfiguration);
                    this.client = new RingMasterClient(this.protocol, transport);
                    this.secureTransport = transport;

                    // The lifetime of transport is now owned by RingMasterClient
                    transport = null;

                    this.secureTransport.StartClient(endpoints);
                }
                finally
                {
                    transport?.Dispose();
                }
            }

            this.client.Request(req.WrappedRequest).ContinueWith(responseTask =>
            {
                try
                {
                    RequestResponse response = responseTask.Result;
                    req.NotifyComplete(response.ResultCode, response.Content, response.Stat);
                }
                catch (System.Exception)
                {
                    req.NotifyComplete((int)RingMasterException.Code.Systemerror, null, null);
                }
            });
        }

        /// <inheritdoc />
        public override ISetDataOperationHelper GetSetDataOperationHelper()
        {
            return SetDataOperationHelper.Instance;
        }

        /// <inheritdoc />
        protected override void OnComplete(IRingMasterBackendRequest req, int resultcode, double timeInMillis)
        {
            // No additional work
        }
    }
}
