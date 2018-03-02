// <copyright file="ISetDataOperation.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Data
{
    /// <summary>
    /// A SetData operation that represents a data command.
    /// </summary>
    public interface ISetDataOperation
    {
        /// <summary>
        /// Gets the data associated with the command.
        /// </summary>
        byte[] RawData { get; }
    }
}