// <copyright file="AbstractRingMaster.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Security.Authentication;
    using System.ServiceModel;
    using System.Text;
    using System.Threading;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.AsyncCallback;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using Code = Microsoft.Azure.Networking.Infrastructure.RingMaster.Data.RingMasterException.Code;
    using GetDataOptions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestGetData.GetDataOptions;
    using IGetDataOptionArgument = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestGetData.IGetDataOptionArgument;

    /// <summary>
    /// Class AbstractRingMaster.
    /// </summary>
    public abstract class AbstractRingMaster
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractRingMaster"/> class.
        /// To create a ZooKeeper client object, the application needs to pass a connection string containing a comma separated list of host:port pairs, each corresponding to a ZooKeeper server
        /// </summary>
        /// <param name="connectString">The connect string.</param>
        /// <param name="sessionTimeout">The session timeout.</param>
        /// <param name="watcher">The watcher.</param>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="sessionPasswd">The session passwd.</param>
        /// <param name="requestTimeout">The request timeout.</param>
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "This is desired behavior.")]
        protected AbstractRingMaster(string connectString, int sessionTimeout, IWatcher watcher, long sessionId = 0, byte[] sessionPasswd = null, int requestTimeout = 10000)
        {
            this.ConnectString = connectString;
            this.Watcher = watcher;
            this.AuthsByScheme = new Dictionary<string, byte[]>();

            this.SessionId = sessionId;
            this.SessionPasswd = sessionPasswd;

            // ReSharper disable once DoNotCallOverridableMethodsInConstructor
            this.Initialize(sessionTimeout, requestTimeout);
        }

        /// <summary>
        /// Gets or sets the connect string.
        /// </summary>
        /// <value>The connect string.</value>
        public string ConnectString { get; protected set; }

        /// <summary>
        /// Gets the session timeout for the RM connection
        /// </summary>
        /// <value>The session timeout.</value>
        public abstract int SessionTimeout { get; }

        /// <summary>
        /// Gets the timeout for RM requests
        /// </summary>
        public abstract int RequestTimeout { get; }

        /// <summary>
        /// Gets or sets the watcher object. TODO: pending proper implementation
        /// </summary>
        public IWatcher Watcher { get; protected set; }

        /// <summary>
        /// Gets or sets the session identifier. TODO: pending proper implementation
        /// </summary>
        public long SessionId
        {
            get
            {
                return long.Parse(Encoding.UTF8.GetString(this.AuthsByScheme["SessionId"]));
            }

            protected set
            {
                this.AuthsByScheme["SessionId"] = Encoding.UTF8.GetBytes(value.ToString());
            }
        }

        /// <summary>
        /// Gets or sets the session password. TODO: improve this implementation
        /// </summary>
        public byte[] SessionPasswd
        {
            get
            {
                return this.AuthsByScheme["SessionPwd"];
            }

            protected set
            {
                this.AuthsByScheme["SessionPwd"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the state. TODO: pending proper implementation
        /// </summary>
        public States State { get; protected set; }

        /// <summary>
        /// Gets or sets the auths by scheme
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope="Member", Justification = "This is desired behavior.")]
        protected Dictionary<string, byte[]> AuthsByScheme { get; set; }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <param name="sessionTimeout">The session timeout</param>
        /// <param name="requestTimeout">The request timeout</param>
        public abstract void Initialize(int sessionTimeout, int requestTimeout);

        /// <summary>
        /// Specify the default watcher for the connection (overrides the one specified during construction).
        /// </summary>
        /// <param name="watcher">The watcher.</param>
        public void Register(IWatcher watcher)
        {
            this.Watcher = watcher;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"RingMaster: {this.SessionId} {this.ConnectString} {this.State}";
        }

        /// <summary>
        /// Adds the authentication information.
        /// </summary>
        /// <param name="scheme">The scheme.</param>
        /// <param name="auth">The authentication.</param>
        public virtual void AddAuthInfo(string scheme, byte[] auth)
        {
            this.AuthsByScheme[scheme] = auth;
        }

        /// <summary>
        /// Adds the specified authentication information
        /// </summary>
        /// <param name="scheme">The scheme</param>
        /// <param name="auth">The authentication.</param>
        public void AddAuthInfo(string scheme, string auth)
        {
            this.AddAuthInfo(scheme, Encoding.UTF8.GetBytes(auth));
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        public abstract void Close();

        /// <summary>
        /// Executes an operation package
        /// </summary>
        /// <param name="ops">the operation package to run</param>
        /// <param name="completeSynchronously">if true the call completes when the operation is replicated locally</param>
        /// <param name="executionQueueId">the id of the execution queue, or null or Guid.Empty if none</param>
        /// <param name="executionQueueTimeoutMillis">the timeout for waiting on the execution queue</param>
        /// <returns>the list of operation results</returns>
        public IReadOnlyList<OpResult> Multi(IReadOnlyList<Op> ops, bool completeSynchronously = false, Guid? executionQueueId = null, int executionQueueTimeoutMillis = 0)
        {
            return this.Multi(ops, completeSynchronously, null, executionQueueId, executionQueueTimeoutMillis);
        }

        /// <summary>
        /// Executes an operation package
        /// </summary>
        /// <param name="ops">the operation package to run</param>
        /// <param name="completeSynchronously">if true the call completes when the operation is replicated locally</param>
        /// <param name="scheduledName">if not null this command will be inserted with the given name (must be unique) into the RingMaster backend scheduler command queue for later background execution.</param>
        /// <param name="executionQueueId">the id of the execution queue, or null or Guid.Empty if none</param>
        /// <param name="executionQueueTimeoutMillis">the timeout for waiting on the execution queue</param>
        /// <returns>the list of operation results</returns>
        public IReadOnlyList<OpResult> Multi(
            IReadOnlyList<Op> ops,
            bool completeSynchronously = false,
            string scheduledName = null,
            Guid? executionQueueId = null,
            int executionQueueTimeoutMillis = 0)
        {
            ManualResetEvent ev = ManualResetEventPool.InstancePool.GetOne();
            IReadOnlyList<OpResult> result = null;
            Exception e = null;
            this.Multi(
                ops,
                (rc, r, ctx) =>
                {
                    result = r;
                    e = KeeperException.ExceptionHelper.GetException(rc);
                    ManualResetEventPool.InstancePool.Set(ev);
                },
                null,
                completeSynchronously,
                scheduledName,
                executionQueueId,
                executionQueueTimeoutMillis);

            ManualResetEventPool.InstancePool.WaitOneAndReturn(ref ev);
            if (e != null)
            {
                throw e;
            }

            return result;
        }

        /// <summary>
        /// Executes an operation package
        /// </summary>
        /// <param name="ops">the operation package to run</param>
        /// <param name="completeSynchronously">if true the call completes when the operation is replicated locally</param>
        /// <param name="executionQueueId">the execution queue Id, or Guid.empty</param>
        /// <param name="executionQueueTimeout">the timeout for EQ</param>
        /// <returns>the list of operation results</returns>
        public IReadOnlyList<OpResult> Batch(IReadOnlyList<Op> ops, bool completeSynchronously, Guid? executionQueueId = null, int executionQueueTimeout = 0)
        {
            ManualResetEvent ev = ManualResetEventPool.InstancePool.GetOne();
            IReadOnlyList<OpResult> result = null;
            Exception e = null;
            this.Batch(
                ops,
                (rc, r, ctx) =>
                {
                    result = r;
                    e = KeeperException.ExceptionHelper.GetException(rc);
                    ManualResetEventPool.InstancePool.Set(ev);
                },
                null,
                completeSynchronously,
                executionQueueId,
                executionQueueTimeout);

            ManualResetEventPool.InstancePool.WaitOneAndReturn(ref ev);
            if (e != null)
            {
                throw e;
            }

            return result;
        }

        /// <summary>
        /// The Asynchronous version of multi.
        /// </summary>
        /// <param name="ops">The operations.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="completeSynchronously">if true the call completes when the operation is replicated locally</param>
        /// <param name="executionQueueId">the id of the execution queue, or null or Guid.Empty if none</param>
        /// <param name="executionQueueTimeoutMillis">the timeout for waiting on the execution queue</param>
        public void Multi(IReadOnlyList<Op> ops, IOpsResultCallback cb, object ctx, bool completeSynchronously = false, Guid? executionQueueId = null, int executionQueueTimeoutMillis = 0)
        {
            this.Multi(ops, cb == null ? (OpsResultCallbackDelegate)null : cb.ProcessResult, ctx, completeSynchronously, null, executionQueueId, executionQueueTimeoutMillis);
        }

        /// <summary>
        /// The Asynchronous version of batch.
        /// </summary>
        /// <param name="ops">The operations.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="completeSynchronously">if true the call completes when the operation is replicated locally</param>
        /// <param name="executionQueueId">optional, the id of the execution queue to use for this batch operation</param>
        /// <param name="executionQueueTimeout">optional, the timeout on wait queue for the execution queue</param>
        public void Batch(IReadOnlyList<Op> ops, IOpsResultCallback cb, object ctx, bool completeSynchronously = false, Guid? executionQueueId = null, int executionQueueTimeout = 0)
        {
            this.Batch(ops, cb == null ? (OpsResultCallbackDelegate)null : cb.ProcessResult, ctx, completeSynchronously, executionQueueId, executionQueueTimeout);
        }

        /// <summary>
        /// Create a node with the given path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="data">The data.</param>
        /// <param name="acl">The acl.</param>
        /// <param name="createMode">The create mode.</param>
        /// <returns>String.</returns>
        public string Create(string path, byte[] data, List<Acl> acl, CreateMode createMode)
        {
            ManualResetEvent ev = ManualResetEventPool.InstancePool.GetOne();
            string result = null;
            Exception e = null;
            this.Create(
                path,
                data,
                acl,
                createMode,
                (rc, p, ctx, name) =>
                {
                    result = name;
                    e = KeeperException.ExceptionHelper.GetException(rc);
                    ManualResetEventPool.InstancePool.Set(ev);
                },
                null);

            ManualResetEventPool.InstancePool.WaitOneAndReturn(ref ev);
            if (e != null)
            {
                throw e;
            }

            return result;
        }

        /// <summary>
        /// Create a node with the given path.
        /// </summary>
        /// <param name="nodePath">The path to the node to move.</param>
        /// <param name="version">the version of the node path</param>
        /// <param name="newParentPath">The path where the node needs to move into (i.e. the path that will become the parent path to this node).</param>
        /// <param name="moveMode">The move mode.</param>
        /// <returns>string with the new path of the node</returns>
        public string Move(string nodePath, int version, string newParentPath, MoveMode moveMode)
        {
            ManualResetEvent ev = ManualResetEventPool.InstancePool.GetOne();
            string result = null;
            Exception e = null;
            this.Move(
                nodePath,
                version,
                newParentPath,
                moveMode,
                (rc, p, ctx, path) =>
                {
                    result = path;
                    e = KeeperException.ExceptionHelper.GetException(rc);
                    ManualResetEventPool.InstancePool.Set(ev);
                },
                null);

            ManualResetEventPool.InstancePool.WaitOneAndReturn(ref ev);
            if (e != null)
            {
                throw e;
            }

            return result;
        }

        /// <summary>
        /// Schedule a delete operation for the given path.
        /// </summary>
        /// <param name="nodePath">Path of the node to be deleted</param>
        /// <param name="version">Version of the node</param>
        public void ScheduleDelete(string nodePath, int version)
        {
            string stagingLocation = $"{RingMasterExtensions.GetScheduledDeleteRoot()}/{Guid.NewGuid()}";
            this.Move(nodePath, version, stagingLocation, MoveMode.AllowPathCreationFlag);
        }

        /// <summary>
        /// The Asynchronous version of create.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="data">The data.</param>
        /// <param name="acl">The acl.</param>
        /// <param name="createMode">The create mode.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void Create(string path, byte[] data, List<Acl> acl, CreateMode createMode, IStringCallback cb, object ctx)
        {
            this.Create(path, data, acl, createMode, cb == null ? (StringCallbackDelegate)null : cb.ProcessResult, ctx);
        }

        /// <summary>
        /// The Asynchronous version of create.
        /// </summary>
        /// <param name="nodePath">The path to move.</param>
        /// <param name="version">The version of the original path.</param>
        /// <param name="destParentPath">The path that will become parent to the moved node.</param>
        /// <param name="moveMode">The move mode.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void Move(string nodePath, int version, string destParentPath, MoveMode moveMode, IStringCallback cb, object ctx)
        {
            this.Move(nodePath, version, destParentPath, moveMode, cb == null ? (StringCallbackDelegate)null : cb.ProcessResult, ctx);
        }

        /// <summary>
        /// Delete the node with the given path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="version">The version.</param>
        /// <param name="mode">the deletion mode</param>
        /// <returns>Whether deletion was successful</returns>
        public bool Delete(string path, int version, DeleteMode mode = DeleteMode.None)
        {
            ManualResetEvent ev = ManualResetEventPool.InstancePool.GetOne();
            Code code = Code.Unknown;
            this.Delete(
                path,
                version,
                (rc, p, ctx) =>
                {
                    code = KeeperException.ExceptionHelper.GetCode(rc);
                    ManualResetEventPool.InstancePool.Set(ev);
                },
                null,
                mode);

            ManualResetEventPool.InstancePool.WaitOneAndReturn(ref ev);
            if (code == Code.Ok)
            {
                return true;
            }

            if (code == Code.Nonode)
            {
                return false;
            }

            throw KeeperException.ExceptionHelper.GetException(code);
        }

        /// <summary>
        /// Deletes the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="version">The version.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="recursive">if true, the delte is recursive.</param>
        public void Delete(string path, int version, IVoidCallback cb, object ctx, bool recursive)
        {
            this.Delete(path, version, cb == null ? (VoidCallbackDelegate)null : cb.ProcessResult, ctx, recursive ? DeleteMode.CascadeDelete : DeleteMode.None);
        }

        /// <summary>
        /// Deletes the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="version">The version.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="mode">deletion mode</param>
        public void Delete(string path, int version, IVoidCallback cb, object ctx, DeleteMode mode = DeleteMode.None)
        {
            this.Delete(path, version, cb == null ? (VoidCallbackDelegate)null : cb.ProcessResult, ctx, mode);
        }

        /// <summary>
        /// Existses the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="watch">if set to <c>true</c> [watch].</param>
        /// <param name="throwIfNotFound">if the path doesn't exist, KeyNotFoundException will be thrown if set to true, if set to false, the function returns null.</param>
        /// <returns>Stat.</returns>
        public IStat Exists(string path, bool watch, bool throwIfNotFound = true)
        {
            ManualResetEvent ev = ManualResetEventPool.InstancePool.GetOne();
            IStat result = null;
            Exception e = null;
            this.Exists(
                path,
                watch,
                (rc, p, ctx, stat) =>
                {
                    result = stat;
                    e = KeeperException.ExceptionHelper.GetException(rc);
                    ManualResetEventPool.InstancePool.Set(ev);
                },
                null);

            ManualResetEventPool.InstancePool.WaitOneAndReturn(ref ev);
            if (e != null)
            {
                if (throwIfNotFound && e is KeyNotFoundException)
                {
                    throw e;
                }
            }

            return result;
        }

        /// <summary>
        /// Existses the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="watch">if set to <c>true</c> [watch].</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void Exists(string path, bool watch, IStatCallback cb, object ctx)
        {
            this.Exists(path, watch, cb == null ? (StatCallbackDelegate)null : cb.ProcessResult, ctx);
        }

        /// <summary>
        /// Existses the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="watcher">The watcher.</param>
        /// <param name="throwIfNotFound">if the path doesn't exist, KeyNotFoundException will be thrown if set to true, if set to false, the function returns null.</param>
        /// <returns>Stat.</returns>
        public IStat Exists(string path, IWatcher watcher, bool throwIfNotFound = true)
        {
            ManualResetEvent ev = ManualResetEventPool.InstancePool.GetOne();
            IStat result = null;
            Exception e = null;
            this.Exists(
                path,
                watcher,
                (rc, p, ctx, stat) =>
                {
                    result = stat;
                    e = KeeperException.ExceptionHelper.GetException(rc);
                    ManualResetEventPool.InstancePool.Set(ev);
                },
                null);

            ManualResetEventPool.InstancePool.WaitOneAndReturn(ref ev);
            if (e != null)
            {
                if (throwIfNotFound && e is KeyNotFoundException)
                {
                    throw e;
                }
            }

            return result;
        }

        /// <summary>
        /// Existses the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="watcher">The watcher.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void Exists(string path, IWatcher watcher, IStatCallback cb, object ctx)
        {
            this.Exists(path, watcher, cb == null ? (StatCallbackDelegate)null : cb.ProcessResult, ctx);
        }

        /// <summary>
        /// Gets the acl.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="stat">The stat.</param>
        /// <returns>List&lt;ACL&gt;.</returns>
        public IReadOnlyList<Acl> GetAcl(string path, IStat stat)
        {
            ManualResetEvent ev = ManualResetEventPool.InstancePool.GetOne();
            IReadOnlyList<Acl> result = null;
            Exception e = null;
            this.GetAcl(
                path,
                stat,
                (rc, p, ctx, acls, s) =>
                {
                    result = acls;
                    e = KeeperException.ExceptionHelper.GetException(rc);
                    ManualResetEventPool.InstancePool.Set(ev);
                },
                null);

            ManualResetEventPool.InstancePool.WaitOneAndReturn(ref ev);
            if (e != null)
            {
                throw e;
            }

            return result;
        }

        /// <summary>
        /// Gets the acl.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="stat">The stat.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void GetAcl(string path, IStat stat, IAclCallback cb, object ctx)
        {
            this.GetAcl(path, stat, cb == null ? (AclCallbackDelegate)null : cb.ProcessResult, ctx);
        }

        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="watch">if set to <c>true</c> [watch].</param>
        /// <param name="retrievalCondition">if not null, the retrieval contition in the form >:[top]:[childName].
        /// valid interval definitions:
        ///
        ///   ">:[Top]:[ChildName]"     ... returns the elements greater than the [ChildName] limited to Top count
        ///                                 so ">:1000:contoso" means give me first 1000 childrens greater than contoso
        ///                                 so ">:1000:"        means give me first 1000 elements
        /// </param>
        /// <returns>List&lt;String&gt;.</returns>
        public IReadOnlyList<string> GetChildren(string path, bool watch, string retrievalCondition)
        {
            ManualResetEvent ev = ManualResetEventPool.InstancePool.GetOne();
            IReadOnlyList<string> result = null;
            Exception e = null;
            this.GetChildren(
                path,
                watch,
                retrievalCondition,
                (int rc, string p, object ctx, IReadOnlyList<string> children, IStat stat) =>
                {
                    result = children;
                    e = KeeperException.ExceptionHelper.GetException(rc);
                    ManualResetEventPool.InstancePool.Set(ev);
                },
                null);

            ManualResetEventPool.InstancePool.WaitOneAndReturn(ref ev);
            if (e != null)
            {
                throw e;
            }

            return result;
        }

        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="watch">if set to <c>true</c> [watch].</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void GetChildren(string path, bool watch, IChildren2Callback cb, object ctx)
        {
            this.GetChildren(path, watch, cb == null ? (Children2CallbackDelegate)null : cb.ProcessResult, ctx);
        }

        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="watch">if set to <c>true</c> [watch].</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void GetChildren(string path, bool watch, IChildrenCallback cb, object ctx)
        {
            this.GetChildren(path, watch, cb == null ? (ChildrenCallbackDelegate)null : cb.ProcessResult, ctx);
        }

        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="watch">if set to <c>true</c> [watch].</param>
        /// <returns>List&lt;String&gt;.</returns>
        public IReadOnlyList<string> GetChildren(string path, bool watch)
        {
            ManualResetEvent ev = ManualResetEventPool.InstancePool.GetOne();
            IReadOnlyList<string> result = null;
            Exception e = null;
            this.GetChildren(
                path,
                watch,
                (int rc, string p, object ctx, IReadOnlyList<string> children, IStat s) =>
                {
                    result = children;
                    e = KeeperException.ExceptionHelper.GetException(rc);
                    ManualResetEventPool.InstancePool.Set(ev);
                },
                null);

            ManualResetEventPool.InstancePool.WaitOneAndReturn(ref ev);
            if (e != null)
            {
                throw e;
            }

            return result;
        }

        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="watcher">The watcher.</param>
        /// <returns>List&lt;String&gt;.</returns>
        public IReadOnlyList<string> GetChildren(string path, IWatcher watcher)
        {
            ManualResetEvent ev = ManualResetEventPool.InstancePool.GetOne();
            IReadOnlyList<string> result = null;
            Exception e = null;
            this.GetChildren(
                path,
                watcher,
                (int rc, string p, object ctx, IReadOnlyList<string> children) =>
                {
                    result = children;
                    e = KeeperException.ExceptionHelper.GetException(rc);
                    ManualResetEventPool.InstancePool.Set(ev);
                },
                null);

            ManualResetEventPool.InstancePool.WaitOneAndReturn(ref ev);
            if (e != null)
            {
                throw e;
            }

            return result;
        }

        /// <summary>
        /// The Asynchronous version of getChildren.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="watcher">The watcher.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void GetChildren(string path, IWatcher watcher, IChildren2Callback cb, object ctx)
        {
            this.GetChildren(path, watcher, cb == null ? (Children2CallbackDelegate)null : cb.ProcessResult, ctx);
        }

        /// <summary>
        /// The Asynchronous version of getChildren.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="watcher">The watcher.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void GetChildren(string path, IWatcher watcher, IChildrenCallback cb, object ctx)
        {
            this.GetChildren(path, watcher, cb == null ? (ChildrenCallbackDelegate)null : cb.ProcessResult, ctx);
        }

        /// <summary>
        /// The Asynchronous version of getData.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="watch">if set to <c>true</c> [watch].</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void GetData(string path, bool watch, IDataCallback cb, object ctx)
        {
            this.GetData(path, watch, cb == null ? (DataCallbackDelegate)null : cb.ProcessResult, ctx);
        }

        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="watch">if set to <c>true</c> [watch].</param>
        /// <returns>System.Byte[].</returns>
        public byte[] GetData(string path, bool watch)
        {
            ManualResetEvent ev = ManualResetEventPool.InstancePool.GetOne();
            byte[] result = null;
            Exception e = null;
            this.GetData(
                path,
                watch,
                (int rc, string p, object ctx, byte[] children, IStat s) =>
                {
                    result = children;
                    e = KeeperException.ExceptionHelper.GetException(rc);
                    ManualResetEventPool.InstancePool.Set(ev);
                },
                null);

            ManualResetEventPool.InstancePool.WaitOneAndReturn(ref ev);
            if (e != null)
            {
                throw e;
            }

            return result;
        }

        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="watcher">The watcher.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void GetData(string path, IWatcher watcher, IDataCallback cb, object ctx)
        {
            this.GetData(path, watcher, cb == null ? (DataCallbackDelegate)null : cb.ProcessResult, ctx);
        }

        /// <summary>
        /// Return the data and the stat of the node of the given path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="watcher">The watcher.</param>
        /// <returns>the byte[] data for the node</returns>
        public byte[] GetData(string path, IWatcher watcher)
        {
            ManualResetEvent ev = ManualResetEventPool.InstancePool.GetOne();
            byte[] result = null;
            Exception e = null;
            this.GetData(
                path,
                watcher,
                (int rc, string p, object ctx, byte[] children, IStat s) =>
                {
                    result = children;
                    e = KeeperException.ExceptionHelper.GetException(rc);
                    ManualResetEventPool.InstancePool.Set(ev);
                },
                null);

            ManualResetEventPool.InstancePool.WaitOneAndReturn(ref ev);
            if (e != null)
            {
                throw e;
            }

            return result;
        }

        /// <summary>
        /// Set the data for the node of the given path if such a node exists and the given version
        /// matches the version of the node
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="data">The data.</param>
        /// <param name="version">The version.</param>
        /// <returns>Stat.</returns>
        public IStat SetData(string path, ISetDataOperation data, int version)
        {
            ManualResetEvent ev = ManualResetEventPool.InstancePool.GetOne();
            IStat result = null;
            Exception e = null;

            this.SetData(
                path,
                data,
                version,
                (int rc, string p, object ctx, IStat stat) =>
                {
                    result = stat;
                    e = KeeperException.ExceptionHelper.GetException(rc);
                    ManualResetEventPool.InstancePool.Set(ev);
                },
                null);

            ManualResetEventPool.InstancePool.WaitOneAndReturn(ref ev);
            if (e != null)
            {
                throw e;
            }

            return result;
        }

        /// <summary>
        /// Set the data for the node of the given path if such a node exists and the given version
        /// matches the version of the node
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="data">The data.</param>
        /// <param name="version">The version.</param>
        /// <returns>Stat.</returns>
        public IStat SetData(string path, byte[] data, int version)
        {
            ManualResetEvent ev = ManualResetEventPool.InstancePool.GetOne();
            IStat result = null;
            Exception e = null;

            this.SetData(
                path,
                data,
                version,
                (int rc, string p, object ctx, IStat stat) =>
                {
                    result = stat;
                    e = KeeperException.ExceptionHelper.GetException(rc);
                    ManualResetEventPool.InstancePool.Set(ev);
                },
                null);

            ManualResetEventPool.InstancePool.WaitOneAndReturn(ref ev);
            if (e != null)
            {
                throw e;
            }

            return result;
        }

        /// <summary>
        /// Set the ACL for the node of the given path if such a node exists and the given version
        /// matches the version of the node.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="acl">The acl.</param>
        /// <param name="version">The version.</param>
        /// <returns>Stat.</returns>
        public IStat SetAcl(string path, List<Acl> acl, int version)
        {
            ManualResetEvent ev = ManualResetEventPool.InstancePool.GetOne();
            IStat result = null;
            Exception e = null;
            this.SetAcl(
                path,
                acl,
                version,
                (int rc, string p, object ctx, IStat stat) =>
                {
                    result = stat;
                    e = KeeperException.ExceptionHelper.GetException(rc);
                    ManualResetEventPool.InstancePool.Set(ev);
                },
                null);

            ManualResetEventPool.InstancePool.WaitOneAndReturn(ref ev);
            if (e != null)
            {
                throw e;
            }

            return result;
        }

        /// <summary>
        /// Synchronizes the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        public void Sync(string path)
        {
            ManualResetEvent ev = ManualResetEventPool.InstancePool.GetOne();
            Exception e = null;
            this.Sync(
                path,
                (int rc, string p, object ctx) =>
                {
                    e = KeeperException.ExceptionHelper.GetException(rc);
                    ManualResetEventPool.InstancePool.Set(ev);
                },
                null);

            ManualResetEventPool.InstancePool.WaitOneAndReturn(ref ev);
            if (e != null)
            {
                throw e;
            }
        }

        /// <summary>
        /// Sets the acl, asynchronously.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="acl">The acl.</param>
        /// <param name="version">The version.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void SetAcl(string path, List<Acl> acl, int version, IStatCallback cb, object ctx)
        {
            this.SetAcl(path, acl, version, cb == null ? (StatCallbackDelegate)null : cb.ProcessResult, ctx);
        }

        /// <summary>
        /// Sets the data.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="data">The data.</param>
        /// <param name="version">The version.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void SetData(string path, ISetDataOperation data, int version, IStatCallback cb, object ctx)
        {
            this.SetData(path, data, version, cb == null ? (StatCallbackDelegate)null : cb.ProcessResult, ctx);
        }

        /// <summary>
        /// Sets the data.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="data">The data.</param>
        /// <param name="version">The version.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void SetData(string path, byte[] data, int version, IStatCallback cb, object ctx)
        {
            this.SetData(path, data, version, cb == null ? (StatCallbackDelegate)null : cb.ProcessResult, ctx);
        }

        /// <summary>
        /// Synchronizes the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void Sync(string path, IVoidCallback cb, object ctx)
        {
            this.Sync(path, cb == null ? (VoidCallbackDelegate)null : cb.ProcessResult, ctx);
        }

        /// <summary>
        /// Executes an operation package.
        /// </summary>
        /// <param name="ops">The operations.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="completeSynchronously">if true the operation calls cb once the replication happens locally</param>
        /// <param name="scheduledName">if not null this command will be inserted with the given name (must be unique) into the RingMaster backend scheduler command queue for later background execution.</param>
        /// <param name="executionQueueId">the id of the execution queue, or null or Guid.Empty if none</param>
        /// <param name="executionQueueTimeoutMillis">the timeout for waiting on the execution queue</param>
        public void Multi(IReadOnlyList<Op> ops, OpsResultCallbackDelegate cb, object ctx, bool completeSynchronously = false, string scheduledName = null, Guid? executionQueueId = null, int executionQueueTimeoutMillis = 0)
        {
            RequestMulti request = new RequestMulti(ops, ctx, cb, completeSynchronously, scheduledName);
            if (executionQueueId.HasValue)
            {
                request.ExecutionQueueId = executionQueueId.Value;
            }

            request.ExecutionQueueTimeoutMillis = executionQueueTimeoutMillis;
            this.Send(request);
        }

        /// <summary>
        /// Executes an operation package.
        /// </summary>
        /// <param name="ops">The operations.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="completeSynchronously">if true the operation calls cb once the replication happens locally</param>
        /// <param name="scheduledName">if not null this command will be inserted with the given name (must be unique) into the RingMaster backend scheduler command queue for later background execution.</param>
        /// <param name="executionQueueId">the id of the execution queue, or null or Guid.Empty if none</param>
        /// <param name="executionQueueTimeoutMillis">the timeout for waiting on the execution queue</param>
        public void Multi(IRingMasterBackendRequest[] ops, OpsResultCallbackDelegate cb, object ctx, bool completeSynchronously = false, string scheduledName = null, Guid? executionQueueId = null, int executionQueueTimeoutMillis = 0)
        {
            RequestMulti request = new RequestMulti(ops, ctx, cb, completeSynchronously, scheduledName);
            if (executionQueueId.HasValue)
            {
                request.ExecutionQueueId = executionQueueId.Value;
            }

            request.ExecutionQueueTimeoutMillis = executionQueueTimeoutMillis;
            this.Send(request);
        }

        /// <summary>
        /// Executes an operation package.
        /// </summary>
        /// <param name="ops">The operations.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="completeSynchronously">if true the operation calls cb once the replication happens locally</param>
        /// <param name="executionQueueId">execution queue id or null if none</param>
        /// <param name="executionQueueTimeout">timeout waiting in the execution queue</param>
        public void Batch(IReadOnlyList<Op> ops, OpsResultCallbackDelegate cb, object ctx, bool completeSynchronously = false, Guid? executionQueueId = null, int executionQueueTimeout = 0)
        {
            RequestBatch request = new RequestBatch(ops, ctx, cb, completeSynchronously);
            if (executionQueueId.HasValue)
            {
                request.ExecutionQueueId = executionQueueId.Value;
            }

            request.ExecutionQueueTimeoutMillis = executionQueueTimeout;
            this.Send(request);
        }

        /// <summary>
        /// Creates the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="data">The data.</param>
        /// <param name="acl">The acl.</param>
        /// <param name="createMode">The create mode.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void Create(string path, byte[] data, List<Acl> acl, CreateMode createMode, StringCallbackDelegate cb, object ctx)
        {
            this.Send(new RequestCreate(path, ctx, data, acl, createMode, cb));
        }

        /// <summary>
        /// Moves a node so that its new parent is the given path.
        /// </summary>
        /// <param name="nodePath">The path to the node to move.</param>
        /// <param name="version">The version of the old node path</param>
        /// <param name="newParentPath">The path where the node needs to move into (i.e. the path that will become the parent path to this node).</param>
        /// <param name="moveMode">The move mode.</param>
        /// <param name="cb">callback on completion</param>
        /// <param name="ctx">the context</param>
        public void Move(string nodePath, int version, string newParentPath, MoveMode moveMode, StringCallbackDelegate cb, object ctx)
        {
            this.Send(new RequestMove(nodePath, ctx, version, newParentPath, cb, moveMode));
        }

        /// <summary>
        /// Deletes the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="version">The version.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="recursive">if true, the delte is recursive. </param>
        public void Delete(string path, int version, VoidCallbackDelegate cb, object ctx, bool recursive)
        {
            this.Send(new RequestDelete(path, ctx, version, cb, recursive ? DeleteMode.CascadeDelete : DeleteMode.None));
        }

        /// <summary>
        /// Deletes the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="version">The version.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="deletemode">the delete mode of the operation.</param>
        public void Delete(string path, int version, VoidCallbackDelegate cb, object ctx, DeleteMode deletemode = DeleteMode.None)
        {
            this.Send(new RequestDelete(path, ctx, version, cb, deletemode));
        }

        /// <summary>
        /// Moves the specified path to a destination location.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="version">The version.</param>
        /// <param name="pathDst">destination path</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="mode">the mode of the move operation</param>
        public void Move(string path, int version, string pathDst, StringCallbackDelegate cb, object ctx, MoveMode mode = MoveMode.None)
        {
            this.Send(new RequestMove(path, ctx, version, pathDst, cb, mode));
        }

        /// <summary>
        /// Existses the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="watch">if set to <c>true</c> [watch].</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void Exists(string path, bool watch, StatCallbackDelegate cb, object ctx)
        {
            this.Send(new RequestExists(path, ctx, watch ? this.Watcher : null, cb));
        }

        /// <summary>
        /// Existses the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="watcher">The watcher.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void Exists(string path, IWatcher watcher, StatCallbackDelegate cb, object ctx)
        {
            this.Send(new RequestExists(path, ctx, watcher, cb));
        }

        /// <summary>
        /// Gets the acl.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="stat">The stat.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void GetAcl(string path, IStat stat, AclCallbackDelegate cb, object ctx)
        {
            this.Send(new RequestGetAcl(path, ctx, stat, cb));
        }

        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="watch">if set to <c>true</c> [watch].</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void GetChildren(string path, bool watch, Children2CallbackDelegate cb, object ctx)
        {
            this.Send(new RequestGetChildren(path, ctx, watch ? this.Watcher : null, cb));
        }

        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="watch">if set to <c>true</c> [watch].</param>
        /// <param name="retrievalCondition">if not null, the retrieval contition in the form >:[top]:[ChildName].
        /// valid interval definitions:
        ///
        ///   ">:[Top]:[ChildName]"     ... returns the elements greater than the [ChildName] limited to Top count
        ///                                 so ">:1000:contoso" means give me first 1000 childrens greater than contoso
        ///                                 so ">:1000:"        means give me first 1000 elements
        /// </param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void GetChildren(string path, bool watch, string retrievalCondition, Children2CallbackDelegate cb, object ctx)
        {
            this.Send(new RequestGetChildren(path, ctx, watch ? this.Watcher : null, cb, retrievalCondition));
        }

        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="watch">if set to <c>true</c> [watch].</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void GetChildren(string path, bool watch, ChildrenCallbackDelegate cb, object ctx)
        {
            this.Send(new RequestGetChildren(path, ctx, watch ? this.Watcher : null, cb));
        }

        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="watch">if set to <c>true</c> [watch].</param>
        /// <param name="retrievalCondition">if not null, the retrieval contition in the form >:[top]:[ChildName].
        /// valid interval definitions:
        ///
        ///   ">:[Top]:[ChildName]"     ... returns the elements greater than the [ChildName] limited to Top count
        ///                                 so ">:1000:contoso" means give me first 1000 childrens greater than contoso
        ///                                 so ">:1000:"        means give me first 1000 elements
        /// </param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void GetChildren(string path, bool watch, string retrievalCondition, ChildrenCallbackDelegate cb, object ctx)
        {
            this.Send(new RequestGetChildren(path, ctx, watch ? this.Watcher : null, cb, retrievalCondition));
        }

        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="watcher">The watcher.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void GetChildren(string path, IWatcher watcher, Children2CallbackDelegate cb, object ctx)
        {
            this.Send(new RequestGetChildren(path, ctx, watcher, cb));
        }

        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="watcher">The watcher.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void GetChildren(string path, IWatcher watcher, ChildrenCallbackDelegate cb, object ctx)
        {
            this.Send(new RequestGetChildren(path, ctx, watcher, cb));
        }

        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="watch">if set to <c>true</c> [watch].</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void GetData(string path, bool watch, DataCallbackDelegate cb, object ctx)
        {
            this.Send(new RequestGetData(path, ctx, watch ? this.Watcher : null, cb));
        }

        /// <summary>
        /// Gets the data from the node, or from the first parent with data.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="watch">if set to <c>true</c> [watch].</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void GetDataFromNodeOrParent(string path, bool watch, DataCallbackDelegate cb, object ctx)
        {
            this.Send(new RequestGetData(path, true, ctx, watch ? this.Watcher : null, cb));
        }

        /// <summary>
        /// Gets the data with options for the getdata operation.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="options">The options for the operation.</param>
        /// <param name="watch">if set to <c>true</c> [watch].</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void GetDataWithOptions(string path, GetDataOptions options, bool watch, DataCallbackDelegate cb, object ctx)
        {
            this.Send(new RequestGetData(path, options, ctx, watch ? this.Watcher : null, cb));
        }

        /// <summary>
        /// Gets the data with options for the getdata operation.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="options">The options for the operation.</param>
        /// <param name="arg">argument for the option (optional)</param>
        /// <param name="watch">if set to <c>true</c> [watch].</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void GetDataWithOptions(string path, GetDataOptions options, IGetDataOptionArgument arg, bool watch, DataCallbackDelegate cb, object ctx)
        {
            RequestGetData req = new RequestGetData(path, options, arg, ctx, watch ? this.Watcher : null, cb);
            this.Send(req);
        }

        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="watcher">The watcher.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void GetData(string path, IWatcher watcher, DataCallbackDelegate cb, object ctx)
        {
            this.Send(new RequestGetData(path, ctx, watcher, cb));
        }

        /// <summary>
        /// Gets the data from the node, or from the first parent with data.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="watcher">The watcher.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void GetDataFromNodeOrParent(string path, IWatcher watcher, DataCallbackDelegate cb, object ctx)
        {
            this.Send(new RequestGetData(path, true, ctx, watcher, cb));
        }

        /// <summary>
        /// Gets the data specifying options for the operation.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="options">The options for the operation.</param>
        /// <param name="watcher">The watcher.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void GetDataWithOptions(string path, GetDataOptions options, IWatcher watcher, DataCallbackDelegate cb, object ctx)
        {
            this.Send(new RequestGetData(path, options, ctx, watcher, cb));
        }

        /// <summary>
        /// Sets the data.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="data">The data.</param>
        /// <param name="version">The version.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void SetData(string path, byte[] data, int version, StatCallbackDelegate cb, object ctx)
        {
            this.Send(new RequestSetData(path, ctx, data, version, cb));
        }

        /// <summary>
        /// Sets the data.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="data">The data.</param>
        /// <param name="version">The version.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void SetData(string path, ISetDataOperation data, int version, StatCallbackDelegate cb, object ctx)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            this.Send(new RequestSetData(path, ctx, data.RawData, version, cb, true));
        }

        /// <summary>
        /// Sets the acl.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="acl">The acl.</param>
        /// <param name="version">The version.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void SetAcl(string path, List<Acl> acl, int version, StatCallbackDelegate cb, object ctx)
        {
            this.Send(new RequestSetAcl(path, ctx, acl, version, cb));
        }

        /// <summary>
        /// Synchronizes the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="cb">The cb.</param>
        /// <param name="ctx">The CTX.</param>
        public void Sync(string path, VoidCallbackDelegate cb, object ctx)
        {
            this.Send(new RequestSync(path, ctx, cb));
        }

        /// <summary>
        /// Enqueues the specified req.
        /// </summary>
        /// <param name="req">The req.</param>
        public abstract void Send(IRingMasterBackendRequest req);

        /// <summary>
        /// retrieves a setdataoperation helper object, useful to compose SetData operations.
        /// </summary>
        /// <returns>the helper to use</returns>
        public abstract ISetDataOperationHelper GetSetDataOperationHelper();

        /// <summary>
        /// Called when [complete].
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="resultcode">The resultcode.</param>
        /// <param name="timeInMillis">The time in millis.</param>
        protected abstract void OnComplete(IRingMasterBackendRequest req, int resultcode, double timeInMillis);
    }
}
