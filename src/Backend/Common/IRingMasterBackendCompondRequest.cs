// <copyright file="IRingMasterBackendCompondRequest.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.AsyncCallback;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    using Code = Microsoft.Azure.Networking.Infrastructure.RingMaster.Data.RingMasterException.Code;
    using GetDataOptions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestGetData.GetDataOptions;
    using IGetDataOptionArgument = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestGetData.IGetDataOptionArgument;
    using IRingMasterRequest = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.IRingMasterRequest;
    using ISessionAuth = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.ISessionAuth;
    using RequestDefinitions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using RingMasterRequestType = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RingMasterRequestType;

    /// <summary>
    /// IRingMasterBackendCompoundRequest defines a request that is a collection of other requests.
    /// </summary>
    public interface IRingMasterBackendCompondRequest : IRingMasterBackendRequest
    {
        /// <summary>
        /// Gets or sets a value indicating whether the request should be completed synchronously
        /// </summary>
        bool CompleteSynchronously { get; set; }

        /// <summary>
        /// Gets the list of requests in the compound request
        /// </summary>
        IReadOnlyList<IRingMasterBackendRequest> Requests { get; }
    }
}