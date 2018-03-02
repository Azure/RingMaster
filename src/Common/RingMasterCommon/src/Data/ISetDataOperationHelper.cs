// <copyright file="ISetDataOperationHelper.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Data
{
    /// <summary>
    /// This interface abstracts the composition of SetData operations.
    /// </summary>
    public interface ISetDataOperationHelper
    {
        /// <summary>
        /// Returns the value corresponding to the byte[] stored in the node
        /// </summary>
        /// <param name="bytes">The data as retrieved from the node</param>
        /// <returns>Value encoded in the given data</returns>
        long GetValue(byte[] bytes);

        /// <summary>
        /// Create a <see cref="ISetDataOperation"/> that represents a <c>InterlockedAdd(value)</c> operation.
        /// </summary>
        /// <param name="value">Value to <c>ADD</c> with the data already in the node</param>
        /// <returns>A <see cref="ISetDataOperation"/> that represents the encoded operation as a data command</returns>
        ISetDataOperation InterlockedAdd(long value);

        /// <summary>
        /// Create a <see cref="ISetDataOperation"/> that represents a <c>InterlockedXor(value)</c> operation.
        /// </summary>
        /// <param name="value">Value to <c>XOR</c> with the data already in the node</param>
        /// <returns>A <see cref="ISetDataOperation"/> that represents the encoded operation as a data command</returns>
        ISetDataOperation InterlockedXOR(long value);

        /// <summary>
        /// Create a <see cref="ISetDataOperation"/> that represents a <c>InterlockedSet(value)</c> operation.
        /// </summary>
        /// <param name="value">value to set</param>
        /// <returns>A <see cref="ISetDataOperation"/> that represents the encoded operation as a data command</returns>
        ISetDataOperation InterlockedSet(long value);
    }
}