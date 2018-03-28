// <copyright file="IRingMasterBackendRequest.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using IRingMasterRequest = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.IRingMasterRequest;

    /// <summary>
    /// IRingMasterBackendRequest interface extends IRingMasterRequest with support for adding callbacks to be invoked after the
    /// request has been completed.
    /// </summary>
    public interface IRingMasterBackendRequest : IRingMasterRequest
    {
        /// <summary>
        /// Gets the <see cref="IRingMasterRequest"/> that is being wrapped by this request.
        /// </summary>
        IRingMasterRequest WrappedRequest { get; }

        /// <summary>
        /// Sets the on after complete.
        /// </summary>
        /// <param name="onComplete">The on complete.</param>
        void SetOnAfterComplete(RequestDelegates.OnAfterComplete onComplete);

        /// <summary>
        /// Notifies the complete.
        /// </summary>
        /// <param name="resultCode">The result code.</param>
        /// <param name="result">The result.</param>
        /// <param name="stat">The stat.</param>
        void NotifyComplete(int resultCode, object result, IStat stat);

        /// <summary>
        /// Datas the equals.
        /// </summary>
        /// <param name="ringMasterRequest">The ring master request.</param>
        /// <returns><c>true</c> if objects are equal (including data), <c>false</c> otherwise.</returns>
        bool DataEquals(IRingMasterBackendRequest ringMasterRequest);
    }
}