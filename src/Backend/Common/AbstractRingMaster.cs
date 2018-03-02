// <copyright file="AbstractRingMaster.cs" company="Microsoft">
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
        /// TODO: pending proper implementation
        /// </summary>
        /// <value>The watcher.</value>
        public IWatcher Watcher { get; protected set; }

        /// <summary>
        /// TODO: pending proper implementation
        /// </summary>
        /// <value>The session identifier.</value>
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
        /// TODO: improve this implementation
        /// </summary>
        /// <value>The session passwd.</value>
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
        /// TODO: pending proper implementation
        /// </summary>
        /// <value>The state.</value>
        public States State { get; protected set; }

        /// <summary>
        /// The auths by scheme
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
            this.Multi(ops, (rc, r, ctx) =>
            {
                result = r;
                e = KeeperException.ExceptionHelper.GetException(rc);
                ManualResetEventPool.InstancePool.Set(ev);
            }, null, completeSynchronously, scheduledName, executionQueueId, executionQueueTimeoutMillis);

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
            this.Batch(ops, (rc, r, ctx) =>
            {
                result = r;
                e = KeeperException.ExceptionHelper.GetException(rc);
                ManualResetEventPool.InstancePool.Set(ev);
            }, null, completeSynchronously, executionQueueId, executionQueueTimeout);

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
            this.Create(path, data, acl, createMode, (rc, p, ctx, name) =>
            {
                result = name;
                e = KeeperException.ExceptionHelper.GetException(rc);
                ManualResetEventPool.InstancePool.Set(ev);
            }, null);

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
            this.Move(nodePath, version, newParentPath, moveMode, (rc, p, ctx, path) =>
            {
                result = path;
                e = KeeperException.ExceptionHelper.GetException(rc);
                ManualResetEventPool.InstancePool.Set(ev);
            }, null);

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
        /// <param name="path">Path of the node to be deleted</param>
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
            this.Delete(path, version, (rc, p, ctx) =>
            {
                code = KeeperException.ExceptionHelper.GetCode(rc);
                ManualResetEventPool.InstancePool.Set(ev);
            }, null, mode);

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
            this.Exists(path, watch, (rc, p, ctx, stat) =>
            {
                result = stat;
                e = KeeperException.ExceptionHelper.GetException(rc);
                ManualResetEventPool.InstancePool.Set(ev);
            }, null);

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
            this.Exists(path, watcher, (rc, p, ctx, stat) =>
            {
                result = stat;
                e = KeeperException.ExceptionHelper.GetException(rc);
                ManualResetEventPool.InstancePool.Set(ev);
            }, null);

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
            this.GetAcl(path, stat, (rc, p, ctx, acls, s) =>
            {
                result = acls;
                e = KeeperException.ExceptionHelper.GetException(rc);
                ManualResetEventPool.InstancePool.Set(ev);
            }, null);

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
            this.GetChildren(path, watch, retrievalCondition, (int rc, string p, object ctx, IReadOnlyList<string> children, IStat stat) =>
            {
                result = children;
                e = KeeperException.ExceptionHelper.GetException(rc);
                ManualResetEventPool.InstancePool.Set(ev);
            }, null);

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
            this.GetChildren(path, watch, (int rc, string p, object ctx, IReadOnlyList<string> children, IStat s) =>
            {
                result = children;
                e = KeeperException.ExceptionHelper.GetException(rc);
                ManualResetEventPool.InstancePool.Set(ev);
            }, null);

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
            this.GetChildren(path, watcher, (int rc, string p, object ctx, IReadOnlyList<string> children) =>
            {
                result = children;
                e = KeeperException.ExceptionHelper.GetException(rc);
                ManualResetEventPool.InstancePool.Set(ev);
            }, null);

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
            this.GetData(path, watch, (int rc, string p, object ctx, byte[] children, IStat s) =>
            {
                result = children;
                e = KeeperException.ExceptionHelper.GetException(rc);
                ManualResetEventPool.InstancePool.Set(ev);
            }, null);

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
            this.GetData(path, watcher, (int rc, string p, object ctx, byte[] children, IStat s) =>
            {
                result = children;
                e = KeeperException.ExceptionHelper.GetException(rc);
                ManualResetEventPool.InstancePool.Set(ev);
            }, null);

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

            this.SetData(path, data, version, (int rc, string p, object ctx, IStat stat) =>
            {
                result = stat;
                e = KeeperException.ExceptionHelper.GetException(rc);
                ManualResetEventPool.InstancePool.Set(ev);
            }, null);

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

            this.SetData(path, data, version, (int rc, string p, object ctx, IStat stat) =>
            {
                result = stat;
                e = KeeperException.ExceptionHelper.GetException(rc);
                ManualResetEventPool.InstancePool.Set(ev);
            }, null);

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
            this.SetAcl(path, acl, version, (int rc, string p, object ctx, IStat stat) =>
            {
                result = stat;
                e = KeeperException.ExceptionHelper.GetException(rc);
                ManualResetEventPool.InstancePool.Set(ev);
            }, null);

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
            this.Sync(path, (int rc, string p, object ctx) =>
            {
                e = KeeperException.ExceptionHelper.GetException(rc);
                ManualResetEventPool.InstancePool.Set(ev);
            }, null);

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
        /// Called when [complete].
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="resultcode">The resultcode.</param>
        /// <param name="timeInMillis">The time in millis.</param>
        protected abstract void OnComplete(IRingMasterBackendRequest req, int resultcode, double timeInMillis);

        /// <summary>
        /// retrieves a setdataoperation helper object, useful to compose SetData operations.
        /// </summary>
        /// <returns>the helper to use</returns>
        public abstract ISetDataOperationHelper GetSetDataOperationHelper();
    }

    /// <summary>
    /// Origin operations.
    /// </summary>
    public enum RepositoryOperation
    {
        /// <summary>
        /// Query name operation.
        /// </summary>
        QueryName,

        /// <summary>
        /// Delegation lookup operation.
        /// </summary>
        Delegation,

        /// <summary>
        /// RingMaster get call.
        /// </summary>
        RingMasterGet,

        /// <summary>
        /// RingMaster put call.
        /// </summary>
        RingMasterWrite,

        /// <summary>
        /// RingMaster delete call.
        /// </summary>
        RingMasterDelete,

        /// <summary>
        /// RingMaster sync call.
        /// </summary>
        RingMasterSync,

        /// <summary>
        /// RingMaster multi call.
        /// </summary>
        RingMasterMulti,

        /// <summary>
        /// Resource provider read operation.
        /// </summary>
        ProviderReadOperation,

        /// <summary>
        /// Resource provider update or insert operation.
        /// </summary>
        ProviderUpsertOperation,

        /// <summary>
        /// Resource provider delete operation.
        /// </summary>
        ProviderDeleteOperation,

        /// <summary>
        /// Resource provider list operation.
        /// </summary>
        ProviderListOperation,

        /// <summary>
        /// Resource provider move operation.
        /// </summary>
        ProviderMoveOperation,

        /// <summary>
        /// ResourceProviderStatus operation.
        /// </summary>
        ProviderStatusOperation
    }

    /// <summary>
    /// This interface abstracts a client connection instrumentation.
    /// </summary>
    public interface IClientConnectionInstrumentation
    {
        /// <summary>
        /// Start operation trace.
        /// </summary>
        /// <param name="operation">Operation type</param>
        /// <param name="operationId">Operation Id</param>
        void StartOperation(RepositoryOperation operation, Guid operationId);

        /// <summary>
        /// End operation trace.
        /// </summary>
        /// <param name="operation">Operation type</param>
        /// <param name="operationId">Operation Id</param>
        /// <param name="ringMasterCode">Ring master return code</param>
        /// <param name="elapsedMilliseconds">Elapsed milliseconds</param>
        void EndOperation(RepositoryOperation operation, Guid operationId, int ringMasterCode, long elapsedMilliseconds);

        /// <summary>
        /// Fires when request is sent for processing.
        /// Reports request queue length waiting for response from server.
        /// </summary>
        /// <param name="queueLength">Length of a queue of requests awaiting for response</param>
        void ReportAwaitingRequestQueue(int queueLength);

        /// <summary>
        /// Fires when request is sent for processing.
        /// Reports request queue length waiting to be sent to the server.
        /// </summary>
        /// <param name="queueLength">Length of a queue of requests awaiting to be sent from client</param>
        void ReportOutgoingRequestQueue(int queueLength);

        /// <summary>
        /// Fires when request is dropped before sending to the queue.
        /// </summary>
        /// <param name="request">Request being dropped</param>
        /// <param name="error">Drop reason</param>
        void ReportDroppedRequest(IRingMasterBackendRequest request, Code error);
    }

    public enum BehaviorAction
    {
        AllowRequest = 1,
        FailRequest,
    }

    /// <summary>
    /// this interface abstracts a client connection load limiter
    /// </summary>
    public interface ILoadBehavior
    {
        /// <summary>
        /// called when a new request arrives
        /// </summary>
        /// <returns>allow request or fail request, depending on the limiter policies</returns>
        BehaviorAction OnRequestArrived();

        /// <summary>
        /// called when a request is completed, indicating the completion time
        /// </summary>
        /// <param name="timeInTicks">if bigger than zero, the time in ticks ellapsed</param>
        void OnRequestCompleted(long timeInTicks);

        /// <summary>
        /// called when we need to reset the limiter
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// Enum States
    /// </summary>
    public enum States
    {
        /// <summary>
        /// The associating
        /// </summary>
        Associating,

        /// <summary>
        /// The aut h_ failed
        /// </summary>
        AuthFailed,

        /// <summary>
        /// The closed
        /// </summary>
        Closed,

        /// <summary>
        /// The connected
        /// </summary>
        Connected,

        /// <summary>
        /// The connecting
        /// </summary>
        Connecting,
    }

    [Serializable]
    public class AuthNotSetException : Exception
    {
        public AuthNotSetException(string msg) : base(msg)
        {
        }
    }

    [Serializable]
    public class InvalidAclException : Exception
    {
        public string SessionData { get; }

        public InvalidAclException(string msg, string sessionData)
            : base($"Session:{sessionData}, {msg}")
        {
            this.SessionData = sessionData;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue("SessionData", this.SessionData);
        }
    }

    namespace Data
    {
        public static class Scheme
        {
            public const string World = "world";
            public const string Authenticated = "auth";
            public const string Digest = "digest";
            public const string Host = "host";
            public const string Ip = "ip";
        }

        /// <summary>
        /// Interface IStat
        /// </summary>
        public interface IMutableStat : IStat
        {
            /// <summary>
            /// Gets or sets the czxid - Creation Transaction Id.
            /// </summary>
            /// <value>The czxid.</value>
            new long Czxid { get; set; }

            /// <summary>
            /// Gets or sets the mzxid - Modification Transaction Id.
            /// </summary>
            /// <value>The mzxid.</value>
            new long Mzxid { get; set; }

            /// <summary>
            /// Gets or sets the pzxid - Children Changed Transaction Id.
            /// </summary>
            /// <value>The pzxid.</value>
            new long Pzxid { get; set; }

            /// <summary>
            /// Gets or sets the ctime - Create time.
            /// </summary>
            /// <value>The ctime.</value>
            new long Ctime { get; set; }

            /// <summary>
            /// Gets or sets the mtime - Modification time.
            /// </summary>
            /// <value>The mtime.</value>
            new long Mtime { get; set; }

            /// <summary>
            /// Gets or sets the version - Data Version.
            /// </summary>
            /// <value>The version.</value>
            new int Version { get; set; }

            /// <summary>
            /// Gets or sets the cversion - Children list version.
            /// </summary>
            /// <value>The cversion.</value>
            new int Cversion { get; set; }

            /// <summary>
            /// Gets or sets the aversion - Acl version.
            /// </summary>
            /// <value>The aversion.</value>
            new int Aversion { get; set; }

            /// <summary>
            /// Gets or sets the length of the data.
            /// </summary>
            /// <value>The length of the data.</value>
            new int DataLength { get; set; }

            /// <summary>
            /// Gets or sets the number children.
            /// </summary>
            /// <value>The number children.</value>
            new int NumChildren { get; set; }
        }

        /// <summary>
        /// Class FirstStat.
        /// </summary>
        [Serializable]
        public sealed class FirstStat : IMutableStat
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="FirstStat"/> class.
            /// </summary>
            /// <param name="other">The other.</param>
            public FirstStat(IStat other)
            {
                if (other == null)
                {
                    throw new ArgumentNullException(nameof(other));
                }

                this.Czxid = other.Czxid;
                this.Ctime = other.Ctime;
                this.DataLength = other.DataLength;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="FirstStat"/> class.
            /// </summary>
            /// <param name="zxid">The zxid.</param>
            /// <param name="time">The time.</param>
            /// <param name="dataLength">Length of the data.</param>
            public FirstStat(long zxid, long time, int dataLength)
            {
                this.Czxid = zxid;
                this.Ctime = time;
                this.DataLength = dataLength;
            }

            /// <summary>
            /// Gets or sets the czxid.
            /// </summary>
            /// <value>The czxid.</value>
            public long Czxid { get; set; }

            /// <summary>
            /// Gets or sets the mzxid.
            /// </summary>
            /// <value>The mzxid.</value>
            /// <exception cref="System.InvalidOperationException"></exception>
            public long Mzxid
            {
                get { return this.Czxid; } set { throw new InvalidOperationException(); }
            }

            /// <summary>
            /// Gets or sets the pzxid.
            /// </summary>
            /// <value>The pzxid.</value>
            /// <exception cref="System.InvalidOperationException"></exception>
            public long Pzxid
            {
                get { return this.Czxid; } set { throw new InvalidOperationException(); }
            }

            /// <summary>
            /// Gets or sets the ctime.
            /// </summary>
            /// <value>The ctime.</value>
            public long Ctime { get; set; }

            /// <summary>
            /// Gets or sets the mtime.
            /// </summary>
            /// <value>The mtime.</value>
            /// <exception cref="System.InvalidOperationException"></exception>
            public long Mtime
            {
                get { return this.Ctime; } set { throw new InvalidOperationException(); }
            }

            /// <summary>
            /// Gets or sets the version.
            /// </summary>
            /// <value>The version.</value>
            /// <exception cref="System.InvalidOperationException"></exception>
            public int Version
            {
                get { return 1; } set { throw new InvalidOperationException(); }
            }

            /// <summary>
            /// Gets or sets the cversion.
            /// </summary>
            /// <value>The cversion.</value>
            /// <exception cref="System.InvalidOperationException"></exception>
            public int Cversion
            {
                get { return this.Version; } set { throw new InvalidOperationException(); }
            }

            /// <summary>
            /// Gets or sets the aversion.
            /// </summary>
            /// <value>The aversion.</value>
            /// <exception cref="System.InvalidOperationException"></exception>
            public int Aversion
            {
                get { return this.Version; } set { throw new InvalidOperationException(); }
            }

            /// <summary>
            /// Gets or sets the length of the data.
            /// </summary>
            /// <value>The length of the data.</value>
            public int DataLength { get; set; }

            /// <summary>
            /// Gets or sets the number children.
            /// </summary>
            /// <value>The number children.</value>
            public int NumChildren { get; set; }

            /// <summary>
            /// Gets the unique incarnation id for this object
            /// </summary>
            public Guid UniqueIncarnationId => Stat.GetUniqueIncarnationId(this, false);

            /// <summary>
            /// Gets the unique incarnation id for this object, also considering changes on its children.
            /// </summary>
            public Guid UniqueExtendedIncarnationId => Stat.GetUniqueIncarnationId(this, true);

            /// <summary>
            /// Determines whether the specified <see cref="object"/> is equal to this instance.
            /// </summary>
            /// <param name="obj">The object to compare with the current object.</param>
            /// <returns><c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
            public override bool Equals(object obj)
            {
                IMutableStat other = obj as IMutableStat;
                if (other == null)
                {
                    return false;
                }

                return this.Czxid == other.Czxid &&
                    this.Mzxid == other.Mzxid &&
                    this.Ctime == other.Ctime &&
                    this.Mtime == other.Mtime &&
                    this.Version == other.Version &&
                    this.Cversion == other.Cversion &&
                    this.Aversion == other.Aversion &&
                    this.DataLength == other.DataLength &&
                    this.NumChildren == other.NumChildren &&
                    this.Pzxid == other.Pzxid;
            }

            /// <summary>
            /// Returns a hash code for this instance.
            /// </summary>
            /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
            public override int GetHashCode()
            {
                int hash = this.Czxid.GetHashCode();
                hash ^= this.Mzxid.GetHashCode();
                hash ^= this.Ctime.GetHashCode();
                hash ^= this.Mtime.GetHashCode();
                hash ^= this.Version.GetHashCode();
                hash ^= this.Cversion.GetHashCode();
                hash ^= this.Aversion.GetHashCode();
                hash ^= this.DataLength.GetHashCode();
                hash ^= this.NumChildren.GetHashCode();
                hash ^= this.Pzxid.GetHashCode();
                return hash;
            }

            /// <summary>
            /// Turns the value into a first stat if it makes sense.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>the original value, or a firststat that is equivalent to it</returns>
            public static IMutableStat TurnIntoFirstStatIfNeeded(IMutableStat value)
            {
                if (value != null &&
                    value.Version == 1 &&
                    value.Aversion == 1 &&
                    value.Cversion == 1 &&
                    value.NumChildren == 0 &&
                    value.Ctime == value.Mtime &&
                    value.Czxid == value.Mzxid &&
                    value.Pzxid == value.Czxid)
                {
                    return new FirstStat(value);
                }

                return value;
            }
        }

        /// <summary>
        /// Class Stat.
        /// </summary>
        [Serializable]
        public sealed class MutableStat : IMutableStat
        {
            /// <summary>
            /// Gets or sets the czxid.
            /// </summary>
            /// <value>The czxid.</value>
            public long Czxid { get; set; }

            /// <summary>
            /// Gets or sets the mzxid.
            /// </summary>
            /// <value>The mzxid.</value>
            public long Mzxid { get; set; }

            /// <summary>
            /// Gets or sets the pzxid.
            /// </summary>
            /// <value>The pzxid.</value>
            public long Pzxid { get; set; }

            /// <summary>
            /// Gets or sets the ctime.
            /// </summary>
            /// <value>The ctime.</value>
            public long Ctime { get; set; }

            /// <summary>
            /// Gets or sets the mtime.
            /// </summary>
            /// <value>The mtime.</value>
            public long Mtime { get; set; }

            /// <summary>
            /// Gets or sets the version.
            /// </summary>
            /// <value>The version.</value>
            public int Version { get; set; }

            /// <summary>
            /// returns the unique incarnation id for this object
            /// </summary>
            public Guid UniqueIncarnationId => GetUniqueIncarnationId(this, false);

            /// <summary>
            /// returns the unique incarnation id for this object, also considering changes on its children.
            /// </summary>
            public Guid UniqueExtendedIncarnationId => GetUniqueIncarnationId(this, true);

            /// <summary>
            /// computes the unique incarnation id for an arbitrary IStat
            /// </summary>
            /// <param name="stat">the stat to evaluate</param>
            /// <param name="useExtended">if true, the returned guid is an extended incarnation id (including children version)</param>
            /// <returns>the unique incarnation id for the stat</returns>
            public static Guid GetUniqueIncarnationId(IMutableStat stat, bool useExtended)
            {
                if (stat == null)
                {
                    return Guid.Empty;
                }

                int a = stat.Version;
                short b = 0;
                short c = 0;
                byte[] bytes = BitConverter.GetBytes(stat.Ctime);

                if (useExtended)
                {
                    b = (short)(((ushort)stat.Cversion) >> 2);
                    c = (short)(stat.Cversion % 0xffff);
                }

                return new Guid(a, b, c, bytes);
            }

            public static int ExtractVersionFromUniqueIncarnationId(Guid uniqueIncarnationId)
            {
                byte[] bytes = uniqueIncarnationId.ToByteArray();
                return BitConverter.ToInt32(bytes, 0);
            }

            /// <summary>
            /// Gets or sets the cversion.
            /// </summary>
            /// <value>The cversion.</value>
            public int Cversion { get; set; }

            /// <summary>
            /// Gets or sets the aversion.
            /// </summary>
            /// <value>The aversion.</value>
            public int Aversion { get; set; }

            /// <summary>
            /// Gets or sets the length of the data.
            /// </summary>
            /// <value>The length of the data.</value>
            public int DataLength { get; set; }

            /// <summary>
            /// Gets or sets the number children.
            /// </summary>
            /// <value>The number children.</value>
            public int NumChildren { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="MutableStat"/> class.
            /// </summary>
            public MutableStat()
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="MutableStat"/> class.
            /// </summary>
            /// <param name="czxid">The czxid.</param>
            /// <param name="mzxid">The mzxid.</param>
            /// <param name="ctime">The ctime.</param>
            /// <param name="mtime">The mtime.</param>
            /// <param name="version">The version.</param>
            /// <param name="cversion">The cversion.</param>
            /// <param name="aversion">The aversion.</param>
            /// <param name="dataLength">Length of the data.</param>
            /// <param name="numChildren">The number children.</param>
            /// <param name="pzxid">The pzxid.</param>
            public MutableStat(long czxid, long mzxid, long ctime, long mtime, int version, int cversion, int aversion, int dataLength, int numChildren, long pzxid)
            {
                this.Czxid = czxid;
                this.Mzxid = mzxid;
                this.Ctime = ctime;
                this.Mtime = mtime;
                this.Version = version;
                this.Cversion = cversion;
                this.Aversion = aversion;
                this.DataLength = dataLength;
                this.NumChildren = numChildren;
                this.Pzxid = pzxid;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="MutableStat"/> class.
            /// </summary>
            /// <param name="other">The other.</param>
            public MutableStat(IStat other)
            {
                if (other == null)
                {
                    throw new ArgumentNullException(nameof(other));
                }

                this.Czxid = other.Czxid;
                this.Mzxid = other.Mzxid;
                this.Ctime = other.Ctime;
                this.Mtime = other.Mtime;
                this.Version = other.Version;
                this.Cversion = other.Cversion;
                this.Aversion = other.Aversion;
                this.DataLength = other.DataLength;
                this.NumChildren = other.NumChildren;
                this.Pzxid = other.Pzxid;
            }

            public MutableStat(IMutableStat other)
                : this((IStat)other)
            {
            }

            /// <summary>
            /// Determines whether the specified <see cref="object"/> is equal to this instance.
            /// </summary>
            /// <param name="obj">The object to compare with the current object.</param>
            /// <returns><c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
            public override bool Equals(object obj)
            {
                IMutableStat other = obj as IMutableStat;
                if (other == null)
                {
                    return false;
                }

                return this.Czxid == other.Czxid &&
                    this.Mzxid == other.Mzxid &&
                    this.Ctime == other.Ctime &&
                    this.Mtime == other.Mtime &&
                    this.Version == other.Version &&
                    this.Cversion == other.Cversion &&
                    this.Aversion == other.Aversion &&
                    this.DataLength == other.DataLength &&
                    this.NumChildren == other.NumChildren &&
                    this.Pzxid == other.Pzxid;
            }

            /// <summary>
            /// Returns a hash code for this instance.
            /// </summary>
            /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
            public override int GetHashCode()
            {
                int hash = this.Czxid.GetHashCode();
                hash ^= this.Mzxid.GetHashCode();
                hash ^= this.Ctime.GetHashCode();
                hash ^= this.Mtime.GetHashCode();
                hash ^= this.Version.GetHashCode();
                hash ^= this.Cversion.GetHashCode();
                hash ^= this.Aversion.GetHashCode();
                hash ^= this.DataLength.GetHashCode();
                hash ^= this.NumChildren.GetHashCode();
                hash ^= this.Pzxid.GetHashCode();
                return hash;
            }

            /// <summary>
            /// Converts the time.
            /// </summary>
            /// <param name="t">The t.</param>
            /// <returns>System.Int64.</returns>
            public static long ConvertTime(DateTime t)
            {
                return t.ToFileTimeUtc();
            }

            /// <summary>
            /// Converts the time.
            /// </summary>
            /// <param name="lt">The lt.</param>
            /// <returns>DateTime.</returns>
            public static DateTime ConvertTime(long lt)
            {
                return DateTime.FromFileTimeUtc(lt);
            }

            /// <summary>
            /// Converts the time span into RM time delta.
            /// </summary>
            /// <param name="ts">The timespan to convert.</param>
            /// <returns>the RM internalized time delta represented by ts</returns>
            public static long ConvertToRmTimeDelta(TimeSpan ts)
            {
                return (long)(ts.TotalMilliseconds * FsTimeTicksPerMillisecond);
            }

            /// <summary>
            /// Windows file time units is 100-nanoseconds, so there is 10k fs ticks in a millisecond:
            /// </summary>
            private const long FsTimeTicksPerMillisecond = 10000;

            /// <summary>
            /// Converts the RM time delta into a timespan.
            /// </summary>
            /// <param name="timedelta">The RM time delta.</param>
            /// <returns>the timespan representing the time delta</returns>
            public static TimeSpan ConvertFromRmTimeDelta(long timedelta)
            {
                return TimeSpan.FromMilliseconds(timedelta / (double)FsTimeTicksPerMillisecond);
            }

            /// <summary>
            /// Returns a <see cref="string" /> that represents this instance.
            /// </summary>
            /// <returns>A <see cref="string" /> that represents this instance.</returns>
            public override string ToString()
            {
                return $"[STAT czxid:{this.Czxid} mzxid:{this.Mzxid} pzxid:{this.Pzxid} "
                    + $"ctime:{ConvertTime(this.Ctime).ToString("o")} "
                    + $"mtime:{ConvertTime(this.Mtime).ToString("o")} version:{this.Version} "
                    + $"cversion:{this.Cversion} aversion:{this.Aversion} "
                    + $"numChildren:{this.NumChildren} uniqueIncId:{this.UniqueIncarnationId} "
                    + $"uniqueExtIncId:{this.UniqueExtendedIncarnationId}]";
            }
        }
    }

    namespace KeeperException
    {
        /// <summary>
        /// Class ExceptionHelper.
        /// </summary>
        public class ExceptionHelper
        {
            /// <summary>
            /// Gets the code string.
            /// </summary>
            /// <param name="rc">The rc.</param>
            /// <returns>System.String.</returns>
            public static string GetCodeString(int rc)
            {
                switch (rc)
                {
                    case (int)Code.Apierror: return "Apierror";
                    case (int)Code.Authfailed: return "Authfailed";
                    case (int)Code.Badarguments: return "Badarguments";
                    case (int)Code.Badversion: return "Badversion";
                    case (int)Code.Connectionloss: return "Connectionloss";
                    case (int)Code.Datainconsistency: return "Datainconsistency";
                    case (int)Code.Invalidacl: return "Invalidacl";
                    case (int)Code.Invalidcallback: return "Invalidcallback";
                    case (int)Code.Marshallingerror: return "Marshallingerror";
                    case (int)Code.Noauth: return "Noauth";
                    case (int)Code.Nochildrenforephemerals: return "Nochildrenforephemerals";
                    case (int)Code.Nodeexists: return "Nodeexists";
                    case (int)Code.Nonode: return "Nonode";
                    case (int)Code.Notempty: return "Notempty";
                    case (int)Code.Ok: return "Ok";
                    case (int)Code.Operationtimeout: return "Operationtimeout";
                    case (int)Code.Runtimeinconsistency: return "Runtimeinconsistency";
                    case (int)Code.Sessionexpired: return "Sessionexpired";
                    case (int)Code.Sessionmoved: return "Sessionmoved";
                    case (int)Code.Systemerror: return "Systemerror";
                    case (int)Code.TransactionNotAgreed: return "TransactionNotAgreed";
                    case (int)Code.Unimplemented: return "Unimplemented";
                    case (int)Code.Unknown: return "Unknown";
                }

                return Enum.IsDefined(typeof(Code), rc) ? ((Code)rc).ToString() : rc.ToString();
            }

            /// <summary>
            /// Gets the exception associated to the result code.
            /// </summary>
            /// <param name="code">The rc.</param>
            /// <returns>Exception, or null if none.</returns>
            public static Exception GetException(Code code)
            {
                string message = $"error came from RM server: {code}";
                switch (code)
                {
                    case Code.Ok:
                        return null;
                    case Code.Nodeexists:
                        return null;
                    case Code.Unimplemented:
                        return new NotSupportedException(message);
                    case Code.Badarguments:
                        return new ArgumentException(message);
                    case Code.Authfailed:
                    case Code.Noauth:
                        return new AuthenticationException(message);
                    case Code.Connectionloss:
                        return new CommunicationException(message);
                    case Code.Operationtimeout:
                    case Code.Sessionexpired:
                        return new TimeoutException(message);
                    case Code.Marshallingerror:
                        return new SerializationException(message);
                    case Code.Badversion:
                    case Code.Invalidacl:
                    case Code.Nochildrenforephemerals:
                        return new InvalidDataException(message);
                    case Code.Notempty:
                        return new ArgumentException(message);
                    case Code.Nonode:
                        return new KeyNotFoundException(message);
                    case Code.Invalidcallback:
                        return new InvalidOperationException(message);
                    case Code.Apierror:
                    case Code.Systemerror:
                    case Code.Unknown:
                    case Code.Runtimeinconsistency:
                    case Code.Datainconsistency:
                    case Code.Sessionmoved:
                        return new SessionMovedException(message);
                    default:
                        return new UnknownException(message);
                }
            }

            [Serializable]
            public class SessionMovedException : Exception
            {
                public SessionMovedException(string message)
                    : base(message)
                {
                }
            }

            [Serializable]
            public class UnknownException : Exception
            {
                public UnknownException(string message)
                    : base(message)
                {
                }
            }

            /// <summary>
            /// Gets the exception associated to the result code.
            /// </summary>
            /// <param name="rc">The rc.</param>
            /// <returns>Exception, or null if none.</returns>
            public static Exception GetException(int rc)
            {
                return GetException(GetCode(rc));
            }

            /// <summary>
            /// Gets the code.
            /// </summary>
            /// <param name="rc">The rc.</param>
            /// <returns>Code.</returns>
            public static Code GetCode(int rc)
            {
                Code code = Code.Unknown;
                if (Enum.IsDefined(typeof(Code), rc))
                {
                    code = (Code)rc;
                }

                return code;
            }

            internal static string GetTypeString(RingMasterRequestType requestType)
            {
                switch (requestType)
                {
                    case RingMasterRequestType.Check: return "Check";
                    case RingMasterRequestType.Create: return "Create";
                    case RingMasterRequestType.Delete: return "Delete";
                    case RingMasterRequestType.Exists: return "Exists";
                    case RingMasterRequestType.GetAcl: return "GetAcl";
                    case RingMasterRequestType.GetChildren: return "GetChildren";
                    case RingMasterRequestType.GetData: return "GetData";
                    case RingMasterRequestType.Init: return "Init";
                    case RingMasterRequestType.Multi: return "Multi";
                    case RingMasterRequestType.Batch: return "Batch";
                    case RingMasterRequestType.Nested: return "Nested";
                    case RingMasterRequestType.None: return "None";
                    case RingMasterRequestType.SetAcl: return "SetAcl";
                    case RingMasterRequestType.SetAuth: return "SetAuth";
                    case RingMasterRequestType.SetData: return "SetData";
                    case RingMasterRequestType.Sync: return "Sync";
                }

                return requestType.ToString();
            }
        }
    }

    namespace Watcher.Event
    {
        /// <summary>
        /// Enum EventType
        /// </summary>
        public enum EventType
        {
            /// <summary>
            /// The none
            /// </summary>
            None = 0,

            /// <summary>
            /// The node children changed
            /// </summary>
            NodeChildrenChanged,

            /// <summary>
            /// The node created
            /// </summary>
            NodeCreated,

            /// <summary>
            /// The node data changed
            /// </summary>
            NodeDataChanged,

            /// <summary>
            /// The node deleted
            /// </summary>
            NodeDeleted,

            /// <summary>
            /// The watcher removed
            /// </summary>
            WatcherRemoved
        }

        /// <summary>
        /// Enum KeeperState
        /// </summary>
        public enum KeeperState
        {
            // Deprecated.
            /// <summary>
            /// The unknown
            /// </summary>
            Unknown = 0,

            /// <summary>
            /// The authentication failed
            /// </summary>
            AuthFailed,

            /// <summary>
            /// The client is in the disconnected state - it is not connected to any server in the ensemble.
            /// </summary>
            Disconnected,

            /// <summary>
            /// The serving cluster has expired this session.
            /// </summary>
            Expired,

            /// <summary>
            /// The no synchronize connected (deprecated).
            /// </summary>
            NoSyncConnected,

            /// <summary>
            /// The client is in the connected state - it is connected to a server in the ensemble
            /// (one of the servers specified in the host connection parameter during ZooKeeper
            /// client creation).
            /// </summary>
            SyncConnected,
        }
    }

    namespace AsyncCallback
    {
        /// <summary>
        /// Delegate StringCallbackDelegate
        /// </summary>
        /// <param name="rc">The rc.</param>
        /// <param name="path">The path.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="name">The name.</param>
        public delegate void StringCallbackDelegate(int rc, string path, object ctx, string name);

        /// <summary>
        /// Interface IStringCallback
        /// </summary>
        public interface IStringCallback
        {
            /// <summary>
            /// Processes the result.
            /// </summary>
            /// <param name="rc">The rc.</param>
            /// <param name="path">The path.</param>
            /// <param name="ctx">The CTX.</param>
            /// <param name="name">The name.</param>
            void ProcessResult(int rc, string path, object ctx, string name);
        }

        /// <summary>
        /// Delegate OpResultCallbackDelegate
        /// </summary>
        /// <param name="rc">The rc.</param>
        /// <param name="res">The list of opresults.</param>
        /// <param name="ctx">The CTX.</param>
        public delegate void OpsResultCallbackDelegate(int rc, IReadOnlyList<OpResult> res, object ctx);

        /// <summary>
        /// Interface IOpsResultCallback
        /// </summary>
        public interface IOpsResultCallback
        {
            /// <summary>
            /// Processes the result.
            /// </summary>
            /// <param name="rc">The rc.</param>
            /// <param name="res">The list of opresults.</param>
            /// <param name="ctx">The CTX.</param>
            void ProcessResult(int rc, IReadOnlyList<OpResult> res, object ctx);
        }

        /// <summary>
        /// Delegate ACLCallbackDelegate
        /// </summary>
        /// <param name="rc">The rc.</param>
        /// <param name="path">The path.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="acl">The acl.</param>
        /// <param name="stat">The stat.</param>
        public delegate void AclCallbackDelegate(int rc, string path, object ctx, IReadOnlyList<Acl> acl, IStat stat);

        /// <summary>
        /// Interface IACLCallback
        /// </summary>
        public interface IAclCallback
        {
            /// <summary>
            /// Processes the result.
            /// </summary>
            /// <param name="rc">The rc.</param>
            /// <param name="path">The path.</param>
            /// <param name="ctx">The CTX.</param>
            /// <param name="acl">The acl.</param>
            /// <param name="stat">The stat.</param>
            void ProcessResult(int rc, string path, object ctx, IReadOnlyList<Acl> acl, IStat stat);
        }

        /// <summary>
        /// Delegate ChildrenCallbackDelegate
        /// </summary>
        /// <param name="rc">The rc.</param>
        /// <param name="path">The path.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="children">The children.</param>
        public delegate void ChildrenCallbackDelegate(int rc, string path, object ctx, IReadOnlyList<string> children);

        /// <summary>
        /// Interface IChildrenCallback
        /// </summary>
        public interface IChildrenCallback
        {
            /// <summary>
            /// Processes the result.
            /// </summary>
            /// <param name="rc">The rc.</param>
            /// <param name="path">The path.</param>
            /// <param name="ctx">The CTX.</param>
            /// <param name="children">The children.</param>
            void ProcessResult(int rc, string path, object ctx, IReadOnlyList<string> children);
        }

        /// <summary>
        /// Delegate Children2CallbackDelegate
        /// </summary>
        /// <param name="rc">The rc.</param>
        /// <param name="path">The path.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="children">The children.</param>
        /// <param name="stat">The stat.</param>
        public delegate void Children2CallbackDelegate(int rc, string path, object ctx, IReadOnlyList<string> children, IStat stat);

        /// <summary>
        /// Interface IChildren2Callback
        /// </summary>
        public interface IChildren2Callback
        {
            /// <summary>
            /// Processes the result.
            /// </summary>
            /// <param name="rc">The rc.</param>
            /// <param name="path">The path.</param>
            /// <param name="ctx">The CTX.</param>
            /// <param name="children">The children.</param>
            /// <param name="stat">The stat.</param>
            void ProcessResult(int rc, string path, object ctx, IReadOnlyList<string> children, IStat stat);
        }

        /// <summary>
        /// Delegate DataCallbackDelegate
        /// </summary>
        /// <param name="rc">The rc.</param>
        /// <param name="path">The path.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="data">The data.</param>
        /// <param name="stat">The stat.</param>
        public delegate void DataCallbackDelegate(int rc, string path, object ctx, byte[] data, IStat stat);

        /// <summary>
        /// Interface IDataCallback
        /// </summary>
        public interface IDataCallback
        {
            /// <summary>
            /// Processes the result.
            /// </summary>
            /// <param name="rc">The rc.</param>
            /// <param name="path">The path.</param>
            /// <param name="ctx">The CTX.</param>
            /// <param name="data">The data.</param>
            /// <param name="stat">The stat.</param>
            void ProcessResult(int rc, string path, object ctx, byte[] data, IStat stat);
        }

        /// <summary>
        /// Delegate StatCallbackDelegate
        /// </summary>
        /// <param name="rc">The rc.</param>
        /// <param name="path">The path.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="stat">The stat.</param>
        public delegate void StatCallbackDelegate(int rc, string path, object ctx, IStat stat);

        /// <summary>
        /// Interface IStatCallback
        /// </summary>
        public interface IStatCallback
        {
            /// <summary>
            /// Processes the result.
            /// </summary>
            /// <param name="rc">The rc.</param>
            /// <param name="path">The path.</param>
            /// <param name="ctx">The CTX.</param>
            /// <param name="stat">The stat.</param>
            void ProcessResult(int rc, string path, object ctx, IStat stat);
        }

        /// <summary>
        /// Delegate VoidCallbackDelegate
        /// </summary>
        /// <param name="rc">The rc.</param>
        /// <param name="path">The path.</param>
        /// <param name="ctx">The CTX.</param>
        public delegate void VoidCallbackDelegate(int rc, string path, object ctx);

        /// <summary>
        /// Interface IVoidCallback
        /// </summary>
        public interface IVoidCallback
        {
            /// <summary>
            /// Processes the result.
            /// </summary>
            /// <param name="rc">The rc.</param>
            /// <param name="path">The path.</param>
            /// <param name="ctx">The CTX.</param>
            void ProcessResult(int rc, string path, object ctx);
        }
    }
}
