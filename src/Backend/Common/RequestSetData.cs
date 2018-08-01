// <copyright file="RequestSetData.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.AsyncCallback;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using RequestDefinitions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// Request to change the data associated with a node.
    /// </summary>
    public sealed class RequestSetData : BackendRequestWithContext<RequestDefinitions.RequestSetData, NoType>
    {
        private readonly StatCallbackDelegate callback;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestSetData"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="data">Data to set on the node</param>
        /// <param name="version">Expected version of data on the node</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="dataCommand">Indicates whether the data is an encoded command</param>
        /// <param name="uid">Unique Id to assign to the request</param>
        public RequestSetData(
            string path,
            object context,
            byte[] data,
            int version,
            StatCallbackDelegate callback,
            bool dataCommand = false,
            ulong uid = 0)
            : this(new RequestDefinitions.RequestSetData(path, data, version, dataCommand, MakeUid(uid)), context, callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestSetData"/> class.
        /// </summary>
        /// <param name="request">Request to wrap</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        public RequestSetData(RequestDefinitions.RequestSetData request, object context, StatCallbackDelegate callback)
            : base(request, context)
        {
            this.callback = callback;
        }

        /// <summary>
        /// Gets the data that must be set on the node.
        /// </summary>
        public byte[] Data => this.Request.Data;

        /// <summary>
        /// Gets the expected version of data on the node.
        /// </summary>
        public int Version => this.Request.Version;

        /// <summary>
        /// Gets a value indicating whether the contents are encoded commands that specify how the node's data must be manipulated.
        /// </summary>
        public bool IsDataCommand => this.Request.IsDataCommand;

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            hash ^= this.Version.GetHashCode();

            hash ^= this.IsDataCommand.GetHashCode();

            if (this.Data != null)
            {
                int arrayhash = this.Data.Length;

                for (int i = 0; i < this.Data.Length; i++)
                {
                    hash ^= this.Data[i].GetHashCode() << (i % 32);
                }

                hash ^= arrayhash;
            }

            return hash;
        }

        /// <inheritdoc />
        public override bool DataEquals(IRingMasterBackendRequest obj)
        {
            RequestSetData other = obj as RequestSetData;

            if (this.Version != other?.Version)
            {
                return false;
            }

            if (this.IsDataCommand != other.IsDataCommand)
            {
                return false;
            }

            if (!base.DataEquals(other))
            {
                return false;
            }

            return EqualityHelper.Equals(this.Data, other.Data);
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified object is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return this.DataEquals(obj as IRingMasterBackendRequest);
        }

        /// <inheritdoc />
        protected override void NotifyComplete(int resultCode, NoType ign, IStat stat, string responsePath)
        {
            this.callback?.Invoke(resultCode, this.Path, this.Context, stat);
        }
    }
}