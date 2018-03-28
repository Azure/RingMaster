// <copyright file="SmartWriterBufferedStream.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System;
    using System.IO;
    using System.Threading;

    /// <summary>
    /// Class SmartWriterBufferedStream is a stream representing a mapped file, where we can change the size and do low-cost seek operations.
    /// </summary>
    /// <seealso cref="Stream" />
    public class SmartWriterBufferedStream : Stream
    {
        /// <summary>
        /// counter to provide names to the mapping
        /// </summary>
        private static int mapNameCounter = 0;

        /// <summary>
        /// if true, a call to Flush() will have no effect.
        /// </summary>
        private readonly bool makeFlushNull;

        /// <summary>
        /// the amount of bytes to increase the underlying mapfile size when we need to grow the file.
        /// </summary>
        private readonly long fileSizeJumps;

        /// <summary>
        /// The memmappedfile object backing the mapping
        /// </summary>
        private System.IO.MemoryMappedFiles.MemoryMappedFile file;

        /// <summary>
        /// The stream to use
        /// </summary>
        private System.IO.MemoryMappedFiles.MemoryMappedViewStream stream;

        /// <summary>
        /// The filestream backing the file itself
        /// </summary>
        private FileStream underlyingFileStream;

        /// <summary>
        /// the actual length of the stream (i.e. exposed to the consumer of this instance.)
        /// </summary>
        private long length;

        private int refCount = 0;
        private int closed = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartWriterBufferedStream"/> class.
        /// </summary>
        /// <param name="fileStream">stream backing the mappingt</param>
        /// <param name="makeFlushNull">if set to true (default) calls to Flush will have no effect, and only calls to RealFlush() will push the pages to the disk</param>
        /// <param name="fileSizeJumps">optional, the amount of bytes to grow the underlying file each time we need to change its size. It must be a 4k multiples, or 0 for default (1GB)</param>
        /// <exception cref="ArgumentException">fileSizeJumps must be a non-negative number, multiple of 4k, or 0 for default.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification ="ComputeStream needs to set the position")]
        public SmartWriterBufferedStream(FileStream fileStream, bool makeFlushNull = true, long fileSizeJumps = 0)
        {
            if (fileStream == null)
            {
                throw new ArgumentNullException(nameof(fileStream));
            }

            if (fileSizeJumps == 0)
            {
                fileSizeJumps = 1L * 1024L * 1024L * 1024L; // 1G
            }

            if ((this.fileSizeJumps % (4 * 1024)) != 0)
            {
                throw new ArgumentException("fileSizeJumps must be a non-negative number, multiple of 4k, or 0 for default.");
            }

            this.fileSizeJumps = fileSizeJumps;
            this.makeFlushNull = makeFlushNull;
            this.underlyingFileStream = fileStream;
            this.underlyingFileStream.Position = 0;
            this.underlyingFileStream.SetLength(0);

            this.ComputeStream(1);
        }

        /// <summary>
        /// Gets the number of references
        /// </summary>
        public int NumRefs => this.refCount;

        /// <summary>
        /// Gets a value indicating whether we can read from the stream
        /// </summary>
        public override bool CanRead
        {
            get
            {
                if (this.stream == null)
                {
                    return false;
                }

                return this.stream.CanRead;
            }
        }

        /// <summary>
        /// Gets a value indicating whether we can seek on the stream
        /// </summary>
        public override bool CanSeek
        {
            get
            {
                if (this.stream == null)
                {
                    return false;
                }

                return this.stream.CanSeek;
            }
        }

        /// <summary>
        /// Gets a value indicating whether we can write on the stream.
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                if (this.stream == null)
                {
                    return false;
                }

                return this.stream.CanWrite;
            }
        }

        /// <summary>
        /// Gets the value indicating if we can the Length of the stream.
        /// The mapped file will be longer than this, but this length is the actual length exposed to the consumer of this instance.
        /// </summary>
        public override long Length => this.length;

        /// <summary>
        /// Gets or sets the value indicating the position in the file
        /// </summary>
        public override long Position
        {
            get
            {
                return this.stream.Position;
            }

            set
            {
                this.Seek(value, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// flushes the data into the disk.
        /// This is a no-op in this class, and RealFlush must be used instead
        /// </summary>
        public override void Flush()
        {
            if (!this.makeFlushNull)
            {
                this.RealFlush();
            }
        }

        /// <summary>
        /// Flushes the data into the disk.
        /// </summary>
        public void RealFlush()
        {
            this.stream.Flush();
            this.underlyingFileStream.Flush();
        }

        /// <summary>
        /// Increments the reference count
        /// </summary>
        public void AddRef()
        {
            Interlocked.Increment(ref this.refCount);
        }

        /// <summary>
        /// Closes this instance, freeing up all resources.
        /// </summary>
        public override void Close()
        {
            if (Interlocked.Increment(ref this.closed) == 1)
            {
                if (Interlocked.Decrement(ref this.refCount) == 0)
                {
                    this.stream.Flush();
                    this.stream.Close();
                    this.stream = null;
                    this.file.Dispose();
                    this.file = null;

                    this.underlyingFileStream.SetLength(this.length);
                    this.underlyingFileStream.Flush();
                    this.underlyingFileStream.Close();
                    this.underlyingFileStream.Dispose();
                    this.underlyingFileStream = null;

                    base.Close();
                }
            }
        }

        /// <summary>
        /// Reads the data from the file into a buffer
        /// </summary>
        /// <param name="buffer">destination buffer</param>
        /// <param name="offset">offset to write into</param>
        /// <param name="count">number of bytes to write</param>
        /// <returns>number of writen bytes</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.stream.Read(buffer, offset, count);
        }

        /// <summary>
        /// moves the position of the cursor in the file.
        /// If offset is negative, the new position is required to precede the position specified by origin by the number of bytes specified by offset.
        /// If offset is zero (0), the new position is required to be the position specified by origin.
        /// If offset is positive, the new position is required to follow the position specified by origin by the number of bytes specified by offset.
        /// Seeking to any location beyond the length of the stream is supported.
        /// </summary>
        /// <param name="offset">offset to move</param>
        /// <param name="origin">reference of the move</param>
        /// <returns>the final position regarding the beginning of the file</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Current:
                {
                    offset += this.stream.Position;
                    break;
                }

                case SeekOrigin.End:
                {
                    offset = this.length - offset;
                    break;
                }
            }

            if (offset == this.stream.Position)
            {
                return offset;
            }

            if (offset < 0)
            {
                throw new NotSupportedException("offset cannot be prepending the beginning of the file");
            }

            if (offset > this.length)
            {
                this.SetLength(offset);
            }

            return this.stream.Seek(offset, SeekOrigin.Begin);
        }

        /// <summary>
        /// sets the length of the file (as perceived by the class consumer).
        /// It might require re-dimension the mapping object, and hence, the backing filestream.
        /// </summary>
        /// <param name="value">the new length</param>
        public override void SetLength(long value)
        {
            this.length = value;

            long fileLength = this.underlyingFileStream.Length;
            int k = 0;

            // if we need to grow the file, compute how much.
            while (this.length > fileLength)
            {
                k++;
                fileLength += this.fileSizeJumps;
            }

            // if the above didn't cause any efect it may be because we need to srink.
            if (k == 0)
            {
                // if we need to srink the file, do it.
                while (this.length < fileLength - this.fileSizeJumps)
                {
                    k--;
                    fileLength -= this.fileSizeJumps;
                }
            }

            if (k != 0)
            {
                this.ComputeStream(k);
            }
        }

        /// <summary>
        /// Writes a buffer into the stream.
        /// It might require re-dimensioning the underlying file.
        /// </summary>
        /// <param name="buffer">the buffer to be writen</param>
        /// <param name="offset">the offset within the file</param>
        /// <param name="count">the number of bytes to write</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            long newpos = this.stream.Position + count;
            if (newpos >= this.underlyingFileStream.Length)
            {
                long fileLen = this.underlyingFileStream.Length;

                int k = 0;
                while (newpos >= fileLen)
                {
                    k++;
                    fileLen += this.fileSizeJumps;
                }

                this.ComputeStream(k);
            }

            this.stream.Write(buffer, offset, count);

            if (newpos > this.length)
            {
                this.length = newpos;
            }
        }

        /// <summary>
        /// Resizes the underlying stream, closing the map object and re-opening it with the new size.
        /// It can grow or srink the file, depending on the factor.
        /// </summary>
        /// <param name="factor">the new size will change by factor*FileSizeJumps, so factor=1 --> make the file FileSizeJump larger; factor=-2 --> make the file 2*FileSizeJump shorter.</param>
        private void ComputeStream(int factor)
        {
            long pos = 0;

            if (this.file != null)
            {
                pos = this.stream.Position;

                this.stream.Flush();
                this.stream.Close();
                this.stream.Dispose();
                this.file.Dispose();
                this.file = null;
                this.stream = null;
                this.underlyingFileStream.Flush();
            }

            this.underlyingFileStream.SetLength(Math.Max(0, this.underlyingFileStream.Length + (this.fileSizeJumps * factor)));

            int mapcount = Interlocked.Increment(ref mapNameCounter);

            string mapName = $"map-{Path.GetFileName(this.underlyingFileStream.Name)}-{mapcount}";

            try
            {
                this.file = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateFromFile(
                    this.underlyingFileStream,
                    mapName,
                    this.underlyingFileStream.Length,
                    System.IO.MemoryMappedFiles.MemoryMappedFileAccess.ReadWrite,
                    null,
                    HandleInheritability.None,
                    true);

                this.stream = this.file.CreateViewStream();
                this.stream.Position = pos;
            }
            catch (Exception)
            {
                this.stream?.Dispose();
                this.file?.Dispose();
                throw;
            }
        }
    }
}