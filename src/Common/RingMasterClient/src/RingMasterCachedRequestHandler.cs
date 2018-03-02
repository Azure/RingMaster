// <copyright file="RingMasterCachedRequestHandler.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// RingMasterCachedRequestHandler - Manages cached connection with the server and handles sending IRingMasterRequest
    /// and receiving RingMasterResponse
    /// </summary>
    internal sealed class RingMasterCachedRequestHandler : IRingMasterRequestHandler
    {
        /// <summary>
        /// The base handler to use
        /// </summary>
        private IRingMasterRequestHandler baseHandler;

        /// <summary>
        /// The cache object to use for this client
        /// </summary>
        private IRingMasterClientCache cache = null;

        /// <summary>
        /// What was the last cache that we tried to setup
        /// </summary>
        private IRingMasterClientCache cacheInflight = null;

        /// <summary>
        /// The object to lock when modifying the cache object
        /// </summary>
        private object cacheLockobj = new object();

        /// <summary>
        /// The prefix this ringmaster instance needs to use within the cache
        /// </summary>
        private string cachePrefix = string.Empty;

        /// <summary>
        /// The action to be invoked only when the bulkwatcher is removed at the end of the cache existence
        /// </summary>
        private Action onBulkWatcherRemoved = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterCachedRequestHandler"/> class.
        /// </summary>
        /// <param name="baseHandler">The base handler to use</param>
        public RingMasterCachedRequestHandler(IRingMasterRequestHandler baseHandler)
        {
            if (baseHandler == null)
            {
                throw new ArgumentNullException("baseHandler");
            }

            this.onBulkWatcherRemoved = null;
            this.baseHandler = baseHandler;
        }

        /// <summary>
        /// Gets or sets the number of milliseconds to wait before a request is timed out.
        /// </summary>
        /// <value>The timeout.</value>
        public int Timeout
        {
            get
            {
                return this.baseHandler.Timeout;
            }

            set
            {
                this.baseHandler.Timeout = value;
            }
        }

        /// <summary>
        /// Enqueue a request.
        /// </summary>
        /// <param name="request">Request to enqueue</param>
        /// <returns>A task that resolves to the response sent by the server</returns>
        public async Task<RequestResponse> Request(IRingMasterRequest request)
        {
            bool haswatcher = false;

            switch (request.RequestType)
            {
                case RingMasterRequestType.Exists:
                    {
                        var existsRequest = (RequestExists)request;
                        haswatcher = existsRequest.Watcher != null;
                        break;
                    }

                case RingMasterRequestType.GetData:
                    {
                        var getDataRequest = (RequestGetData)request;
                        haswatcher = getDataRequest.Watcher != null;
                        break;
                    }

                case RingMasterRequestType.GetChildren:
                    {
                        var getChildrenRequest = (RequestGetChildren)request;
                        haswatcher = getChildrenRequest.Watcher != null;
                        break;
                    }
            }

            if (!haswatcher)
            {
                return await this.ProcessCacheRequest(this.cache, request);
            }

            return await this.baseHandler.Request(request);
        }

        /// <summary>
        /// Abandons this instance's use of the basehandler, assuming no cache was ever set.
        /// </summary>
        public void Abandon()
        {
            if (this.cache != null)
            {
                throw new InvalidOperationException("cannot abandon an object with a cache");
            }

            this.baseHandler = null;
        }

        /// <summary>
        /// Close this request handler.
        /// </summary>
        public void Close()
        {
            if (this.baseHandler != null)
            {
                ManualResetEvent ev = null;

                if (this.cache != null)
                {
                    ev = new ManualResetEvent(false);

                    this.onBulkWatcherRemoved = () =>
                    {
                        ev.Set();
                    };

                    this.SetCacheInstance(null, null, false, true).Wait();
                }

                this.baseHandler.Close();
                this.baseHandler = null;

                if (ev != null)
                {
                    bool timeout = !ev.WaitOne(15000);

                    if (timeout)
                    {
                        throw new TimeoutException("could not close the Handler in 15 seconds");
                    }
                }
            }
        }

        /// <summary>
        /// Dispose this request handler.
        /// </summary>
        public void Dispose()
        {
            this.Close();
            this.Dispose(true);
            GC.SuppressFinalize(this);
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
        public async Task<IRingMasterClientCache> SetCacheInstance(IRingMasterClientCache cache, string cachePrefix, bool auto_invalidate, bool allowReplacement)
        {
            if (this.baseHandler == null)
            {
                return null;
            }

            lock (this.cacheLockobj)
            {
                if (this.cacheInflight != this.cache)
                {
                    return this.cache;
                }

                if (this.cache == cache)
                {
                    return cache;
                }

                if (this.cache == null || allowReplacement)
                {
                    this.cachePrefix = cachePrefix;
                    this.cacheInflight = cache;
                }
            }

            bool set = await this.CacheStart(cache, auto_invalidate);

            if (!set)
            {
                return null;
            }

            return cache;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                if (this.baseHandler != null)
                {
                    this.baseHandler.Dispose();
                    this.baseHandler = null;
                }
            }
        }

        /// <summary>
        /// Starts the cache registration
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="auto_invalidate">if true, the cache gets invalidated from change notifications from RM service automatically</param>
        /// <returns>a task with the completion of the action, indicating true if the cache was properly set. False otherwise.</returns>
        private async Task<bool> CacheStart(IRingMasterClientCache cache, bool auto_invalidate)
        {
            Task<bool> registration = null;

            lock (this.cacheLockobj)
            {
                if (this.cacheInflight != cache)
                {
                    return false;
                }

                this.cache = null;

                this.cache = cache;

                if (auto_invalidate && cache != null)
                {
                    string cachePrefix = this.cachePrefix;
                    string path = string.IsNullOrEmpty(cachePrefix) ? "/" : cachePrefix;

                    registration = this.RegisterOnAnySubPathChange(this.Timeout, path, false, false, (RingMasterException.Code rc, WatchedEvent onChange) =>
                    {
                        bool reconnectNeeded = cache.NotifyWatcherEvent(rc, onChange, cachePrefix);

                        if (!reconnectNeeded)
                        {
                            return;
                        }

                        if (this.onBulkWatcherRemoved != null)
                        {
                            this.onBulkWatcherRemoved();
                            return;
                        }

                        if (this.cache != cache)
                        {
                            return;
                        }

                        Task.Delay(1000).ContinueWith(t =>
                        {
                            return this.CacheStart(cache, auto_invalidate);
                        });
                    });
                }
            }

            if (registration != null)
            {
                return await registration;
            }

            return true;
        }

        private void CacheInvalidate(IRingMasterClientCache cache, IRingMasterRequest req)
        {
            if (cache == null || req == null)
            {
                return;
            }

            switch (req.RequestType)
            {
                case RingMasterRequestType.GetData:
                case RingMasterRequestType.GetChildren:
                case RingMasterRequestType.GetAcl:
                case RingMasterRequestType.Exists:
                    return;
                case RingMasterRequestType.Create:
                    {
                        cache.Invalidate(this.cachePrefix, req.Path);
                        cache.Invalidate(this.cachePrefix, PrefixedClientCache.GetParent(req.Path));
                        break;
                    }

                case RingMasterRequestType.Delete:
                    {
                        RequestDelete delete = req as RequestDelete;

                        if (delete.IsCascade)
                        {
                            cache.Wipe(this.cachePrefix);
                        }
                        else
                        {
                            cache.Invalidate(this.cachePrefix, req.Path);
                            cache.Invalidate(this.cachePrefix, PrefixedClientCache.GetParent(req.Path));
                        }

                        break;
                    }

                case RingMasterRequestType.Move:
                    {
                        RequestMove move = req as RequestMove;

                        cache.Invalidate(this.cachePrefix, move.Path);
                        cache.Invalidate(this.cachePrefix, PrefixedClientCache.GetParent(move.Path));
                        cache.Invalidate(this.cachePrefix, PrefixedClientCache.GetParent(move.PathDst));
                        break;
                    }

                default:
                    {
                        cache.Invalidate(this.cachePrefix, req.Path);

                        AbstractRingMasterCompoundRequest list = req as AbstractRingMasterCompoundRequest;

                        if (list != null && list.Requests != null)
                        {
                            foreach (IRingMasterRequest child in list.Requests)
                            {
                                this.CacheInvalidate(cache, child);
                            }
                        }

                        break;
                    }
            }
        }

        /// <summary>
        /// Gets the response from the cache.
        /// </summary>
        /// <param name="cache">The cache to use.</param>
        /// <param name="req">The request provided.</param>
        /// <returns>the response if cached, or null if not found</returns>
        private RequestResponse GetCachedResponse(IRingMasterClientCache cache, IRingMasterRequest req)
        {
            if (cache == null || req == null)
            {
                return null;
            }

            switch (req.RequestType)
            {
                case RingMasterRequestType.GetData:
                    {
                        IRingMasterClientCacheDataEntry data;

                        if (this.cache.TryGetInfo(this.cachePrefix, req.Path, CachedKind.NodeData | CachedKind.NodeStats, out data))
                        {
                            RequestResponse response = new RequestResponse()
                            {
                                CallId = 0,
                                ResponsePath = req.Path,
                                ResultCode = (int)RingMasterException.Code.Ok,
                                Content = data.Data,
                                Stat = data.Stat
                            };

                            return response;
                        }

                        break;
                    }

                case RingMasterRequestType.GetChildren:
                    {
                        IRingMasterClientCacheDataEntry data;

                        if (!string.IsNullOrEmpty(((RequestGetChildren)req).RetrievalCondition))
                        {
                            break;
                        }

                        if (this.cache.TryGetInfo(this.cachePrefix, req.Path, CachedKind.NodeChildren | CachedKind.NodeStats, out data))
                        {
                            RequestResponse response = new RequestResponse()
                            {
                                CallId = 0,
                                ResponsePath = req.Path,
                                ResultCode = (int)RingMasterException.Code.Ok,
                                Content = data.Children,
                                Stat = data.Stat
                            };

                            return response;
                        }

                        break;
                    }

                case RingMasterRequestType.GetAcl:
                    {
                        IRingMasterClientCacheDataEntry data;

                        if (this.cache.TryGetInfo(this.cachePrefix, req.Path, CachedKind.NodeAcls | CachedKind.NodeStats, out data))
                        {
                            RequestResponse response = new RequestResponse()
                            {
                                CallId = 0,
                                ResponsePath = req.Path,
                                ResultCode = (int)RingMasterException.Code.Ok,
                                Content = data.Acls,
                                Stat = data.Stat
                            };

                            return response;
                        }

                        break;
                    }

                case RingMasterRequestType.Exists:
                    {
                        IRingMasterClientCacheDataEntry data;

                        if (this.cache.TryGetInfo(this.cachePrefix, req.Path, CachedKind.NodeStats, out data))
                        {
                            RequestResponse response = new RequestResponse()
                            {
                                CallId = 0,
                                ResponsePath = req.Path,
                                ResultCode = (int)RingMasterException.Code.Ok,
                                Content = data.Stat,
                                Stat = data.Stat
                            };

                            return response;
                        }

                        break;
                    }

                default:
                    {
                        this.CacheInvalidate(cache, req);
                        break;
                    }
            }

            return null;
        }

        /// <summary>
        /// Processes the request in regards to the given cache object.
        /// </summary>
        /// <param name="cache">The cache object to use.</param>
        /// <param name="req">The request to process.</param>
        /// <returns><c>a response</c> if the processing was completed, <c>null</c> otherwise.</returns>
        private async Task<RequestResponse> ProcessCacheRequest(IRingMasterClientCache cache, IRingMasterRequest req)
        {
            if (cache != null && req != null)
            {
                RequestResponse cachedResponse = this.GetCachedResponse(cache, req);

                if (cachedResponse != null)
                {
                    return cachedResponse;
                }

                switch (req.RequestType)
                {
                    case RingMasterRequestType.GetData:
                        {
                            RequestResponse resp = await this.baseHandler.Request(req);

                            string prefix = this.cachePrefix;

                            if (prefix != null)
                            {
                                cache.SetInfo(prefix, req.Path, CachedKind.NodeData | CachedKind.NodeStats, new PrefixedClientCache.DataEntry() { Data = (byte[])resp.Content, Stat = resp.Stat });
                            }

                            return resp;
                        }

                    case RingMasterRequestType.GetChildren:
                        {
                            RequestResponse resp = await this.baseHandler.Request(req);

                            string prefix = this.cachePrefix;

                            if (prefix != null)
                            {
                                RequestGetChildren gchil = req as RequestGetChildren;

                                if (string.IsNullOrEmpty(gchil.RetrievalCondition))
                                {
                                    cache.SetInfo(prefix, req.Path, CachedKind.NodeChildren | CachedKind.NodeStats, new PrefixedClientCache.DataEntry() { Children = (IReadOnlyList<string>)resp.Content, Stat = resp.Stat });
                                }
                            }

                            return resp;
                        }

                    case RingMasterRequestType.GetAcl:
                        {
                            RequestResponse resp = await this.baseHandler.Request(req);

                            string prefix = this.cachePrefix;

                            if (prefix != null)
                            {
                                cache.SetInfo(prefix, req.Path, CachedKind.NodeAcls | CachedKind.NodeStats, new PrefixedClientCache.DataEntry() { Acls = (IReadOnlyList<Acl>)resp.Content, Stat = resp.Stat });
                            }

                            return resp;
                        }

                    case RingMasterRequestType.Exists:
                        {
                            RequestResponse resp = await this.baseHandler.Request(req);

                            string prefix = this.cachePrefix;

                            if (prefix != null)
                            {
                                cache.SetInfo(prefix, req.Path, CachedKind.NodeStats, new PrefixedClientCache.DataEntry() { Stat = resp.Stat });
                            }

                            return resp;
                        }
                }
            }

            return await this.baseHandler.Request(req);
        }
    }
}