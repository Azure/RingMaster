// <copyright file="DelegateWatcher.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Data
{
    using System;
    
    /// <summary>
    /// Class DelegateWatcher.
    /// </summary>
    public class DelegateWatcher : IWatcher
    {
        /// <summary>
        /// The on process
        /// </summary>
        private Action<WatchedEvent> onProcess;

        /// <summary>
        /// Initializes a new instance of the DelegateWatcher class.
        /// </summary>
        /// <param name="onProcess">The on process.</param>
        /// <param name="oneUse">if set to true the watcher is for one use (meaning it will be fired exactly once).</param>
        /// <exception cref="System.ArgumentNullException">onProcess cannot be null</exception>
        public DelegateWatcher(Action<WatchedEvent> onProcess, bool oneUse = true)
        {
            if (onProcess == null)
            {
                throw new ArgumentNullException("onProcess");
            }

            this.OneUse = oneUse;
            this.onProcess = onProcess;
        }

        /// <summary>
        /// Gets or sets the unique id of this watcher.
        /// </summary>
        /// <value>The identifier for this watcher.</value>
        public ulong Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is a one use watcher or reusable.
        /// </summary>
        /// <value><c>true</c> if [one use]; otherwise, <c>false</c>.</value>
        public bool OneUse { get; set; }

        /// <summary>
        /// Processes the specified event.
        /// </summary>
        /// <param name="evt">The event.</param>
        public void Process(WatchedEvent evt)
        {
            this.onProcess(evt);
        }
    }
}