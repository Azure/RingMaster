// <copyright file="RequestGetData.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.AsyncCallback;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using GetDataOptions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestGetData.GetDataOptions;
    using IGetDataOptionArgument = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestGetData.IGetDataOptionArgument;
    using RequestDefinitions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// Request to get the data associated with node.
    /// </summary>
    public sealed class RequestGetData : BackendRequestWithContext<RequestDefinitions.RequestGetData, byte[]>
    {
        private readonly DataCallbackDelegate callback;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestGetData"/> class.
        /// </summary>
        /// <param name="path">Path to the node.</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="watcher"><see cref="IWatcher"/> to associate with the node</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="uid">Unique id to assign to the request</param>
        public RequestGetData(string path, object context, IWatcher watcher, DataCallbackDelegate callback, ulong uid = 0)
            : this(path, GetDataOptions.None, context, watcher, callback, uid)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestGetData"/> class.
        /// </summary>
        /// <param name="path">Path to the node.</param>
        /// <param name="faultbackOnParentData">if true, if the requested path doesnt exist, it will return the data for the first ancestor with some non-null data</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="watcher"><see cref="IWatcher"/> to associate with the node</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="uid">Unique id to assign to the request</param>
        public RequestGetData(string path, bool faultbackOnParentData, object context, IWatcher watcher, DataCallbackDelegate callback, ulong uid = 0)
            : this(path, faultbackOnParentData ? GetDataOptions.FaultbackOnParentData : GetDataOptions.None, context, watcher, callback, uid)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestGetData"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="options">the options for this request</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="watcher"><see cref="IWatcher"/> to associate with the node</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="uid">Unique id to assign to the request</param>
        public RequestGetData(string path, GetDataOptions options, object context, IWatcher watcher, DataCallbackDelegate callback, ulong uid = 0)
            : this(path, options, null, context, watcher, callback, uid)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestGetData"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="options">the options for this request</param>
        /// <param name="optionArgument">Arguments for the option</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="watcher"><see cref="IWatcher"/> to associate with the node</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="uid">Unique id to assign to the request</param>
        public RequestGetData(string path, GetDataOptions options, IGetDataOptionArgument optionArgument, object context, IWatcher watcher, DataCallbackDelegate callback, ulong uid = 0)
            : this(new RequestDefinitions.RequestGetData(path, options, optionArgument, watcher, MakeUid(uid)), context, callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestGetData"/> class.
        /// </summary>
        /// <param name="request">Request to wrap</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        public RequestGetData(RequestDefinitions.RequestGetData request, object context, DataCallbackDelegate callback)
            : base(request, context)
        {
            this.callback = callback;
        }

        /// <summary>
        /// Gets or sets the watcher that will be set on the node.
        /// </summary>
        public IWatcher Watcher
        {
            get
            {
                return this.Request.Watcher;
            }

            set
            {
                this.Request.Watcher = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether in the case the requested path not existing, this request will return the data
        /// associated with the first ancestor of the given path that exists and has non-null data on it.
        /// </summary>
        public bool FaultbackOnParentData => this.Request.FaultbackOnParentData;

        /// <summary>
        /// Gets a value indicating whether the result will not contain a stat.
        /// </summary>
        public bool NoStatRequired => this.Request.NoStatRequired;

        /// <summary>
        /// Gets a value indicating whether the path in this request is literal, meaning the wildcards in tree should be ignored.
        /// </summary>
        public bool NoWildcardsForPath => this.Request.NoWildcardsForPath;

        /// <summary>
        /// Gets all options that have been specified for this request
        /// </summary>
        public GetDataOptions Options => this.Request.Options;

        /// <summary>
        /// Gets argument for the specified option (if any).
        /// </summary>
        public IGetDataOptionArgument OptionArgument => this.Request.OptionArgument;

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            if (this.Watcher != null)
            {
                hash ^= this.Watcher.GetHashCode();
            }

            return hash;
        }

        /// <inheritdoc />
        public override bool DataEquals(IRingMasterBackendRequest obj)
        {
            RequestGetData other = obj as RequestGetData;

            if (other == null)
            {
                return false;
            }

            if (!base.DataEquals(other))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified object is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            RequestGetData other = obj as RequestGetData;

            if (other == null)
            {
                return false;
            }

            if (!this.DataEquals(other))
            {
                return false;
            }

            if (this.Watcher != other.Watcher)
            {
                return false;
            }

            if (this.Options != other.Options)
            {
                return false;
            }

            return EqualityHelper.Equals(this.Watcher, other.Watcher);
        }

        /// <inheritdoc />
        protected override void NotifyComplete(int resultCode, byte[] data, IStat stat, string responsePath)
        {
            this.callback?.Invoke(resultCode, this.Path, this.Context, data, stat);
        }
    }
}