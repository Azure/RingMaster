// <copyright file="RequestMulti.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    using System.Collections.Generic;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Request to execute a list of <see cref="IRingMasterRequest"/>s as a transaction.
    /// </summary>
    public class RequestMulti : AbstractRingMasterCompoundRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestMulti"/> class.
        /// </summary>
        /// <param name="operations">List of <see cref="Op"/>s to include in the batch</param>
        /// <param name="completeSynchronously"><c>true</c> if the server must ensure durability before returning</param>
        /// <param name="uid">Unique Id of the request</param>
        public RequestMulti(IReadOnlyList<Op> operations, bool completeSynchronously, ulong uid = 0)
            : this(AbstractRingMasterCompoundRequest.GetRequests(operations), completeSynchronously, null, uid)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestMulti"/> class.
        /// </summary>
        /// <param name="operations">List of <see cref="Op"/>s to include in the batch</param>
        /// <param name="completeSynchronously"><c>true</c> if the server must ensure durability before returning</param>
        /// <param name="scheduledName">if not null makes this command be inserted with the given name (must be unique) into the RingMaster backend scheduler command queue for later background execution</param>
        /// <param name="uid">Unique Id of the request</param>
        public RequestMulti(IReadOnlyList<Op> operations, bool completeSynchronously, string scheduledName, ulong uid = 0)
            : this(AbstractRingMasterCompoundRequest.GetRequests(operations), completeSynchronously, scheduledName, uid)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestMulti"/> class.
        /// </summary>
        /// <param name="requests">List of <see cref="IRingMasterRequest"/>s to include in the batch</param>
        /// <param name="completeSynchronously"><c>true</c> if the server must ensure durability before returning</param>
        /// <param name="scheduledName">if not null makes this command be inserted with the given name (must be unique) into the RingMaster backend scheduler command queue for later background execution</param>
        /// <param name="uid">Unique Id of the request</param>
        public RequestMulti(IReadOnlyList<IRingMasterRequest> requests, bool completeSynchronously, string scheduledName = null, ulong uid = 0)
            : base(RingMasterRequestType.Multi, requests, completeSynchronously, uid)
        {
            this.ScheduledName = scheduledName;
        }

        /// <summary>
        /// Gets or sets a string, which if not null makes this command be inserted with the given name (must be unique) into the RingMaster backend scheduler command queue for later background execution
        /// </summary>
        /// <value>The name of the scheduled command, or null if the command will be executed now.</value>
        public string ScheduledName
        {
            get;
            set;
        }
    }
}
