// <copyright file="RequestCheck.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.AsyncCallback;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using RequestDefinitions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// Request to check if the Version, CVersion and AVersion on the node are equal to the values
    /// specified in this request.
    /// </summary>
    public sealed class RequestCheck : BackendRequestNoResult<RequestDefinitions.RequestCheck>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestCheck"/> class.
        /// </summary>
        /// <param name="path">Path of the node</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="version">Expected value of the node's data version</param>
        /// <param name="cversion">Expected value of the node's children version</param>
        /// <param name="aversion">Expected value of the node's <see cref="Acl"/> version</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="uid">Unique id to assign to the request</param>
        public RequestCheck(string path, object context, int version, int cversion, int aversion, VoidCallbackDelegate callback, ulong uid = 0)
            : this(path, context, version, cversion, aversion, Guid.Empty, RequestDefinitions.RequestCheck.UniqueIncarnationIdType.None, callback, uid)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestCheck"/> class.
        /// </summary>
        /// <param name="path">Path of the node</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="version">Expected value of the node's data version</param>
        /// <param name="uniqueIncarnation">Expected value of the unique incarnation id</param>
        /// <param name="uniqueIncarnationIdKind">Expected kind of unique incarnation id</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="uid">Unique id to assign to the request</param>
        public RequestCheck(
            string path,
            object context,
            int version,
            Guid uniqueIncarnation,
            RequestDefinitions.RequestCheck.UniqueIncarnationIdType uniqueIncarnationIdKind,
            VoidCallbackDelegate callback,
            ulong uid = 0)
            : this(path, context, version, -1, -1, uniqueIncarnation, uniqueIncarnationIdKind, callback, uid)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestCheck"/> class.
        /// </summary>
        /// <param name="path">Path of the node</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="version">Expected value of the node's data version</param>
        /// <param name="cversion">Expected value of the node's children version</param>
        /// <param name="aversion">Expected value of the node's <see cref="Acl"/> version</param>
        /// <param name="uniqueIncarnation">Expected value of the unique incarnation id</param>
        /// <param name="uniqueIncarnationIdKind">Expected kind of unique incarnation id</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="uid">Unique id to assign to the request</param>
        public RequestCheck(
            string path,
            object context,
            int version,
            int cversion,
            int aversion,
            Guid uniqueIncarnation,
            RequestDefinitions.RequestCheck.UniqueIncarnationIdType uniqueIncarnationIdKind,
            VoidCallbackDelegate callback,
            ulong uid = 0)
            : this(new RequestDefinitions.RequestCheck(path, version, cversion, aversion, uniqueIncarnation, uniqueIncarnationIdKind, MakeUid(uid)), context, callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestCheck"/> class.
        /// </summary>
        /// <param name="request">Request to wrap</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        public RequestCheck(
            RequestDefinitions.RequestCheck request,
            object context,
            VoidCallbackDelegate callback)
            : base(request, context, callback)
        {
        }

        /// <summary>
        /// Gets the expected unique incarnation Id.
        /// </summary>
        public Guid UniqueIncarnationId => this.Request.UniqueIncarnationId;

        /// <summary>
        /// Gets the type of unique incarnation id.
        /// </summary>
        public RequestDefinitions.RequestCheck.UniqueIncarnationIdType UniqueIncarnationIdKind => this.Request.UniqueIncarnationIdKind;

        /// <summary>
        /// Gets the expected data version.
        /// </summary>
        public override int Version => this.Request.Version;

        /// <summary>
        /// Gets the expected children version.
        /// </summary>
        public int CVersion => this.Request.CVersion;

        /// <summary>
        /// Gets the expected <see cref="Acl"/> version.
        /// </summary>
        public int AVersion => this.Request.AVersion;
    }
}