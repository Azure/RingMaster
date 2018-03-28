// <copyright file="RingMasterClient.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport;

    /// <summary>
    /// RingMaster Client - Implements IRingMaster interface using RingMasterRequestHandler.
    /// </summary>
    public sealed class RingMasterClient : IRingMasterRequestHandler, IRingMaster
    {
        /// <summary>
        /// Channel through which <see cref="IRingMasterRequest"/>s created by this client will be sent.
        /// </summary>
        private IRingMasterRequestHandler requestHandler;

        /// <summary>
        /// The current watcher set for this client.
        /// </summary>
        private IWatcher watcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterClient"/> class.
        /// </summary>
        /// <param name="serverSpec">Server specification</param>
        /// <param name="configuration">Configuration settings</param>
        /// <param name="instrumentation">Instrumentation consumer</param>
        /// <param name="watcher">Watcher interface</param>
        /// <param name="cancellationToken">Token that will be observed for cancellation signal</param>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "secureTransport is being disposed correctly")]
        public RingMasterClient(
            ServerSpec serverSpec,
            Configuration configuration,
            IRingMasterClientInstrumentation instrumentation,
            IWatcher watcher,
            CancellationToken cancellationToken)
        {
            if (serverSpec == null)
            {
                throw new ArgumentNullException(nameof(serverSpec));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            IPEndPoint[] endpoints = serverSpec.Endpoints;
            if ((endpoints == null) || (endpoints.Length == 0))
            {
                throw new ArgumentException("Endpoints were not specified");
            }

            if (watcher != null)
            {
                this.SetWatcher(watcher);
            }

            SecureTransport secureTransport = null;
            try
            {
                var transportConfiguration = new SecureTransport.Configuration();
                transportConfiguration.UseSecureConnection = serverSpec.UseSecureConnection;
                transportConfiguration.ClientCertificates = new X509Certificate[] { serverSpec.ClientCertificate };
                transportConfiguration.ServerCertificates = serverSpec.AcceptedServerCertificates;
                transportConfiguration.RemoteCertificateValidationCallback = serverSpec.ServerCertificateValidationCallback;
                transportConfiguration.LocalCertificateSelectionCallback = serverSpec.ClientCertificationSelectionCallback;
                transportConfiguration.MustCheckCertificateRevocation = serverSpec.MustCheckCertificateRevocation;
                transportConfiguration.MustCheckCertificateTrustChain = serverSpec.MustCheckCertificateTrustChain;
                transportConfiguration.CommunicationProtocolVersion = serverSpec.CommunicationProtocolVersion;
                transportConfiguration.SendBufferSize = configuration.BufferSize;
                transportConfiguration.ReceiveBufferSize = configuration.BufferSize;
                transportConfiguration.SendQueueLength = configuration.RequestQueueLength;
                transportConfiguration.AuthAsClient = true;

                List<SecureTransport.SubjectRuleValidation> subjectRules = new List<SecureTransport.SubjectRuleValidation>();

                if (!string.IsNullOrWhiteSpace(serverSpec.CertValidationASubject) && serverSpec.CertValidationASigningThumbprints != null)
                {
                    SecureTransport.SubjectValidation subjectA = new SecureTransport.SubjectValidation()
                    {
                        CertificateSubject = serverSpec.CertValidationASubject,
                        SigningCertThumbprints = serverSpec.CertValidationASigningThumbprints,
                    };
                    subjectRules.Add(new SecureTransport.SubjectRuleValidation(CertificateRules.AbstractCertificateRule.RoleToApply.ServerCert, subjectA));
                }

                if (!string.IsNullOrWhiteSpace(serverSpec.CertValidationBSubject) && serverSpec.CertValidationBSigningThumbprints != null)
                {
                    SecureTransport.SubjectValidation subjectB = new SecureTransport.SubjectValidation()
                    {
                        CertificateSubject = serverSpec.CertValidationBSubject,
                        SigningCertThumbprints = serverSpec.CertValidationBSigningThumbprints,
                    };
                    subjectRules.Add(new SecureTransport.SubjectRuleValidation(CertificateRules.AbstractCertificateRule.RoleToApply.ServerCert, subjectB));
                }

                if (subjectRules != null && subjectRules.Count > 0)
                {
                    transportConfiguration.SubjectValidations = subjectRules;
                }

                secureTransport = new SecureTransport(transportConfiguration, instrumentation: null, cancellationToken: cancellationToken);

                ICommunicationProtocol protocol = new RingMasterCommunicationProtocol();

                var handlerConfiguration = new RingMasterRequestHandler.Configuration();

                handlerConfiguration.DefaultTimeout = configuration.DefaultTimeout;
                handlerConfiguration.HeartBeatInterval = configuration.HeartBeatInterval;
                handlerConfiguration.RequestQueueLength = configuration.RequestQueueLength;
                handlerConfiguration.RequireLockForReadOnlyOperations = configuration.RequireLockForReadOnlyOperations;
                handlerConfiguration.MustTransparentlyForwardRequests = configuration.MustTransparentlyForwardRequests;

                var handlerInstrumentation = new RingMasterRequestHandlerInstrumentation(instrumentation);

                this.requestHandler = new RingMasterRequestHandler(
                    handlerConfiguration,
                    handlerInstrumentation,
                    protocol,
                    secureTransport,
                    cancellationToken);

                foreach (var endpoint in endpoints)
                {
                    RingMasterClientEventSource.Log.Start(endpoint.ToString());
                }

                secureTransport.StartClient(endpoints);
                secureTransport = null;
            }
            finally
            {
                if (secureTransport != null)
                {
                    secureTransport.Dispose();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterClient"/> class.
        /// </summary>
        /// <param name="serverSpec">Server specification</param>
        /// <param name="configuration">Configuration settings</param>
        /// <param name="instrumentation">Instrumentation consumer</param>
        /// <param name="cancellationToken">Token that will be observed for cancellation signal</param>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "secureTransport is being disposed correctly")]
        public RingMasterClient(
            ServerSpec serverSpec,
            Configuration configuration,
            IRingMasterClientInstrumentation instrumentation,
            CancellationToken cancellationToken)
            : this(serverSpec, configuration, instrumentation, null, cancellationToken)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterClient"/> class.
        /// </summary>
        /// <param name="connectionString">Comma separated list of host:port pairs, each corresponding to a RingMaster server</param>
        /// <param name="configuration">Configuration settings</param>
        /// <param name="instrumentation">Instrumentation consumer</param>
        /// <param name="cancellationToken">Token that will be observed for cancellation signal</param>
        public RingMasterClient(
            string connectionString,
            Configuration configuration,
            IRingMasterClientInstrumentation instrumentation,
            CancellationToken cancellationToken)
            : this(CreateServerSpec(connectionString), configuration, instrumentation, cancellationToken)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterClient"/> class.
        /// </summary>
        /// <param name="connectionString">Comma separated list of host:port pairs, each corresponding to a RingMaster server</param>
        /// <param name="configuration">Configuration settings</param>
        /// <param name="instrumentation">Instrumentation consumer</param>
        public RingMasterClient(
            string connectionString,
            Configuration configuration,
            IRingMasterClientInstrumentation instrumentation = null)
            : this(connectionString, configuration, instrumentation, CancellationToken.None)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterClient"/> class.
        /// </summary>
        /// <param name="connectionString">Comma separated list of host:port pairs,
        ///  each corresponding to a RingMaster server</param>
        /// <param name="clientCerts">Client certificates to use for SSL connection</param>
        /// <param name="serverCerts">Server certificates to use for SSL connection</param>
        /// <param name="requestTimeout">Request timeout</param>
        /// <param name="watcher">Watcher interface</param>
        /// <param name="cancellationToken">Token that will be observed for cancellation signal</param>
        public RingMasterClient(
            string connectionString,
            X509Certificate[] clientCerts,
            X509Certificate[] serverCerts,
            TimeSpan requestTimeout,
            IWatcher watcher,
            CancellationToken cancellationToken)
            : this(CreateServerSpec(connectionString, clientCerts, serverCerts), new Configuration() { DefaultTimeout = requestTimeout }, null, watcher, cancellationToken)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterClient"/> class.
        /// </summary>
        /// <param name="connectionString">Comma separated list of host:port pairs,
        ///  each corresponding to a RingMaster server</param>
        /// <param name="clientCerts">Client certificates to use for SSL connection</param>
        /// <param name="serverCerts">Server certificates to use for SSL connection</param>
        /// <param name="requestTimeout">Request timeout in milliseconds</param>
        /// <param name="watcher">Watcher interface</param>
        /// <param name="cancellationToken">Token that will be observed for cancellation signal</param>
        public RingMasterClient(
            string connectionString,
            X509Certificate[] clientCerts,
            X509Certificate[] serverCerts,
            int requestTimeout,
            IWatcher watcher,
            CancellationToken cancellationToken)
            : this(connectionString, clientCerts, serverCerts, TimeSpan.FromMilliseconds(requestTimeout), watcher, cancellationToken)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterClient"/> class.
        /// </summary>
        /// <param name="connectionString">Comma separated list of host:port pairs,
        ///  each corresponding to a RingMaster server</param>
        /// <param name="clientCerts">Client certificates to use for SSL connection</param>
        /// <param name="serverCerts">Server certificates to use for SSL connection</param>
        /// <param name="requestTimeout">Request timeout in milliseconds</param>
        /// <param name="watcher">Watcher interface</param>
        public RingMasterClient(
            string connectionString,
            X509Certificate[] clientCerts,
            X509Certificate[] serverCerts,
            int requestTimeout,
            IWatcher watcher = null)
            : this(connectionString, clientCerts, serverCerts, requestTimeout, watcher, CancellationToken.None)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterClient"/> class.
        /// </summary>
        /// <param name="connectionString">Comma separated list of host:port pairs,
        ///  each corresponding to a RingMaster server</param>
        /// <param name="clientThumbprints">Thumbprints of client certificates to use for SSL connection</param>
        /// <param name="serverThumbprints">Thumbprints of server certificates to use for SSL connection</param>
        /// <param name="requestTimeout">Request timeout</param>
        /// <param name="watcher">Watcher interface</param>
        public RingMasterClient(
            string connectionString,
            string[] clientThumbprints,
            string[] serverThumbprints,
            TimeSpan requestTimeout,
            IWatcher watcher)
            : this(
                connectionString,
                GetCertificatesFromThumbPrintOrFileName(clientThumbprints),
                GetCertificatesFromThumbPrintOrFileName(serverThumbprints),
                requestTimeout,
                watcher,
                CancellationToken.None)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterClient"/> class.
        /// </summary>
        /// <param name="connectionString">Comma separated list of host:port pairs,
        ///  each corresponding to a RingMaster server</param>
        /// <param name="clientThumbprints">Thumbprints of client certificates to use for SSL connection</param>
        /// <param name="serverThumbprints">Thumbprints of server certificates to use for SSL connection</param>
        /// <param name="requestTimeout">Request timeout in milliseconds</param>
        /// <param name="watcher">Watcher interface</param>
        public RingMasterClient(
            string connectionString,
            string[] clientThumbprints,
            string[] serverThumbprints,
            int requestTimeout,
            IWatcher watcher)
            : this(
                connectionString,
                clientThumbprints,
                serverThumbprints,
                TimeSpan.FromMilliseconds(requestTimeout),
                watcher)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterClient"/> class.
        /// </summary>
        /// <param name="requestHandler">Channel through which <see cref="IRingMasterRequest"/>s created by this client will be sent.</param>
        public RingMasterClient(
            IRingMasterRequestHandler requestHandler)
        {
            this.requestHandler = requestHandler;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterClient"/> class.
        /// </summary>
        /// <param name="configuration">RingMasterClient configuration</param>
        /// <param name="instrumentation">Instrumentation consumer</param>
        /// <param name="communicationProtocol">Interface to the communication protocol</param>
        /// <param name="transport">Interface to the transport layer</param>
        /// <param name="cancellationToken">Token that will be observed for cancellation signal</param>
        public RingMasterClient(
            Configuration configuration,
            IRingMasterClientInstrumentation instrumentation,
            ICommunicationProtocol communicationProtocol,
            ITransport transport,
            CancellationToken cancellationToken)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var handlerConfiguration = new RingMasterRequestHandler.Configuration();

            handlerConfiguration.DefaultTimeout = configuration.DefaultTimeout;
            handlerConfiguration.HeartBeatInterval = configuration.HeartBeatInterval;
            handlerConfiguration.RequestQueueLength = configuration.RequestQueueLength;
            handlerConfiguration.RequireLockForReadOnlyOperations = configuration.RequireLockForReadOnlyOperations;
            handlerConfiguration.MustTransparentlyForwardRequests = configuration.MustTransparentlyForwardRequests;

            var handlerInstrumentation = new RingMasterRequestHandlerInstrumentation(instrumentation);

            this.requestHandler = new RingMasterRequestHandler(handlerConfiguration, handlerInstrumentation, communicationProtocol, transport, cancellationToken);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterClient"/> class.
        /// </summary>
        /// <param name="instrumentation">Instrumentation consumer</param>
        /// <param name="communicationProtocol">Interface to the communication protocol</param>
        /// <param name="transport">Interface to the transport layer</param>
        /// <param name="cancellationToken">Token that will be observed for cancellation signal</param>
        public RingMasterClient(
            IRingMasterClientInstrumentation instrumentation,
            ICommunicationProtocol communicationProtocol,
            ITransport transport,
            CancellationToken cancellationToken)
            : this(new Configuration(), instrumentation, communicationProtocol, transport, cancellationToken)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterClient"/> class.
        /// </summary>
        /// <param name="communicationProtocol">Interface to the communication protocol</param>
        /// <param name="transport">Interface to the transport layer</param>
        /// <param name="cancellationToken">Token that will be observed for cancellation signal</param>
        public RingMasterClient(
            ICommunicationProtocol communicationProtocol,
            ITransport transport,
            CancellationToken cancellationToken)
            : this(null, communicationProtocol, transport, cancellationToken)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterClient"/> class.
        /// </summary>
        /// <param name="communicationProtocol">Interface to the communication protocol</param>
        /// <param name="transport">Interface to the transport layer</param>
        public RingMasterClient(
            ICommunicationProtocol communicationProtocol,
            ITransport transport)
            : this(communicationProtocol, transport, CancellationToken.None)
        {
        }

        /// <summary>
        /// Gets or sets the number of milliseconds to wait before a request is timed out.
        /// </summary>
        public int Timeout
        {
            get
            {
                return this.requestHandler.Timeout;
            }

            set
            {
                this.requestHandler.Timeout = value;
            }
        }

        /// <summary>
        /// Gets a list of <see cref="X509Certificate"/>s that corresponds to the given thumbprint or file paths.
        /// </summary>
        /// <param name="paths">List of thumbprint or file paths</param>
        /// <returns> A list of <see cref="X509Certificate"/>s that correspond to the given paths</returns>
        public static X509Certificate[] GetCertificatesFromThumbPrintOrFileName(string[] paths)
        {
            return SecureTransport.GetCertificatesFromThumbPrintOrFileName(paths);
        }

        /// <summary>
        /// Specify the default watcher for the connection (overrides the one specified during construction).
        /// </summary>
        /// <param name="watcher">Watcher interface</param>
        public void SetWatcher(IWatcher watcher)
        {
            this.watcher = watcher;
        }

        /// <summary>
        /// Sets the cache instance for the client.
        /// </summary>
        /// <param name="cache">The cache instance to set.</param>
        /// <param name="cachePrefix">The cache prefix for this instance.</param>
        /// <param name="auto_invalidate">if set to <c>true</c> the cached elements will be auto-invalidated by
        /// this instance when modified in ringmaster (i.e. with watchers). Otherwise, the instances invalidation is
        /// responsibility of the component providing the cache object)</param>
        /// <param name="allowReplacement">if set to <c>true</c> the caller is indicating that a cache replacement is allowed.
        /// Otherwise, if there is already a cached object different from the provided one, this method will have no effect.</param>
        /// <returns>the cache object that will be used from now on (e.g if allowReplacement was false, and there was
        /// already another cache object set, the return value is such previous cache object)</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "we dispose cacheHandler when we dispose this object")]
        public Task<IRingMasterClientCache> SetCacheInstance(IRingMasterClientCache cache, string cachePrefix, bool auto_invalidate, bool allowReplacement)
        {
            RingMasterCachedRequestHandler cachedHandler = null;

            // we first make sure this.requestHandler is set to a cached handler, but avoiding race conditions on the set.
            while (true)
            {
                IRingMasterRequestHandler currentH = this.requestHandler;

                cachedHandler = currentH as RingMasterCachedRequestHandler;

                if (cachedHandler != null)
                {
                    break;
                }

                cachedHandler = new RingMasterCachedRequestHandler(currentH);

                if (Interlocked.CompareExchange(ref this.requestHandler, cachedHandler, currentH) == currentH)
                {
                    break;
                }
                else
                {
                    cachedHandler.Abandon();
                    cachedHandler.Dispose();
                    cachedHandler = null;
                }
            }

            // then, we call setcacheinstance on the cached handler.
            Task<IRingMasterClientCache> set = cachedHandler.SetCacheInstance(cache, cachePrefix, auto_invalidate, allowReplacement);
            return set;
        }

        /// <summary>
        /// Creates a node with the given path.
        /// </summary>
        /// <param name="path">Node Path</param>
        /// <param name="data">Data to associate with the node</param>
        /// <param name="acl">Access Control List</param>
        /// <param name="createMode">Specifies the node will be created</param>
        /// <param name="throwIfNodeExists">if true, and the error is <c>Nodeexists</c>, it generates an exception</param>
        /// <returns>Task that tracks completion of this method</returns>
        public Task Create(string path, byte[] data, IReadOnlyList<Acl> acl, CreateMode createMode, bool throwIfNodeExists)
        {
            RingMasterClientEventSource.Log.Create(path);

            return this.requestHandler.Create(path, data, acl, createMode, throwIfNodeExists);
        }

        /// <summary>
        /// Creates a node with the given path.
        /// </summary>
        /// <param name="path">Node Path</param>
        /// <param name="data">Data to associate with the node</param>
        /// <param name="acl">Access Control List</param>
        /// <param name="createMode">Specifies the node will be created</param>
        /// <returns>Task that will resolve on success to the path to the newly created node</returns>
        public Task<string> Create(string path, byte[] data, IReadOnlyList<Acl> acl, CreateMode createMode)
        {
            RingMasterClientEventSource.Log.Create(path);

            return this.requestHandler.Create(path, data, acl, createMode);
        }

        /// <summary>
        /// Moves a node with the given path.
        /// </summary>
        /// <param name="srcPath">Node Path to move</param>
        /// <param name="version">version of the source node</param>
        /// <param name="newParentPath">Node Path to be parent of the moved node</param>
        /// <param name="moveMode">Modifiers for the move operation</param>
        /// <returns>Task that will resolve on success to the path to the newly created node</returns>
        public Task<string> Move(string srcPath, int version, string newParentPath, MoveMode moveMode)
        {
            RingMasterClientEventSource.Log.Move(srcPath, newParentPath);

            return this.requestHandler.Move(srcPath, version, newParentPath, moveMode);
        }

        /// <summary>
        /// Creates a node with the given path.
        /// </summary>
        /// <param name="path">Node Path</param>
        /// <param name="data">Data to associate with the node</param>
        /// <param name="acl">Access Control List</param>
        /// <param name="createMode">Specifies the node will be created</param>
        /// <returns>Task that will resolve on success to stat of the newly created node</returns>
        public Task<IStat> CreateAndGetStat(string path, byte[] data, IReadOnlyList<Acl> acl, CreateMode createMode)
        {
            RingMasterClientEventSource.Log.Create(path);

            return this.requestHandler.CreateAndGetStat(path, data, acl, createMode);
        }

        /// <summary>
        /// Deletes the node with the given path.
        /// </summary>
        /// <param name="path">Node path</param>
        /// <param name="version">Node will be deleted only if this value matches the current version of the node</param>
        /// <param name="mode">The deletion mode</param>
        /// <returns>Task that will resolve on success to either <c>true</c> if the node
        /// was successfully deleted or<c>false</c> if no node was found at that path.</returns>
        [SuppressMessage("Microsoft.MSInternal", "CA908:AvoidTypesThatRequireJitCompilationInPrecompiledAssemblies", Justification = "We are not using ngen")]
        public Task<bool> Delete(string path, int version, DeleteMode mode = DeleteMode.None)
        {
            RingMasterClientEventSource.Log.Delete(path, version, (mode & DeleteMode.CascadeDelete) == DeleteMode.CascadeDelete);

            return this.requestHandler.Delete(path, version, mode);
        }

        /// <summary>
        /// Queries the <see cref="Stat"/> of the node with the given path.
        /// </summary>
        /// <param name="path">Node path</param>
        /// <param name="watcher">Watcher interface that receives notifications for changes to this path or null</param>
        /// <param name="ignoreNonodeError">If set to <c>true</c> an exception is not thrown if no node is found at the given path</param>
        /// <returns>Task that will resolve on success to the <see cref="Stat"/> associated with the node</returns>
        public Task<IStat> Exists(string path, IWatcher watcher, bool ignoreNonodeError)
        {
            RingMasterClientEventSource.Log.Exists(path, watcher != null);

            return this.requestHandler.Exists(path, watcher, ignoreNonodeError);
        }

        /// <summary>
        /// Queries the <see cref="Stat"/> of the node with the given path.
        /// </summary>
        /// <param name="path">Node path</param>
        /// <param name="watcher">Watcher interface that receives notifications for changes to this path or null</param>
        /// <returns>Task that will resolve on success to the <see cref="Stat"/> associated with the node</returns>
        public Task<IStat> Exists(string path, IWatcher watcher)
        {
            return this.Exists(path, watcher, ignoreNonodeError: false);
        }

        /// <summary>
        /// Queries the <see cref="Stat"/> of the node with the given path.
        /// </summary>
        /// <param name="path">Node path</param>
        /// <param name="watch">If <c>true</c> watch for changes to this path</param>
        /// <param name="ignoreNonodeError">If set to <c>true</c> an exception is not thrown if no node is found at the given path</param>
        /// <returns>Task that will resolve on success to the <see cref="Stat"/> associated with the node</returns>
        public Task<IStat> Exists(string path, bool watch, bool ignoreNonodeError = false)
        {
            return this.Exists(path, watch ? this.watcher : null, ignoreNonodeError);
        }

        /// <summary>
        /// Gets the list of children of the node at the given path.
        /// </summary>
        /// <param name="path">Node path</param>
        /// <param name="watcher">Watcher interface that receives notifications for changes to this path or null</param>
        /// <param name="retrievalCondition">If not null, the retrieval condition in the form >:[top]:[startingChildName].
        /// valid interval definitions:
        /// <c>
        ///   ">:[Top]:[ChildName]"     ... returns the elements greater than the [ChildName] limited to Top count
        ///                                 so ">:1000:contoso" means give me first 1000 childrens greater than contoso
        ///                                 so ">:1000:"        means give me first 1000 elements
        /// </c>
        /// </param>
        /// <returns>Task that will resolve on success to the list of names of children of the node</returns>
        public Task<IReadOnlyList<string>> GetChildren(string path, IWatcher watcher, string retrievalCondition = null)
        {
            RingMasterClientEventSource.Log.GetChildren(path, watcher != null, retrievalCondition);

            return this.requestHandler.GetChildren(path, watcher, retrievalCondition);
        }

        /// <summary>
        /// Gets the list of children of the node at the given path.
        /// </summary>
        /// <param name="path">Node path</param>
        /// <param name="watch">If <c>true</c> watch for changes to this path</param>
        /// <param name="retrievalCondition">Retrieval condition</param>
        /// <returns>Task that will resolve on success to the list of names of children of the node</returns>
        public Task<IReadOnlyList<string>> GetChildren(string path, bool watch, string retrievalCondition = null)
        {
            return this.GetChildren(path, watch ? this.watcher : null, retrievalCondition);
        }

        /// <summary>
        /// Gets the data associated with the node at the given path.
        /// </summary>
        /// <param name="path">Node path</param>
        /// <param name="watcher">Watcher interface that receives notifications for changes to this path or null</param>
        /// <returns>Task that will resolve on success to the data associated with the node</returns>
        public Task<byte[]> GetData(string path, IWatcher watcher)
        {
            RingMasterClientEventSource.Log.GetData(path, watcher != null);

            return this.requestHandler.GetData(path, watcher);
        }

        /// <summary>
        /// Gets the data and Stat associated with the node at the given path.
        /// </summary>
        /// <param name="path">Node path</param>
        /// <param name="watcher">Watcher interface that receives notifications for changes to this path or null</param>
        /// <returns>Task that will resolve on success to the data associated with the node and stat</returns>
        public Task<Tuple<IStat, byte[]>> GetDataWithStat(string path, IWatcher watcher)
        {
            RingMasterClientEventSource.Log.GetDataWithStat(path, watcher != null);

            return this.requestHandler.GetDataWithStat(path, watcher);
        }

        /// <summary>
        /// Gets the data associated with the node at the given path.
        /// </summary>
        /// <param name="path">Node path</param>
        /// <param name="watch">If <c>true</c> watch for changes to this path</param>
        /// <returns>Task that will resolve on success to the data associated with the node</returns>
        public Task<byte[]> GetData(string path, bool watch)
        {
            return this.GetData(path, watch ? this.watcher : null);
        }

        /// <summary>
        /// Sets the data for the node at the given path if the given version matches
        /// the current version of the node (If the given version is -1, it matches any version).
        /// </summary>
        /// <param name="path">Node path</param>
        /// <param name="data">Data to associate with the node</param>
        /// <param name="version">Version to compare with the current version of the node</param>
        /// <returns>Task that will resolve on success to the <see cref="Stat"/> associated with the node</returns>
        public Task<IStat> SetData(string path, byte[] data, int version)
        {
            RingMasterClientEventSource.Log.SetData(path, version);

            return this.requestHandler.SetData(path, data, version);
        }

        /// <summary>
        /// Gets the Access Control List associated with a node.
        /// </summary>
        /// <param name="path">Node path</param>
        /// <param name="stat"><see cref="Stat"/> associated with the node</param>
        /// <returns>Task that will resolve on success to a List of <see cref="Acl"/>s associated
        /// with the node</returns>
        public Task<IReadOnlyList<Acl>> GetACL(string path, IStat stat)
        {
            RingMasterClientEventSource.Log.GetAcl(path);

            return this.requestHandler.GetACL(path, stat);
        }

        /// <summary>
        /// Sets the access control list for the node at the given path if
        /// the given version matches the current version of the node.
        /// </summary>
        /// <param name="path">Node path</param>
        /// <param name="acl">Access control list to associate with the node</param>
        /// <param name="version">Version to compare with the current version of the node</param>
        /// <returns>Task that will resolve on success to the <see cref="Stat"/> associated with
        /// the node</returns>
        public Task<IStat> SetACL(string path, IReadOnlyList<Acl> acl, int version)
        {
            RingMasterClientEventSource.Log.SetAcl(path, version);

            return this.requestHandler.SetACL(path, acl, version);
        }

        /// <summary>
        /// Synchronizes with the given path.
        /// </summary>
        /// <param name="path">Node path</param>
        /// <returns>Task that tracks the execution of this method</returns>
        public Task Sync(string path)
        {
            RingMasterClientEventSource.Log.Sync(path);

            return this.requestHandler.Sync(path);
        }

        /// <summary>
        /// Executes multiple operations as an atomic group at the server. Either the whole list takes
        /// effect, or no operation do.
        /// </summary>
        /// <param name="operations">List of operations</param>
        /// <param name="mustCompleteSynchronously">If <c>true</c> the server does not complete the operation
        /// until changes are guaranteed to be durable (and are applied locally).</param>
        /// <returns>Task that will resolve on success to a list of
        /// <see cref="OpResult"/>s</returns>
        public Task<IReadOnlyList<OpResult>> Multi(IReadOnlyList<Op> operations, bool mustCompleteSynchronously = false)
        {
            if (operations == null)
            {
                throw new ArgumentNullException("operations");
            }

            RingMasterClientEventSource.Log.Multi(operations.Count, mustCompleteSynchronously);

            return this.requestHandler.Multi(operations, mustCompleteSynchronously);
        }

        /// <summary>
        /// Executes multiple operations in a sequence at the server. No atomicity guarantees are provided.
        /// </summary>
        /// <param name="operations">List of operations</param>
        /// <param name="mustCompleteSynchronously">If <c>true</c> the server does not complete the operation
        /// until all successful operations are guaranteed to be durable (and are applied locally).</param>
        /// <returns>Task that will resolve on success to a list of <see cref="OpResult"/>s</returns>
        public Task<IReadOnlyList<OpResult>> Batch(IReadOnlyList<Op> operations, bool mustCompleteSynchronously = false)
        {
            if (operations == null)
            {
                throw new ArgumentNullException("operations");
            }

            RingMasterClientEventSource.Log.Batch(operations.Count, mustCompleteSynchronously);

            return this.requestHandler.Batch(operations, mustCompleteSynchronously);
        }

        /// <summary>
        /// Sets the client authentication digest
        /// </summary>
        /// <param name="clientId">The client id</param>
        /// <returns>Task that tracks the execution of this method</returns>
        public Task SetAuth(Id clientId)
        {
            return this.requestHandler.SetAuth(clientId);
        }

        /// <summary>
        /// Process the given <see cref="IRingMasterRequest"/>
        /// </summary>
        /// <param name="request">Request to send</param>
        /// <returns>A task that resolves to the response sent by the server</returns>
        public Task<RequestResponse> Request(IRingMasterRequest request)
        {
            return this.requestHandler.Request(request);
        }

        /// <summary>
        /// Close this client.
        /// </summary>
        public void Close()
        {
            RingMasterClientEventSource.Log.Close();
            this.requestHandler.Close();
        }

        /// <summary>
        /// Dispose this client.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private static ServerSpec CreateServerSpec(string connectionString, X509Certificate[] clientCerts = null, X509Certificate[] serverCerts = null)
        {
            var server = new ServerSpec();
            server.Endpoints = SecureTransport.ParseConnectionString(connectionString);
            server.UseSecureConnection = (clientCerts != null) && (serverCerts != null) && (clientCerts.Length > 0) && (serverCerts.Length > 0);

            if (server.UseSecureConnection)
            {
                server.ClientCertificate = clientCerts[0];
                server.AcceptedServerCertificates = serverCerts;
            }

            return server;
        }

        private void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                this.requestHandler.Dispose();
            }
        }

        /// <summary>
        /// Server endpoints and certificates.
        /// </summary>
        public class ServerSpec
        {
            /// <summary>
            /// Gets or sets the endpoints to use to connect to the server.
            /// </summary>
            public IPEndPoint[] Endpoints { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether a secure connection must be established.
            /// </summary>
            public bool UseSecureConnection { get; set; } = false;

            /// <summary>
            /// Gets or sets the certificate that the client will use to identify itself to the server.
            /// </summary>
            public X509Certificate ClientCertificate { get; set; } = null;

            /// <summary>
            /// Gets or sets the list of certificates that the server can present.
            /// </summary>
            public X509Certificate[] AcceptedServerCertificates { get; set; } = null;

            /// <summary>
            /// Gets or sets delegate responsible for validating the certificate supplied by the server.
            /// </summary>
            public RemoteCertificateValidationCallback ServerCertificateValidationCallback { get; set; } = null;

            /// <summary>
            /// Gets or sets the delegate responsible for selecting the certificate used for authentication.
            /// </summary>
            public LocalCertificateSelectionCallback ClientCertificationSelectionCallback { get; set; } = null;

            /// <summary>
            /// Gets or sets a value indicating whether the certificate revocation list must be checked during authentication.
            /// </summary>
            public bool MustCheckCertificateRevocation { get; set; } = true;

            /// <summary>
            /// Gets or sets a value indicating whether the certificate trust chain must be validated.
            /// </summary>
            public bool MustCheckCertificateTrustChain { get; set; } = true;

            /// <summary>
            /// Gets or sets a value indicating the subject allowed for certificate validation A
            /// </summary>
            public string CertValidationASubject { get; set; }

            /// <summary>
            /// Gets or sets a value indicating the thumbprints allowed for certificate validation A
            /// </summary>
            public IReadOnlyList<string> CertValidationASigningThumbprints { get; set; }

            /// <summary>
            /// Gets or sets a value indicating the subject allowed for certificate validation B
            /// </summary>
            public string CertValidationBSubject { get; set; }

            /// <summary>
            /// Gets or sets a value indicating the thumbprints allowed for certificate validation A
            /// </summary>
            public IReadOnlyList<string> CertValidationBSigningThumbprints { get; set; }

            /// <summary>
            /// Gets or sets the protocol version used for communication.
            /// </summary>
            public uint CommunicationProtocolVersion { get; set; } = RingMasterCommunicationProtocol.MaximumSupportedVersion;
        }

        /// <summary>
        /// RingMasterClient Configuration Settings
        /// </summary>
        public class Configuration
        {
            /// <summary>
            /// Gets or sets the default timeout for requests.
            /// </summary>
            public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMilliseconds(10000);

            /// <summary>
            /// Gets or sets the frequency with which heartbeats are sent.
            /// </summary>
            public TimeSpan HeartBeatInterval { get; set; } = TimeSpan.FromMilliseconds(30000);

            /// <summary>
            /// Gets or sets the length of the request queue.
            /// </summary>
            public int RequestQueueLength { get; set; } = 1000;

            /// <summary>
            /// Gets or sets a value indicating whether the backend must use locking even for read-only operations.
            /// </summary>
            public bool RequireLockForReadOnlyOperations { get; set; } = true;

            /// <summary>
            /// Gets or sets a value indicating whether the backend must transparently forward requests to the current primary if the
            /// target server is not the primary.
            /// </summary>
            public bool MustTransparentlyForwardRequests { get; set; } = true;

            /// <summary>
            /// Gets or sets the network buffer size.
            /// </summary>
            public int BufferSize { get; set; } = 65536;
        }

        private sealed class RingMasterRequestHandlerInstrumentation : RingMasterRequestHandler.IInstrumentation
        {
            private readonly IRingMasterClientInstrumentation instrumentation;

            public RingMasterRequestHandlerInstrumentation(IRingMasterClientInstrumentation instrumentation)
            {
                this.instrumentation = instrumentation;
            }

            public void ConnectionClosed(ulong connectionId, EndPoint remoteEndPoint, string remoteIdentity)
            {
                this.instrumentation?.ConnectionClosed(connectionId, remoteEndPoint, remoteIdentity);
            }

            public void ConnectionCreated(ulong connectionId, EndPoint remoteEndPoint, string remoteIdentity)
            {
                this.instrumentation?.ConnectionCreated(connectionId, remoteEndPoint, remoteIdentity);
            }

            public void RequestAborted(ulong requestId, RingMasterRequestType requestType)
            {
                this.instrumentation?.RequestAborted(requestId, requestType);
            }

            public void RequestQueued(ulong requestId, RingMasterRequestType requestType, int pendingRequestCount)
            {
                this.instrumentation?.RequestQueued(requestId, requestType, pendingRequestCount);
            }

            public void RequestQueueFull(ulong requestId, RingMasterRequestType requestType, int pendingRequestCount)
            {
                this.instrumentation?.RequestQueueFull(requestId, requestType, pendingRequestCount);
            }

            public void RequestSent(ulong requestId, RingMasterRequestType requestType, int requestLength)
            {
                this.instrumentation?.RequestSent(requestId, requestType, requestLength);
            }

            public void RequestSendFailed(ulong requestId, RingMasterRequestType requestType)
            {
                this.instrumentation?.RequestSendFailed(requestId, requestType);
            }

            public void RequestTimedOut(ulong requestId, RingMasterRequestType requestType, TimeSpan elapsed)
            {
                this.instrumentation?.RequestTimedOut(requestId, requestType, elapsed);
            }

            public void ResponseQueued(int responseLength)
            {
                this.instrumentation?.ResponseQueued(responseLength);
            }

            public void ResponseProcessed(ulong requestId, RingMasterRequestType requestType, int resultCode, TimeSpan elapsed)
            {
                this.instrumentation?.ResponseProcessed(requestId, requestType, resultCode, elapsed);
            }

            public void HeartBeatSent(ulong heartBeatId)
            {
                this.instrumentation?.HeartBeatSent(heartBeatId);
            }

            public void WatcherNotificationReceived(WatchedEvent.WatchedEventType eventType)
            {
                this.instrumentation?.WatcherNotificationReceived(eventType);
            }

            public void WatcherNotFound()
            {
                this.instrumentation?.WatcherNotFound();
            }

            public void InvalidPacketReceived()
            {
                this.instrumentation?.InvalidPacketReceived();
            }

            public void UnexpectedResponseReceived(ulong callId)
            {
                this.instrumentation?.UnexpectedResponseReceived(callId);
            }

            public void InvalidClientMessageReceived()
            {
                this.instrumentation?.InvalidClientMessageReceived();
            }
        }
    }
}
