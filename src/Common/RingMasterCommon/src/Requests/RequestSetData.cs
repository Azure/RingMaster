// <copyright file="RequestSetData.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    /// <summary>
    /// Request to change the data associated with a node.
    /// </summary>
    public class RequestSetData : AbstractRingMasterRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestSetData"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="data">Data to set on the node</param>
        /// <param name="version">Expected version of data on the node</param>
        /// <param name="dataCommand">Indicates whether the data is an encoded command</param>
        /// <param name="uid">Unique Id of the request</param>
        public RequestSetData(string path, byte[] data, int version, bool dataCommand = false, ulong uid = 0)
            : base(RingMasterRequestType.SetData, path, uid)
        {
            this.Data = data;
            this.Version = version;
            this.IsDataCommand = dataCommand;
        }

        /// <summary>
        /// Gets the data that must be set on the node.
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// Gets the expected version of data on the node.
        /// </summary>
        /// <value>The version.</value>
        public int Version { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the contents are encoded commands that specify how the node's data must be manipulated.
        /// </summary>
        public bool IsDataCommand { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this this request is read only.
        /// </summary>
        /// <returns><c>false</c> because this request modifies the data of the node</returns>
        public override bool IsReadOnly()
        {
            return false;
        }
    }
}
