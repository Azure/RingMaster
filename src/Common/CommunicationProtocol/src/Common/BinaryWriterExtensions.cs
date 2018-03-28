// <copyright file="BinaryWriterExtensions.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol
{
    using System;
    using System.IO;

    /// <summary>
    /// Extensions to the <see cref="BinaryWriter"/> class.
    /// </summary>
    public static class BinaryWriterExtensions
    {
        /// <summary>
        /// Write a string that could be null to the <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="binaryWriter">Binary writer</param>
        /// <param name="stringValue">String to write to the binary writer.</param>
        public static void WriteNullableString(this BinaryWriter binaryWriter, string stringValue)
        {
            if (binaryWriter != null)
            {
                binaryWriter.Write((bool)(stringValue == null));
                if (stringValue != null)
                {
                    binaryWriter.Write(stringValue);
                }
            }
            else
            {
                throw new ArgumentNullException("binaryWriter");
            }
        }

        /// <summary>
        /// Write a <see cref="Guid"/> to a <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="binaryWriter">Binary writer</param>
        /// <param name="guid"><see cref="Guid"/> to write</param>
        public static void Write(this BinaryWriter binaryWriter, Guid guid)
        {
            if (binaryWriter != null)
            {
                byte[] bytes = guid.ToByteArray();
                binaryWriter.Write(bytes, 0, bytes.Length);
            }
            else
            {
                throw new ArgumentNullException("binaryWriter");
            }
        }

        /// <summary>
        /// Writes a short integer to a <see cref="BinaryWriter"/> object in big Endianness
        /// </summary>
        /// <param name="binaryWriter">binary writer object</param>
        /// <param name="thedata">Short integer to be written</param>
        public static void WriteBE(this BinaryWriter binaryWriter, short thedata)
        {
            if (binaryWriter == null)
            {
                throw new ArgumentNullException("binaryWriter");
            }

            byte[] bArr = BitConverter.GetBytes(thedata).Reverse();
            binaryWriter.Write(bArr, 0, bArr.Length);
        }

        /// <summary>
        /// Writes an unsigned short integer to a <see cref="BinaryWriter"/> object in big Endianness
        /// </summary>
        /// <param name="binaryWriter">binary writer object</param>
        /// <param name="thedata">Unsigned short integer to be written</param>
        public static void WriteBE(this BinaryWriter binaryWriter, ushort thedata)
        {
            if (binaryWriter == null)
            {
                throw new ArgumentNullException("binaryWriter");
            }

            byte[] bArr = BitConverter.GetBytes(thedata).Reverse();
            binaryWriter.Write(bArr, 0, bArr.Length);
        }

        /// <summary>
        /// Writes an integer to a <see cref="BinaryWriter"/> object in big Endianness
        /// </summary>
        /// <param name="binaryWriter">binary writer object</param>
        /// <param name="thedata">integer to be written</param>
        public static void WriteBE(this BinaryWriter binaryWriter, int thedata)
        {
            if (binaryWriter == null)
            {
                throw new ArgumentNullException("binaryWriter");
            }

            byte[] bArr = BitConverter.GetBytes(thedata).Reverse();
            binaryWriter.Write(bArr, 0, bArr.Length);
        }

        /// <summary>
        /// Writes a unsigned integer to a <see cref="BinaryWriter"/> object in big Endianness
        /// </summary>
        /// <param name="binaryWriter">binary writer object</param>
        /// <param name="thedata">Unsigned integer to be written</param>
        public static void WriteBE(this BinaryWriter binaryWriter, uint thedata)
        {
            if (binaryWriter == null)
            {
                throw new ArgumentNullException("binaryWriter");
            }

            byte[] bArr = BitConverter.GetBytes(thedata).Reverse();
            binaryWriter.Write(bArr, 0, bArr.Length);
        }

        /// <summary>
        /// Writes a long integer to a <see cref="BinaryWriter"/> object in big Endianness
        /// </summary>
        /// <param name="binaryWriter">binary writer object</param>
        /// <param name="thedata">long integer to be written</param>
        public static void WriteBE(this BinaryWriter binaryWriter, long thedata)
        {
            if (binaryWriter == null)
            {
                throw new ArgumentNullException("binaryWriter");
            }

            byte[] bArr = BitConverter.GetBytes(thedata).Reverse();
            binaryWriter.Write(bArr, 0, bArr.Length);
        }

        /// <summary>
        /// Writes a unsigned long integer to a <see cref="BinaryWriter"/> object in big Endianness
        /// </summary>
        /// <param name="binaryWriter">binary writer object</param>
        /// <param name="thedata">unsigned long integer to be written</param>
        public static void WriteBE(this BinaryWriter binaryWriter, ulong thedata)
        {
            if (binaryWriter == null)
            {
                throw new ArgumentNullException("binaryWriter");
            }

            byte[] bArr = BitConverter.GetBytes(thedata).Reverse();
            binaryWriter.Write(bArr, 0, bArr.Length);
        }

        /// <summary>
        /// Writes a byte array to a <see cref="BinaryWriter"/> object in big Endianness
        /// </summary>
        /// <param name="binaryWriter">binary writer object</param>
        /// <param name="dataArray">byte array to be written</param>
        public static void WriteBE(this BinaryWriter binaryWriter, byte[] dataArray)
        {
            if (binaryWriter == null)
            {
                throw new ArgumentNullException("binaryWriter");
            }

            if (dataArray == null)
            {
                throw new ArgumentNullException("dataArray");
            }

            binaryWriter.WriteBE(dataArray.Length);
            binaryWriter.Write(dataArray);
        }

        /// <summary>
        /// Write a <see cref="string"/> from a <see cref="BinaryWriter"/>.
        /// Write a string whose length is specified as a bigEndian uint32
        /// </summary>
        /// <param name="binaryWriter">The Reader</param>
        /// <param name="s">the string</param>
        public static void WriteString32BitPrefixLengthBE(this BinaryWriter binaryWriter, string s)
        {
            if (binaryWriter == null)
            {
                throw new ArgumentNullException("binaryWriter");
            }

            if (string.IsNullOrEmpty(s))
            {
                binaryWriter.WriteBE((int)-1);
            }
            else
            {
                binaryWriter.WriteBE(s.Length);
                binaryWriter.Write(System.Text.UTF8Encoding.ASCII.GetBytes(s));
            }

            return;
        }

        /// <summary>
        /// Write a byte array to a <see cref="BinaryWriter"/> object
        /// </summary>
        /// <param name="binaryWriter">binary writter object</param>
        /// <param name="dataBuffer">byte array to be written</param>
        public static void WriteByteArray32BitPrefixLengthBE(this BinaryWriter binaryWriter, byte[] dataBuffer)
        {
            if (binaryWriter == null)
            {
                throw new ArgumentNullException("binaryWriter");
            }

            if (dataBuffer == null)
            {
                binaryWriter.WriteBE((int)-1);
            }
            else
            {
                binaryWriter.WriteBE(dataBuffer.Length);
                binaryWriter.Write(dataBuffer);
            }

            return;
        }
    }
}
