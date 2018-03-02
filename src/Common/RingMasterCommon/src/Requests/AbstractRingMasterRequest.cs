// <copyright file="AbstractRingMasterRequest.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    using System;
    using System.Collections.Generic;
    using RingMaster.Data;

    /// <summary>
    /// Base class for classes that implement <see cref="IRingMasterRequest"/>.
    /// </summary>
    public abstract class AbstractRingMasterRequest : IRingMasterRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractRingMasterRequest"/> class.
        /// </summary>
        /// <param name="requestType">Type of the request</param>
        /// <param name="path">The path.</param>
        /// <param name="uid">UniqueId of the request</param>
        public AbstractRingMasterRequest(RingMasterRequestType requestType, string path, ulong uid)
        {
            this.RequestType = requestType;
            this.Uid = uid;
            this.Path = path;

            this.ExecutionQueueId = Guid.Empty;
            this.ExecutionQueueTimeoutMillis = 0;
        }

        /// <summary>
        /// Gets the type of the request.
        /// </summary>
        public RingMasterRequestType RequestType { get; private set; }

        /// <summary>
        /// Gets or sets the unique id of the request.
        /// </summary>
        public ulong Uid { get; set; }

        /// <summary>
        /// Gets or sets the id of the time stream associated with this request.
        /// </summary>
        public ulong TimeStreamId { get; set; }

        /// <summary>
        /// Gets or sets the path of the node associated with this request.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the authentication credentials to use for this request or <c>null</c> of the session's authentication credentials must be used.
        /// </summary>
        public virtual ISessionAuth Auth { get; set; }

        /// <summary>
        /// Gets or sets the information about which transaction id and transaction time to use for this request or null to indicate that ring master must
        /// assign those values.
        /// </summary>
        public IOperationOverrides Overrides { get; set; }

        /// <summary>
        /// Gets or sets the execution queue id to queue this command into.
        /// </summary>
        public Guid ExecutionQueueId { get; set; }

        /// <summary>
        /// Gets or sets the maximum time allowed for this request to be waiting on the execution queue.
        /// </summary>
        public int ExecutionQueueTimeoutMillis { get; set; }

        /// <summary>
        /// Gets a value indicating whether this request is readonly.
        /// </summary>
        /// <returns><c>true</c> if this request is read only</returns>
        public abstract bool IsReadOnly();
    }
}
