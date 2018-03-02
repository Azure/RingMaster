// <copyright file="IRingMaster.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using RingMaster.Data;

    /// <summary>
    /// RingMaster Interface
    /// </summary>
    public interface IRingMaster : IDisposable
    {
        /// <summary>
        /// Sets the Identity that will be used for access control checks.
        /// </summary>
        /// <param name="clientId">Identity of the client</param>
        /// <returns>Task that tracks the execution of this method</returns>
        Task SetAuth(Id clientId);

        /// <summary>
        /// Creates a node with the given path.
        /// </summary>
        /// <param name="path">Node Path</param>
        /// <param name="data">Data to associate with the node</param>
        /// <param name="acl">Access Control List</param>
        /// <param name="createMode">Specifies the node will be created</param>
        /// <returns>Task that will resolve on success to the path to the newly created node</returns>
        Task<string> Create(string path, byte[] data, IReadOnlyList<Acl> acl, CreateMode createMode);

        /// <summary>
        /// Moves a node with the given path.
        /// </summary>
        /// <param name="pathSrc">Node Path to move</param>
        /// <param name="version">version of the source node</param>
        /// <param name="pathDst">Node Path to be parent of the moved node</param>
        /// <param name="moveMode">Modifiers for the move operation</param>
        /// <returns>Task that will resolve on success to the path to the newly created node</returns>
        Task<string> Move(string pathSrc, int version, string pathDst, MoveMode moveMode);

        /// <summary>
        /// Deletes the node with the given path.
        /// </summary>
        /// <param name="path">Node path</param>
        /// <param name="version">Node will be deleted only if this
        /// value matches the current version of the node</param>
        /// <param name="mode">The delete mode of the operation</param>
        /// <returns>Task that will resolve on success to either <c>true</c> if the node
        /// was successfully deleted or<c>false</c> if no node was found at that path.</returns>
        [SuppressMessage("Microsoft.MSInternal", "CA908:AvoidTypesThatRequireJitCompilationInPrecompiledAssemblies", Justification = "We are not using ngen")]
        Task<bool> Delete(string path, int version, DeleteMode mode = DeleteMode.None);

        /// <summary>
        /// Queries the <see cref="Stat"/> of the node with the given path.
        /// </summary>
        /// <param name="path">Node path</param>
        /// <param name="watcher">Watcher interface that receives notifications for changes to this path or null</param>
        /// <returns>Task that will resolve on success to the <see cref="Stat"/> associated with the node</returns>
        Task<IStat> Exists(string path, IWatcher watcher);

        /// <summary>
        /// Gets the list of children of the node at the given path.
        /// </summary>
        /// <param name="path">Node path</param>
        /// <param name="watcher">Watcher interface that receives notifications for changes to this path or null</param>
        /// <param name="retrievalCondition">If not null, the retrieval condition in the form >:[top]:[startingChildName].
        /// valid interval definitions:
        /// <c>
        ///   ">:[Top]:[ChildName]"     ... returns the elements greater than the [ChildName] limited to Top count
        ///                                 so ">:1000:contoso" means give me first 1000 childrens greater than contoso
        ///                                 so ">:1000:"        means give me first 1000 elements
        /// </c>
        /// </param>
        /// <returns>Task that will resolve on success to the list of names of children of the node</returns>
        Task<IReadOnlyList<string>> GetChildren(string path, IWatcher watcher, string retrievalCondition = null);

        /// <summary>
        /// Gets the data associated with the node at the given path.
        /// </summary>
        /// <param name="path">Node path</param>
        /// <param name="watcher">Watcher interface that receives notifications for changes to this path or null</param>
        /// <returns>Task that will resolve on success to the data associated with the node</returns>
        Task<byte[]> GetData(string path, IWatcher watcher);

        /// <summary>
        /// Sets the data for the node at the given path if the given version matches
        /// the current version of the node (If the given version is -1, it matches any version).
        /// </summary>
        /// <param name="path">Node path</param>
        /// <param name="data">Data to associate with the node</param>
        /// <param name="version">Version to compare with the current version of the node</param>
        /// <returns>Task that will resolve on success to the <see cref="Stat"/> associated with the node</returns>
        Task<IStat> SetData(string path, byte[] data, int version);

        /// <summary>
        /// Gets the Access Control List associated with a node.
        /// </summary>
        /// <param name="path">Node path</param>
        /// <param name="stat"><see cref="Stat"/> associated with the node</param>
        /// <returns>Task that will resolve on success to a List of <see cref="Acl"/>s associated
        /// with the node</returns>
        Task<IReadOnlyList<Acl>> GetACL(string path, IStat stat);

        /// <summary>
        /// Sets the access control list for the node at the given path if
        /// the given version matches the current version of the node.
        /// </summary>
        /// <param name="path">Node path</param>
        /// <param name="acl">Access control list to associate with the node</param>
        /// <param name="version">Version to compare with the current version of the node</param>
        /// <returns>Task that will resolve on success to the <see cref="Stat"/> associated with
        /// the node</returns>
        Task<IStat> SetACL(string path, IReadOnlyList<Acl> acl, int version);

        /// <summary>
        /// Synchronizes with the given path.
        /// </summary>
        /// <param name="path">Node path</param>
        /// <returns>Task that tracks the synchronization</returns>
        Task Sync(string path);

        /// <summary>
        /// Executes multiple operations as an atomic group at the server. Either the whole list takes
        /// effect, or no operation do.
        /// </summary>
        /// <param name="operations">List of operations</param>
        /// <param name="mustCompleteSynchronously">If <c>true</c> the server does not complete the operation
        /// until changes are guaranteed to be durable (and are applied locally).</param>
        /// <returns>Task that will resolve on success to a list of
        /// <see cref="OpResult"/>s</returns>
        Task<IReadOnlyList<OpResult>> Multi(IReadOnlyList<Op> operations, bool mustCompleteSynchronously = false);

        /// <summary>
        /// Executes multiple operations in a sequence at the server. No atomicity guarantees are provided.
        /// </summary>
        /// <param name="operations">List of operations</param>
        /// <param name="mustCompleteSynchronously">If <c>true</c> the server does not complete the operation
        /// until all successful operations are guaranteed to be durable (and are applied locally).</param>
        /// <returns>Task that will resolve on success to a list of <see cref="OpResult"/>s</returns>
        Task<IReadOnlyList<OpResult>> Batch(IReadOnlyList<Op> operations, bool mustCompleteSynchronously = false);
    }
}