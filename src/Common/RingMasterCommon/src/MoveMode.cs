// <copyright file="MoveMode.cs" company="Microsoft">
//     Copyright ©  2016
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System;

    /// <summary>
    /// Enumeration MoveMode describes different modes for the move operation.
    /// </summary>
    [Flags]
    public enum MoveMode : ushort
    {
        /// <summary>
        /// No flag
        /// </summary>
        None = 0,

        /// <summary>
        /// If set, this flag allows the move operation to also create intermediate tree nodes for the destination path if not present already
        /// </summary>
        AllowPathCreationFlag = 0x1000,

        /// <summary>
        /// If set, this flag allows the move operation ONLY IF the source node has no children
        /// </summary>
        OnlyIfSourcePathIsEmpty = 0x2000,
    }
}