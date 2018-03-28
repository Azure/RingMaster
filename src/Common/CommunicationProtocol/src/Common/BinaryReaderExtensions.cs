// <copyright file="BinaryReaderExtensions.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol
{
    using System;
    using System.IO;

    /// <summary>
    /// Extensions to the <see cref="BinaryReader"/> class.
    /// </summary>
    public static class BinaryReaderExtensions
    {
        /// <summary>
        /// Read a string that could be null from a <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="binaryReader">Binary reader</param>
        /// <returns>String that was read</returns>
        public static string ReadNullableString(this BinaryReader binaryReader)
        {
            if (binaryReader != null)
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
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Read a <see cref="Guid"/> from a <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="binaryReader">Binary reader</param>
        /// <returns><see cref="Guid"/> that was read</returns>
        public static Guid ReadGuid(this BinaryReader binaryReader)
        {
            if (binaryReader != null)
            {
                byte[] bytes = binaryReader.ReadBytes(16);
                return new Guid(bytes);
            }

            return Guid.Empty;
        }

        /// <summary>
        /// Read a <see cref="ushort"/> from a <see cref="BinaryReader"/>.
        /// Read from a Big Endian (Network byte order) UInt16
        /// </summary>
        /// <param name="binaryReader">The reader</param>
        /// <returns>UInt16</returns>
        public static ushort ReadUInt16BE(this BinaryReader binaryReader)
        {
            if (binaryReader == null)
            {
                throw new ArgumentNullException("binaryReader");
            }

            return BitConverter.ToUInt16(binaryReader.ReadBytes(sizeof(ushort)).Reverse(), 0);
        }

        /// <summary>
        /// Read a <see cref="short"/> from a <see cref="BinaryReader"/>.
        /// Read from a Big Endian (Network byte order) Int16
        /// </summary>
        /// <param name="binaryReader">The reader</param>
        /// <returns>Int16</returns>
        public static short ReadInt16BE(this BinaryReader binaryReader)
        {
            if (binaryReader == null)
            {
                throw new ArgumentNullException("binaryReader");
            }

            return BitConverter.ToInt16(binaryReader.ReadBytes(sizeof(short)).Reverse(), 0);
        }

        /// <summary>
        /// Read a <see cref="uint"/> from a <see cref="BinaryReader"/>.
        /// Read from a Big Endian (Network byte order) UInt32
        /// </summary>
        /// <param name="binaryReader">The reader</param>
        /// <returns>UInt32</returns>
        public static uint ReadUInt32BE(this BinaryReader binaryReader)
        {
            if (binaryReader == null)
            {
                throw new ArgumentNullException("binaryReader");
            }

            return BitConverter.ToUInt32(binaryReader.ReadBytes(sizeof(uint)).Reverse(), 0);
        }

        /// <summary>
        /// Read a <see cref="int"/> from a <see cref="BinaryReader"/>.
        /// Read from a Big Endian (Network byte order) Int32
        /// </summary>
        /// <param name="binaryReader">The reader</param>
        /// <returns>Int32</returns>
        public static int ReadInt32BE(this BinaryReader binaryReader)
        {
            if (binaryReader == null)
            {
                throw new ArgumentNullException("binaryReader");
            }

            return BitConverter.ToInt32(binaryReader.ReadBytes(sizeof(int)).Reverse(), 0);
        }

        /// <summary>
        /// Read a <see cref="ulong"/> from a <see cref="BinaryReader"/>.
        /// Read from a Big Endian (Network byte order) UInt64
        /// </summary>
        /// <param name="binaryReader">The reader</param>
        /// <returns>UInt64</returns>
        public static ulong ReadUInt64BE(this BinaryReader binaryReader)
        {
            if (binaryReader == null)
            {
                throw new ArgumentNullException("binaryReader");
            }

            return BitConverter.ToUInt64(binaryReader.ReadBytes(sizeof(ulong)).Reverse(), 0);
        }

        /// <summary>
        /// Read a <see cref="long"/> from a <see cref="BinaryReader"/>.
        /// Read from a Big Endian (Network byte order) Int64
        /// </summary>
        /// <param name="binaryReader">The reader</param>
        /// <returns>Int64</returns>
        public static long ReadInt64BE(this BinaryReader binaryReader)
        {
            if (binaryReader == null)
            {
                throw new ArgumentNullException("binaryReader");
            }

            return BitConverter.ToInt64(binaryReader.ReadBytes(sizeof(long)).Reverse(), 0);
        }

        /// <summary>
        /// Read a <see cref="double"/> from a <see cref="BinaryReader"/>.
        /// Read from a Big Endian (Network byte order) double
        /// </summary>
        /// <param name="binaryReader">The reader</param>
        /// <returns>double</returns>
        public static double ReadDoubleBE(this BinaryReader binaryReader)
        {
            if (binaryReader == null)
            {
                throw new ArgumentNullException("binaryReader");
            }

            return BitConverter.ToDouble(binaryReader.ReadBytes(sizeof(double)).Reverse(), 0);
        }

        /// <summary>
        /// Read a <see cref="string"/> from a <see cref="BinaryReader"/>.
        /// Read a string whose length is specified as a bigEndian uint32
        /// </summary>
        /// <param name="binaryReader">The Reader</param>
        /// <returns>the String</returns>
        public static string ReadString32BitPrefixLengthBE(this BinaryReader binaryReader)
        {
            if (binaryReader == null)
            {
                throw new ArgumentNullException("binaryReader");
            }

            int length = binaryReader.ReadInt32BE();
            if (length == -1)
            {
                return string.Empty;
            }

            return System.Text.UTF8Encoding.ASCII.GetString(binaryReader.ReadBytes(length));
        }

        /// <summary>
        /// Read a <see cref="string"/> from a <see cref="BinaryReader"/>.
        /// Read a string whose length is specified as a bigEndian uint32
        /// </summary>
        /// <param name="binaryReader">The Reader</param>
        /// <returns>the String</returns>
        public static byte[] ReadByteArray32BitPrefixLengthBE(this BinaryReader binaryReader)
        {
            if (binaryReader == null)
            {
                throw new ArgumentNullException("binaryReader");
            }

            int length = binaryReader.ReadInt32BE();
            if ((length == -1) || (length == 0))
            {
                return new byte[0];
            }

            return binaryReader.ReadBytes(length);
        }
    }
}