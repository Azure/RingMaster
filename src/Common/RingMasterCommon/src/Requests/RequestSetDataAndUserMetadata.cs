// <copyright file="RequestSetDataAndUserMetadata.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    /// <summary>
    /// Request to set data and user metadata
    /// </summary>
    /// <seealso cref="Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.AbstractRingMasterRequest" />
    public class RequestSetDataAndUserMetadata : AbstractRingMasterRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestSetDataAndUserMetadata" /> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="data">The data.</param>
        /// <param name="dataVersion">The data version.</param>
        /// <param name="userMetadata">The user metadata.</param>
        /// <param name="userMetadataVersion">The user metadata version.</param>
        /// <param name="uid">The uid.</param>
        /// <param name="invokeCallbackBeforeComplete">if set to <c>true</c> [invoke callback before complete].</param>
        public RequestSetDataAndUserMetadata(string path, byte[] data, int dataVersion, byte[] userMetadata, int userMetadataVersion, ulong uid = 0, bool invokeCallbackBeforeComplete = false)
            : base(RingMasterRequestType.SetDataAndUserMetadata, path, uid, invokeCallbackBeforeComplete)
        {
            this.Data = data;
            this.DataVersion = dataVersion;
            this.UserMetadata = userMetadata;
            this.UserMetadataVersion = userMetadataVersion;
        }

        /// <summary>
        /// Gets the user metadata.
        /// </summary>
        /// <value>
        /// The user metadata.
        /// </value>
        public byte[] UserMetadata { get; private set; }

        /// <summary>
        /// Gets the version.
        /// </summary>
        /// <value>
        /// The version.
        /// </value>
        public int UserMetadataVersion { get; private set; }

        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <value>
        /// The data.
        /// </value>
        public byte[] Data { get; private set; }

        /// <summary>
        /// Gets the data version.
        /// </summary>
        /// <value>
        /// The data version.
        /// </value>
        public int DataVersion { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this request is readonly.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this request is read only
        /// </returns>
        public override bool IsReadOnly()
        {
            return false;
        }
    }
}
