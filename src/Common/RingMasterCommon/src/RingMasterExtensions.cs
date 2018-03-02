// <copyright file="RingMasterExtensions.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using RingMaster.Data;
    using RingMaster.Requests;

    /// <summary>
    /// RingMaster Extensions provides extension methods for <see cref="IRingMasterRequestHandler"/>
    /// </summary>
    public static class RingMasterExtensions
    {
        /// <summary>
        /// Delegate to be used as a callback for the change notifications coming from a bulk watcher
        /// </summary>
        /// <param name="rc">The result code</param>
        /// <param name="onChange">The notification event.</param>
        public delegate void RegisterOnAnySubPathChangeDelegate(RingMasterException.Code rc, WatchedEvent onChange);

        /// <summary>
        /// Create a channel identified by the given time stream id.
        /// </summary>
        /// <param name="ringMaster">Interface to ringmaster</param>
        /// <param name="timeStreamId">Id of the time stream</param>
        /// <returns>A <see cref="IRingMasterRequestHandler"/> that stamps all requests with the given time stream id</returns>
        public static IRingMasterRequestHandler OpenTimeStream(this IRingMasterRequestHandler ringMaster, ulong timeStreamId)
        {
            return new RingMasterTimeStreamRequestHandler(timeStreamId, ringMaster);
        }

        /// <summary>
        /// Creates a node with the given path.
        /// </summary>
        /// <param name="ringMaster">Interface to ringmaster</param>
        /// <param name="path">Node Path</param>
        /// <param name="data">Data to associate with the node</param>
        /// <param name="acl">Access Control List</param>
        /// <param name="createMode">Specifies the node will be created</param>
        /// <param name="throwIfNodeExists">if true, and the error is <c>Nodeexists</c>, it generates an exception</param>
        /// <returns>Task that tracks completion of this method</returns>
        public static async Task Create(this IRingMasterRequestHandler ringMaster, string path, byte[] data, IReadOnlyList<Acl> acl, CreateMode createMode, bool throwIfNodeExists)
        {
            try
            {
                await Create(ringMaster, path, data, acl, createMode);
            }
            catch (RingMasterException ex)
            {
                if ((ex.ErrorCode != RingMasterException.Code.Nodeexists) || (throwIfNodeExists == true))
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Creates a node with the given path.
        /// </summary>
        /// <param name="ringMaster">Interface to ringmaster</param>
        /// <param name="path">Node Path</param>
        /// <param name="data">Data to associate with the node</param>
        /// <param name="acl">Access Control List</param>
        /// <param name="createMode">Specifies the node will be created</param>
        /// <returns>Task that will resolve on success to the path to the newly created node</returns>
        public static async Task<string> Create(this IRingMasterRequestHandler ringMaster, string path, byte[] data, IReadOnlyList<Acl> acl, CreateMode createMode)
        {
            RequestResponse response = await ringMaster.Request(
                new RequestCreate(
                    path,
                    data,
                    acl,
                    createMode));

            ThrowIfError(response);
            return (string)response.Content;
        }

        /// <summary>
        /// Moves a node with the given path.
        /// </summary>
        /// <param name="ringMaster">The ringmaster handler to operate on</param>
        /// <param name="pathSrc">Node Path to move</param>
        /// <param name="version">Version of the source node</param>
        /// <param name="pathDst">Node Path to be parent of the moved node</param>
        /// <param name="moveMode">Modifiers for the move operation</param>
        /// <returns>Task that will resolve on success to the path to the newly created node</returns>
        public static async Task<string> Move(this IRingMasterRequestHandler ringMaster, string pathSrc, int version, string pathDst, MoveMode moveMode)
        {
            RequestResponse response = await ringMaster.Request(
                new RequestMove(
                    pathSrc,
                    version,
                    pathDst,
                    moveMode));

            ThrowIfError(response);
            return (string)response.Content;
        }

        /// <summary>
        /// Creates a node with the given path.
        /// </summary>
        /// <param name="ringMaster">Interface to ringmaster</param>
        /// <param name="path">Node Path</param>
        /// <param name="data">Data to associate with the node</param>
        /// <param name="acl">Access Control List</param>
        /// <param name="createMode">Specifies the node will be created</param>
        /// <returns>Task that will resolve on success to stat of the newly created node</returns>
        public static async Task<IStat> CreateAndGetStat(this IRingMasterRequestHandler ringMaster, string path, byte[] data, IReadOnlyList<Acl> acl, CreateMode createMode)
        {
            RequestResponse response = await ringMaster.Request(
                new RequestCreate(
                    path,
                    data,
                    acl,
                    createMode));

            ThrowIfError(response);
            return response.Stat;
        }

        /// <summary>
        /// Deletes the node with the given path.
        /// </summary>
        /// <param name="ringMaster">Interface to ringmaster</param>
        /// <param name="path">Node path</param>
        /// <param name="version">Node will be deleted only if this 
        /// value matches the current version of the node</param>
        /// <param name="isRecursive">If set to <c>true</c> a recursive delete is performed.</param>
        /// <returns>Task that will resolve on success to either <c>true</c> if the node 
        /// was successfully deleted or<c>false</c> if no node was found at that path.</returns>
        [SuppressMessage("Microsoft.MSInternal", "CA908:AvoidTypesThatRequireJitCompilationInPrecompiledAssemblies", Justification = "We are not using ngen")]
        public static async Task<bool> Delete(this IRingMasterRequestHandler ringMaster, string path, int version, bool isRecursive)
        {
            return await ringMaster.Delete(path, version, isRecursive ? DeleteMode.CascadeDelete : DeleteMode.None);
        }

        /// <summary>
        /// Deletes the node with the given path.
        /// </summary>
        /// <param name="ringMaster">Interface to ringmaster</param>
        /// <param name="path">Node path</param>
        /// <param name="version">Node will be deleted only if this 
        /// value matches the current version of the node</param>
        /// <param name="deletemode">delete mode of the operation</param>
        /// <returns>Task that will resolve on success to either <c>true</c> if the node 
        /// was successfully deleted or<c>false</c> if no node was found at that path.</returns>
        [SuppressMessage("Microsoft.MSInternal", "CA908:AvoidTypesThatRequireJitCompilationInPrecompiledAssemblies", Justification = "We are not using ngen")]
        public static async Task<bool> Delete(this IRingMasterRequestHandler ringMaster, string path, int version, DeleteMode deletemode = DeleteMode.None)
        {
            RequestResponse response = await ringMaster.Request(
                new RequestDelete(
                    path,
                    version,
                    deletemode));

            switch (RingMasterException.GetCode(response.ResultCode))
            {
                case RingMasterException.Code.Ok: return true;
                case RingMasterException.Code.Nonode: return false;
                default:
                    break;
            }

            ThrowIfError(response);
            return false;
        }

        /// <summary>
        /// Queries the <see cref="Stat"/> of the node with the given path.
        /// </summary>
        /// <param name="ringMaster">Interface to ringmaster</param>
        /// <param name="path">Node path</param>
        /// <param name="watcher">Watcher interface that receives notifications for changes to this path or null</param>
        /// <param name="ignoreNonodeError">If set to <c>true</c> an exception is not thrown if no node is found at the given path</param>
        /// <returns>Task that will resolve on success to the <see cref="Stat"/> associated with the node</returns>
        public static async Task<IStat> Exists(this IRingMasterRequestHandler ringMaster, string path, IWatcher watcher, bool ignoreNonodeError)
        {
            RequestResponse response = await ringMaster.Request(
                new RequestExists(
                    path,
                    watcher));

            if (ignoreNonodeError 
            && (RingMasterException.GetCode(response.ResultCode) == RingMasterException.Code.Nonode))
            {
                return response.Stat;
            }

            ThrowIfError(response);
            return (IStat)response.Content;
        }

        /// <summary>
        /// Queries the <see cref="Stat"/> of the node with the given path.
        /// </summary>
        /// <param name="ringMaster">Interface to ringmaster</param>
        /// <param name="path">Node path</param>
        /// <param name="watcher">Watcher interface that receives notifications for changes to this path or null</param>
        /// <returns>Task that will resolve on success to the <see cref="Stat"/> associated with the node</returns>
        public static Task<IStat> Exists(this IRingMasterRequestHandler ringMaster, string path, IWatcher watcher)
        {
            return ringMaster.Exists(path, watcher, ignoreNonodeError: false);
        }

        /// <summary>
        /// Gets the list of children of the node at the given path.
        /// </summary>
        /// <param name="ringMaster">Interface to ringmaster</param>
        /// <param name="path">Node path</param>
        /// <param name="watcher">Watcher interface that receives notifications for changes to this path or null</param>
        /// <param name="retrievalCondition">If not null, the retrieval condition in the form >:[top]:[startingChildName].
        /// <c>
        ///   ">:[Top]:[ChildName]"     ... returns the elements greater than the [ChildName] limited to Top count
        ///                                 so ">:1000:contoso" means give me first 1000 childrens greater than contoso
        ///                                 so ">:1000:"        means give me first 1000 elements
        /// </c>
        /// </param>
        /// <returns>Task that will resolve on success to the list of names of children of the node</returns>
        public static async Task<IReadOnlyList<string>> GetChildren(this IRingMasterRequestHandler ringMaster, string path, IWatcher watcher, string retrievalCondition = null)
        {
            RequestResponse response = await ringMaster.Request(
                new RequestGetChildren(
                    path,
                    watcher,
                    retrievalCondition: retrievalCondition));

            ThrowIfError(response);
            return (IReadOnlyList<string>)response.Content;
        }

        /// <summary>
        /// Gets the data associated with the node at the given path.
        /// </summary>
        /// <param name="ringMaster">Interface to ringmaster</param>
        /// <param name="path">Node path</param>
        /// <param name="watcher">Watcher interface that receives notifications for changes to this path or null</param>
        /// <returns>Task that will resolve on success to the data associated with the node</returns>
        public static Task<byte[]> GetData(this IRingMasterRequestHandler ringMaster, string path, IWatcher watcher)
        {
            return RingMasterExtensions.GetData(ringMaster, path, options: RequestGetData.GetDataOptions.None, optionArgument: null, watcher: watcher);
        }

        /// <summary>
        /// Gets the data associated with the node at the given path.
        /// </summary>
        /// <param name="ringMaster">Interface to ringmaster</param>
        /// <param name="path">Node path</param>
        /// <param name="options">Options for this request</param>
        /// <param name="optionArgument">Argument for options</param>
        /// <param name="watcher">Watcher interface that receives notifications for changes to this path or null</param>
        /// <returns>Task that will resolve on success to the data associated with the node</returns>
        public static async Task<byte[]> GetData(
            this IRingMasterRequestHandler ringMaster,
            string path,
            RequestGetData.GetDataOptions options,
            RequestGetData.IGetDataOptionArgument optionArgument,
            IWatcher watcher)
        {
            RequestResponse response = await ringMaster.Request(
                new RequestGetData(
                    path,
                    options,
                    optionArgument,
                    watcher: watcher));

            ThrowIfError(response);
            return (byte[])response.Content;
        }

        /// <summary>
        /// Gets the data and stat associated with the node at the given path.
        /// </summary>
        /// <param name="ringMaster">Interface to ringmaster</param>
        /// <param name="path">Node path</param>
        /// <param name="watcher">Watcher interface that receives notifications for changes to this path or null</param>
        /// <returns>Task that will resolve on success to the data associated with the node</returns>
        public static async Task<Tuple<IStat, byte[]>> GetDataWithStat(this IRingMasterRequestHandler ringMaster, string path, IWatcher watcher)
        {
            RequestResponse response = await ringMaster.Request(
                new RequestGetData(
                    path,
                    RequestGetData.GetDataOptions.None,
                    watcher,
                    0));

            ThrowIfError(response);
            return new Tuple<IStat, byte[]>(response.Stat, (byte[])response.Content);
        }

        /// <summary>
        /// Gets the full sub tree under the given path.
        /// </summary>
        /// <param name="ringMaster">Interface to ringmaster</param>
        /// <param name="path">Node path</param>
        /// <param name="withStat">If the stat of the node should be returned</param>
        /// <returns>Task that will resolve on success to the root of the sub tree under the given path</returns>
        public static async Task<TreeNode> GetFullSubtree(this IRingMasterRequestHandler ringMaster, string path, bool withStat = false)
        {
            return TreeNode.Deserialize(await ringMaster.GetData(PathDecoration.GetFullContentPath(path, withStat), watcher: null));
        }

        /// <summary>
        /// Sets the data for the node at the given path if the given version matches
        /// the current version of the node (If the given version is -1, it matches any version).
        /// </summary>
        /// <param name="ringMaster">Interface to ringmaster</param>
        /// <param name="path">Node path</param>
        /// <param name="data">Data to associate with the node</param>
        /// <param name="version">Version to compare with the current version of the node</param>
        /// <returns>Task that will resolve on success to the <see cref="Stat"/> associated with the node</returns>
        public static async Task<IStat> SetData(this IRingMasterRequestHandler ringMaster, string path, byte[] data, int version)
        {
            RequestResponse response = await ringMaster.Request(
                new RequestSetData(
                    path,
                    data,
                    version,
                    dataCommand: false));

            ThrowIfError(response);
            return response.Stat;
        }

        /// <summary>
        /// Gets the Access Control List associated with a node.
        /// </summary>
        /// <param name="ringMaster">Interface to ringmaster</param>
        /// <param name="path">Node path</param>
        /// <param name="stat"><see cref="Stat"/> associated with the node</param>
        /// <returns>Task that will resolve on success to a List of <see cref="Acl"/>s associated 
        /// with the node</returns>
        public static async Task<IReadOnlyList<Acl>> GetACL(this IRingMasterRequestHandler ringMaster, string path, IStat stat)
        {
            RequestResponse response = await ringMaster.Request(
                new RequestGetAcl(path, stat));

            ThrowIfError(response);
            return (IReadOnlyList<Acl>)response.Content;
        }

        /// <summary>
        /// Sets the access control list for the node at the given path if
        /// the given version matches the current version of the node.
        /// </summary>
        /// <param name="ringMaster">Interface to ringmaster</param>
        /// <param name="path">Node path</param>
        /// <param name="acl">Access control list to associate with the node</param>
        /// <param name="version">Version to compare with the current version of the node</param>
        /// <returns>Task that will resolve on success to the <see cref="Stat"/> associated with 
        /// the node</returns>
        public static async Task<IStat> SetACL(this IRingMasterRequestHandler ringMaster, string path, IReadOnlyList<Acl> acl, int version)
        {
            RequestResponse response = await ringMaster.Request(
                new RequestSetAcl(
                    path,
                    acl,
                    version));

            ThrowIfError(response);
            return response.Stat;
        }

        /// <summary>
        /// Synchronizes with the given path.
        /// </summary>
        /// <param name="ringMaster">Interface to ringmaster</param>
        /// <param name="path">Node path</param>
        /// <returns>Task that tracks the execution of this method</returns>
        public static async Task Sync(this IRingMasterRequestHandler ringMaster, string path)
        {
            RequestResponse response = await ringMaster.Request(
                new RequestSync(
                    path));

            ThrowIfError(response);
        }

        /// <summary>
        /// Executes multiple operations as an atomic group at the server. Either the whole list takes 
        /// effect, or no operation do.
        /// </summary>
        /// <param name="ringMaster">Interface to ringmaster</param>
        /// <param name="operations">List of operations</param>
        /// <param name="mustCompleteSynchronously">If <c>true</c> the server does not complete the operation
        /// until changes are guaranteed to be durable (and are applied locally).</param>
        /// <returns>Task that will resolve on success to a list of 
        /// <see cref="OpResult"/>s</returns>
        public static async Task<IReadOnlyList<OpResult>> Multi(this IRingMasterRequestHandler ringMaster, IReadOnlyList<Op> operations, bool mustCompleteSynchronously = false)
        {
            RequestResponse response = await ringMaster.Request(
                new RequestMulti(
                    operations,
                    mustCompleteSynchronously));

            ThrowIfError(response);
            return (IReadOnlyList<OpResult>)response.Content;
        }

        /// <summary>
        /// Executes multiple operations as an atomic group at the server. Either the whole list takes 
        /// effect, or no operation do.
        /// </summary>
        /// <param name="ringMaster">Interface to ringmaster</param>
        /// <param name="operations">List of operations</param>
        /// <param name="scheduledName">if not null, this multi will be scheduled for background execution</param>
        /// <param name="mustCompleteSynchronously">If <c>true</c> the server does not complete the operation
        /// until changes are guaranteed to be durable (and are applied locally).</param>
        /// <returns>Task that will resolve on success to a list of 
        /// <see cref="OpResult"/>s</returns>
        public static async Task<IReadOnlyList<OpResult>> Multi(this IRingMasterRequestHandler ringMaster, IReadOnlyList<Op> operations, string scheduledName, bool mustCompleteSynchronously = false)
        {
            RequestResponse response = await ringMaster.Request(
                new RequestMulti(
                    operations,
                    mustCompleteSynchronously, 
                    scheduledName));

            ThrowIfError(response);
            return (IReadOnlyList<OpResult>)response.Content;
        }

        /// <summary>
        /// Executes multiple operations in a sequence at the server. No atomicity guarantees are provided.
        /// </summary>
        /// <param name="ringMaster">Interface to ringmaster</param>
        /// <param name="operations">List of operations</param>
        /// <param name="mustCompleteSynchronously">If <c>true</c> the server does not complete the operation
        /// until all successful operations are guaranteed to be durable (and are applied locally).</param>
        /// <returns>Task that will resolve on success to a list of 
        /// <see cref="OpResult"/>s</returns>
        public static async Task<IReadOnlyList<OpResult>> Batch(this IRingMasterRequestHandler ringMaster, IReadOnlyList<Op> operations, bool mustCompleteSynchronously = false)
        {
            RequestResponse response = await ringMaster.Request(
                new RequestBatch(
                    operations,
                    mustCompleteSynchronously));

            ThrowIfError(response);
            return (IReadOnlyList<OpResult>)response.Content;
        }

        /// <summary>
        /// Gets the root node under which node trees are scheduled to be deleted.
        /// </summary>
        /// <returns>The scheduled delete root path</returns>
        public static string GetScheduledDeleteRoot()
        {
            return "/$tm/ScheduledDelete";
        }

        /// <summary>
        /// Schedule a delete operation for the given path.
        /// </summary>
        /// <param name="ringMaster">Interface to ringmaster</param>
        /// <param name="path">Path of the node to be deleted</param>
        /// <param name="version">Version of the node</param>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        public static async Task ScheduleDelete(this IRingMasterRequestHandler ringMaster, string path, int version)
        {
            string stagingLocation = $"{GetScheduledDeleteRoot()}/{Guid.NewGuid()}";
            await ringMaster.Move(path, version, stagingLocation, MoveMode.AllowPathCreationFlag);
        }

        /// <summary>
        /// Sets the client authentication digest
        /// </summary>
        /// <param name="ringMaster">Interface to ringmaster</param>
        /// <param name="clientId">The client id</param>
        /// <returns>Task that tracks the execution of this method</returns>
        public static async Task SetAuth(this IRingMasterRequestHandler ringMaster, Id clientId)
        {
            string clientIdentity = string.Format(
                CultureInfo.InvariantCulture,
                "{0}:{1}",
                clientId.Scheme,
                clientId.Identifier);
            RequestResponse response = await ringMaster.Request(new RequestSetAuth(clientIdentity));

            ThrowIfError(response);
        }

        /// <summary>
        /// Enumerates the children of the node at the given path without blocking.
        /// </summary>
        /// <param name="ringMaster">Interface to ringmaster</param>
        /// <param name="path">Node path</param>
        /// <param name="action">Action to execute for each child</param>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        /// <remarks>This method issues multiple GetChildren requests to the given <paramref name="ringMaster"/> if required to enumerate
        /// all the children of the given node. It will invoke the given action for each child</remarks>
        public static Task ForEachChild(this IRingMasterRequestHandler ringMaster, string path, Action<string> action)
        {
            return ForEachChild(ringMaster, path, 1000, action);
        }

        /// <summary>
        /// Enumerates the children of the node at the given path without blocking.
        /// </summary>
        /// <param name="ringMaster">Interface to ringmaster</param>
        /// <param name="path">Node path</param>
        /// <param name="maxChildrenPerRequest">Maximum number of children to retrieve with each GetChildren request</param>
        /// <param name="action">Action to execute for each child</param>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        /// <remarks>This method issues multiple GetChildren requests to the given <paramref name="ringMaster"/> if required to enumerate
        /// all the children of the given node. It will invoke the given action for each child</remarks>
        public static async Task ForEachChild(this IRingMasterRequestHandler ringMaster, string path, int maxChildrenPerRequest, Action<string> action)
        {
            string startingChildName = string.Empty;

            while (true)
            {
                IReadOnlyList<string> children = await ringMaster.GetChildren(
                    path,
                    watcher: null,
                    retrievalCondition: string.Format(">:{0}:{1}", maxChildrenPerRequest, startingChildName));

                foreach (var child in children)
                {
                    startingChildName = child;
                    action(child);
                }

                if (children.Count < maxChildrenPerRequest)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Enumerates the descendants of the node at the given path without blocking.
        /// </summary>
        /// <param name="ringMaster">Interface to ringmaster</param>
        /// <param name="path">Node path</param>
        /// <param name="maxChildrenPerRequest">Maximum number of children to retrieve with each GetChildren request</param>
        /// <param name="action">Action to execute for each descendant</param>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        public static async Task ForEachDescendant(this IRingMasterRequestHandler ringMaster, string path, int maxChildrenPerRequest, Action<string> action)
        {
            var queue = new Queue<string>();
            queue.Enqueue(path);

            while (queue.Count > 0)
            {
                string parentPath = queue.Dequeue();
                await ringMaster.ForEachChild(
                    parentPath, 
                    maxChildrenPerRequest,
                    child =>
                    {
                        string childPath = (parentPath == "/") ? ("/" + child) : (parentPath + "/" + child);
                        queue.Enqueue(childPath);
                    });

                action(parentPath);
            }
        }

        /// <summary>
        /// Registers to notifications for any change under the given path.
        /// </summary>
        /// <param name="ringMaster">The ringmaster handler to use.</param>
        /// <param name="timeout">The timeout for retries on setting the watcher.</param>
        /// <param name="pathToWatch">The path to watch.</param>
        /// <param name="oneuse">if set to <c>true</c> there will be just one notification triggered, and the watcher will be removed then.</param>
        /// <param name="sessionlocal">if set to <c>true</c> we will use a local session for this on the server.</param>
        /// <param name="onChange">The notification callback.</param>
        /// <returns>an async task indicating a boolean where true means the callback will be invoked</returns>
        public static async Task<bool> RegisterOnAnySubPathChange(
            this IRingMasterRequestHandler ringMaster, 
            int timeout, 
            string pathToWatch, 
            bool oneuse,
            bool sessionlocal, 
            RegisterOnAnySubPathChangeDelegate onChange)
        {
            if (ringMaster == null)
            {
                throw new ArgumentNullException("rm");
            }

            if (pathToWatch == null)
            {
                throw new ArgumentNullException("pathToWatch");
            }

            if (onChange == null)
            {
                throw new ArgumentNullException("onChange");
            }

            string path = GetBulkWatcherName(pathToWatch.Replace('/', '_')) + "_" + Guid.NewGuid().ToString();
            byte[] data = Encoding.UTF8.GetBytes("$startswith:" + pathToWatch + ",$sessionlocal:" + sessionlocal);

            DelegateWatcher watcher = new DelegateWatcher(
                ev => 
                {
                    // if the event was signaled because the bulkwatcher node was deleted, this means the watcher is removed as well.
                    if (ev.EventType == WatchedEvent.WatchedEventType.NodeDeleted && string.Equals(ev.Path, path))
                    {
                        return;
                    }

                    onChange(RingMasterException.Code.Ok, ev);

                    if (ev.EventType == WatchedEvent.WatchedEventType.WatcherRemoved && ev.KeeperState == WatchedEvent.WatchedEventKeeperState.SyncConnected)
                    {
                        ringMaster.Delete(path, -1, DeleteMode.None).Wait();
                    }
                }, 
                oneuse);

            DateTime maxTime = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeout);

            while (true)
            {
                try
                {
                    await ringMaster.Create(GetBulkWatcherName(null), null, null, CreateMode.Persistent);

                    break;
                }
                catch (RingMasterException ex)
                {
                    if (ex.ErrorCode == RingMasterException.Code.Connectionloss || ex.ErrorCode == RingMasterException.Code.Operationtimeout)
                    {
                        if (DateTime.UtcNow > maxTime)
                        {
                            return false;
                        }

                        continue;
                    }

                    if (ex.ErrorCode == RingMasterException.Code.Nodeexists)
                    {
                        break;
                    }

                    onChange(ex.ErrorCode, null);
                    return true;
                }
            }

            while (true)
            {
                try
                {
                    await ringMaster.Create(path, data, null, CreateMode.Ephemeral);

                    break;
                }
                catch (RingMasterException ex)
                {
                    if (ex.ErrorCode == RingMasterException.Code.Connectionloss || ex.ErrorCode == RingMasterException.Code.Operationtimeout)
                    {
                        if (DateTime.UtcNow > maxTime)
                        {
                            return false;
                        }

                        continue;
                    }

                    if (ex.ErrorCode == RingMasterException.Code.Nodeexists)
                    {
                        break;
                    }

                    onChange(ex.ErrorCode, null);
                    return true;
                }
            }

            while (true)
            {
                try
                {
                    await ringMaster.Exists(path, watcher, false);

                    break;
                }
                catch (RingMasterException ex)
                {
                    if (ex.ErrorCode == RingMasterException.Code.Connectionloss)
                    {
                        if (DateTime.UtcNow > maxTime)
                        {
                            return false;
                        }

                        continue;
                    }

                    onChange(ex.ErrorCode, null);
                    return true;
                }
            }

            return true;
        }

        /// <summary>
        /// Registers a bulk watcher for the given path prefix.
        /// </summary>
        /// <param name="ringMaster">Interface to ringmaster</param>
        /// <param name="pathPrefix">Path prefix to watch</param>
        /// <param name="watcher">The watcher that will be notified of changes that happen under the given path.</param>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        public static async Task RegisterBulkWatcher(this IRingMasterRequestHandler ringMaster, string pathPrefix, IWatcher watcher)
        {
            await ringMaster.Exists(string.Format("bulkwatcher:{0}", pathPrefix), watcher);
        }
        
        /// <summary>
        /// Gets the name of the bulk watcher.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>the path for a proper bulk watcher node</returns>
        private static string GetBulkWatcherName(string id)
        {
            if (id == null)
            {
                return "/$bulkwatcher";
            }

            return "/$bulkwatcher/" + id;
        }

        /// <summary>
        /// Examine the ResultCode in the given <paramref name="response"/> and if the ResultCode is not Ok, throw an exception that corresponds to the code.
        /// </summary>
        /// <param name="response">Response to check for error</param>
        private static void ThrowIfError(RequestResponse response)
        {
            if (response.ResultCode != (int)RingMasterException.Code.Ok)
            {
                Exception exception = RingMasterException.GetException(response);
                throw exception;
            }
        }
    }
}