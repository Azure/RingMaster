// ***********************************************************************
// Assembly         : RingMaster
// <copyright file="BulkWatcherSpec.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.KeeperException;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Watcher.Event;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Class BulkWatcherSpec.
    /// </summary>
    internal class BulkWatcherSpec : IWatcher
    {
        /// <summary>
        /// The identifier
        /// </summary>
        public readonly string Id;
        /// <summary>
        /// The subpath
        /// </summary>
        public string Subpath;

        /// <summary>
        /// The _watcher
        /// </summary>
        private readonly IWatcher _watcher;

        ulong IWatcher.Id { get { return 0; } }

        /// <summary>
        /// Gets a value indicating whether [one use].
        /// </summary>
        /// <value><c>true</c> if [one use]; otherwise, <c>false</c>.</value>
        public bool OneUse { get { return this._watcher.OneUse; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWatcherSpec"/> class.
        /// </summary>
        /// <param name="specStr">The spec string.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="watcher">The watcher.</param>
        public BulkWatcherSpec(string specStr, string id, IWatcher watcher)
        {
            this.Id = id;
            this._watcher = watcher;
            ProcessSpec(specStr);
        }

        /// <summary>
        /// Processes the spec.
        /// </summary>
        /// <param name="specStr">The spec string.</param>
        /// <exception cref="System.NotImplementedException">unknwon spec:  + specStr</exception>
        public void ProcessSpec(string specStr)
        {
            string[] pieces = specStr.Split(',');
            this.Subpath = null;

            for (int i = 0; i < pieces.Length; i++)
            {
                if (pieces[i].StartsWith("$startswith:"))
                {
                    this.Subpath = pieces[i].Substring("$startswith:".Length);
                }
            }

            if (this.Subpath == null)
            {
                throw new NotImplementedException("unknwon spec: " + specStr);
            }
        }

        /// <summary>
        /// Creates the specified spec.
        /// </summary>
        /// <param name="spec">The spec.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="watcher">The watcher.</param>
        /// <returns>BulkWatcherSpec.</returns>
        public static BulkWatcherSpec Create(byte[] spec, string id, IWatcher watcher)
        {
            BulkWatcherSpec result = null;

            try
            {
                string specStr = Encoding.UTF8.GetString(spec);
                result = new BulkWatcherSpec(specStr, id, watcher);
            }
            catch (Exception)
            {
                // ignored
            }

            return result;
        }

        /// <summary>
        /// Processes the specified evt.
        /// </summary>
        /// <param name="evt">The evt.</param>
        public void Process(WatchedEvent evt)
        {
            this._watcher.Process(evt);
        }

        /// <summary>
        /// Applieses the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if this bulkwatcher applies to the given path, <c>false</c> otherwise.</returns>
        internal bool Applies(string path)
        {
            return path.StartsWith(this.Subpath);
        }
    }
}