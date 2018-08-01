// <copyright file="BulkWatcherSpec.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Text;

    /// <summary>
    /// Class BulkWatcherSpec.
    /// </summary>
    internal class BulkWatcherSpec : IWatcher
    {
        /// <summary>
        /// The identifier
        /// </summary>
        private readonly string id;

        /// <summary>
        /// The _watcher
        /// </summary>
        private readonly IWatcher bulkWatcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWatcherSpec"/> class.
        /// </summary>
        /// <param name="specStr">The spec string.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="watcher">The watcher.</param>
        /// <exception cref="NotImplementedException">If subpath is undefined</exception>
        public BulkWatcherSpec(string specStr, string id, IWatcher watcher)
        {
            this.id = id;
            this.bulkWatcher = watcher;
            this.ProcessSpec(specStr);
        }

        /// <summary>
        /// Gets the identifier of the bulk watcher spec
        /// </summary>
        public string Id => this.id;

        /// <summary>
        /// Gets or sets the subpath
        /// </summary>
        public string Subpath { get; set; }

        /// <summary>
        /// Gets the identifier of the watcher
        /// </summary>
        ulong IWatcher.Id
        {
            get { return 0; }
        }

        /// <summary>
        /// Gets a value indicating whether the watcher is for a single use only.
        /// </summary>
        public bool OneUse => this.Kind.HasFlag(WatcherKind.OneUse);

        /// <summary>
        /// Gets the kind of the watcher, if it is for single use and if the data is included on notification
        /// </summary>
        public WatcherKind Kind
        {
            get { return this.bulkWatcher.Kind; }
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
        /// Processes the specified evt.
        /// </summary>
        /// <param name="evt">The evt.</param>
        public void Process(WatchedEvent evt)
        {
            this.bulkWatcher.Process(evt);
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
