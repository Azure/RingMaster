// <copyright file="RequestDelete.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    /// <summary>
    /// Request to delete a node.
    /// </summary>
    public class RequestDelete : AbstractRingMasterRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestDelete"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="version">Version of the node must match this value for delete to succeed</param>
        /// <param name="cascade">If true, the delete will recursively delete child nodes</param>
        /// <param name="uid">Unique Id of the request</param>
        public RequestDelete(string path, int version, bool cascade, ulong uid = 0)
            : this(path, version, cascade ? DeleteMode.CascadeDelete : DeleteMode.None, uid)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestDelete"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="version">Version of the node must match this value for delete to succeed</param>
        /// <param name="deletemode">The delete options for the operation</param>
        /// <param name="uid">Unique Id of the request</param>
        public RequestDelete(string path, int version, DeleteMode deletemode = DeleteMode.None, ulong uid = 0)
            : base(RingMasterRequestType.Delete, path, uid)
        {
            this.Version = version;
            this.DeleteMode = deletemode;
        }

        /// <summary>
        /// Gets the expected value of the version of the node.
        /// </summary>
        public int Version { get; private set; }

        /// <summary>
        /// Gets a value indicating whether all child nodes will be deleted recursively.
        /// </summary>
        public bool IsCascade
        {
            get
            {
                return (this.DeleteMode & DeleteMode.CascadeDelete) == DeleteMode.CascadeDelete;
            }
        }

        /// <summary>
        /// Gets the delete mode.
        /// </summary>
        /// <value>The delete mode.</value>
        public DeleteMode DeleteMode { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this this request is read only.
        /// </summary>
        /// <returns><c>false</c> because this request deletes a node</returns>
        public override bool IsReadOnly()
        {
            return false;
        }
    }
}
