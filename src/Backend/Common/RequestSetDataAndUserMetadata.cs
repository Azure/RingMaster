// <copyright file="RequestSetDataAndUserMetadata.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.AsyncCallback;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using RequestDefinitions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// Request to set data and user metadata
    /// </summary>
    public class RequestSetDataAndUserMetadata : BackendRequestWithContext<RequestDefinitions.RequestSetDataAndUserMetadata, NoType>
    {
        private readonly StatCallbackDelegate callback;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestSetDataAndUserMetadata" /> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="context">The context.</param>
        /// <param name="data">The data.</param>
        /// <param name="dataVersion">The data version.</param>
        /// <param name="userMetadata">The user metadata.</param>
        /// <param name="userMetadataVersion">The user metadata version.</param>
        /// <param name="callback">The callback.</param>
        /// <param name="uid">The uid.</param>
        public RequestSetDataAndUserMetadata(
            string path,
            object context,
            byte[] data,
            int dataVersion,
            byte[] userMetadata,
            int userMetadataVersion,
            StatCallbackDelegate callback,
            ulong uid = 0)
            : this(new RequestDefinitions.RequestSetDataAndUserMetadata(path, data, dataVersion, userMetadata, userMetadataVersion, MakeUid(uid)), context, callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestSetDataAndUserMetadata"/> class.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="context">The context.</param>
        /// <param name="callback">The callback.</param>
        public RequestSetDataAndUserMetadata(RequestDefinitions.RequestSetDataAndUserMetadata request, object context, StatCallbackDelegate callback)
            : base(request, context)
        {
            this.callback = callback;
        }

        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <value>
        /// The data.
        /// </value>
        public byte[] Data => this.Request.Data;

        /// <summary>
        /// Gets the data version.
        /// </summary>
        /// <value>
        /// The data version.
        /// </value>
        public int DataVersion => this.Request.DataVersion;

        /// <summary>
        /// Gets the user metadata.
        /// </summary>
        /// <value>
        /// The user metadata.
        /// </value>
        public byte[] UserMetadata => this.Request.UserMetadata;

        /// <summary>
        /// Gets the expected user metadata version of data on the node.
        /// </summary>
        public int UserMetadataVersion => this.Request.UserMetadataVersion;

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            hash ^= this.DataVersion.GetHashCode();
            if (this.Data != null)
            {
                int arrayhash = this.Data.Length;

                for (int i = 0; i < this.Data.Length; i++)
                {
                    hash ^= this.Data[i].GetHashCode() << (i % 32);
                }

                hash ^= arrayhash;
            }

            hash ^= this.UserMetadataVersion.GetHashCode();
            if (this.UserMetadata != null)
            {
                int arrayhash = this.UserMetadata.Length;

                for (int i = 0; i < this.UserMetadata.Length; i++)
                {
                    hash ^= this.UserMetadata[i].GetHashCode() << (i % 32);
                }

                hash ^= arrayhash;
            }

            return hash;
        }

        /// <inheritdoc />
        public override bool DataEquals(IRingMasterBackendRequest obj)
        {
            RequestSetDataAndUserMetadata other = obj as RequestSetDataAndUserMetadata;

            if ((this.DataVersion != other?.DataVersion) || (this.UserMetadataVersion != other?.UserMetadataVersion))
            {
                return false;
            }

            if (!base.DataEquals(other))
            {
                return false;
            }

            return EqualityHelper.Equals(this.Data, other.Data) && EqualityHelper.Equals(this.UserMetadata, other.UserMetadata);
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
