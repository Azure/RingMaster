// <copyright file="IWatcher.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    /// <summary>
    /// Interface IWatcher
    /// </summary>
    public interface IWatcher
    {
        /// <summary>
        /// Gets the unique id of this watcher.
        /// </summary>
        ulong Id { get; }

        /// <summary>
        /// Gets a value indicating whether the watcher is for a single use only.
        /// </summary>
        /// <value><c>true</c> if this is a single-use watcher; otherwise, <c>false</c>.</value>
        bool OneUse { get; }

        /// <summary>
        /// Processes the specified event.
        /// </summary>
        /// <param name="evt">The event</param>
        void Process(WatchedEvent evt);
    }
}