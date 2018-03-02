// <copyright file="BinaryReaderExtensions.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol
{
    using System;
    using System.IO;

    /// <summary>
    /// Extensions to the <see cref="BinaryReader"/> class.
    /// </summary>
    internal static class BinaryReaderExtensions
    {
        /// <summary>
        /// Read a string that could be null from a <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="binaryReader">Binary reader</param>
        /// <returns>String that was read</returns>
        public static string ReadNullableString(this BinaryReader binaryReader)
        {
            bool isNull = binaryReader.ReadBoolean();
            if (isNull)
            {
                return null;
            }
            else
            {
                return binaryReader.ReadString();
            }
        }

        /// <summary>
        /// Read a <see cref="Guid"/> from a <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="binaryReader">Binary reader</param>
        /// <returns><see cref="Guid"/> that was read</returns>
        public static Guid ReadGuid(this BinaryReader binaryReader)
        {
            byte[] bytes = binaryReader.ReadBytes(16);
            return new Guid(bytes);
        }
    }
}