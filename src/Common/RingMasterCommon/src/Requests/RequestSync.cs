// <copyright file="RequestSync.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    /// <summary>
    /// Request to sync a node.
    /// </summary>
    public class RequestSync : AbstractRingMasterRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestSync"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="uid">Unique Id of the request</param>
        public RequestSync(string path, ulong uid = 0)
            : base(RingMasterRequestType.Sync, path, uid)
        {
        }

        /// <summary>
        /// Is this request readonly
        /// </summary>
        /// <returns>true if the request is read only</returns>
        public override bool IsReadOnly()
        {
            return true;
        }
    }
}
