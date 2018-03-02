// <copyright file="CachedStream.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;

    /// <summary>
    /// Class CachedStream. This stream abstracts the multi-level caching of a remote stream (typically XStore-backed stream).
    /// It uses a multi-level cache approach. First cache is in-memory (LRU cache), second level is in the local filesystem.
    /// The CachedStream is cloneable, and all clones will share both levels of the cache.
    /// </summary>
    public class CachedStream : Stream, ICloneableStream
    {
        /// <summary>
        /// The mapstream used for filesystem cache.
        /// </summary>
        private SmartWriterBufferedStream mapstream;

        /// <summary>
        /// The remote read stream
        /// </summary>
        private Stream readStream;

        /// <summary>
        /// The hash describing what blocks do we already have in the local filesystem cache
        /// </summary>
        private HashSet<long> blocksInLocalFs;

        /// <summary>
        /// The blocks cached in memory
        /// </summary>
        private LruCache<long, Block> blocksInMemory;

        /// <summary>
        /// The asynchronous queue to write blocks lazily in filesystem
        /// </summary>
        private ExecutionQueue asyncWriteInFsQueue;

        /// <summary>
        /// a function we can use to generate a clone to the base readStream.
        /// </summary>
        private readonly Func<Stream> getStream;

        /// <summary>
        /// The size of blocks to read from the base readStream
        /// </summary>
        private readonly int chunkSize;

        /// <summary>
        /// Current position in the stream as exposed to the class users
        /// </summary>
        private long position;

        /// <summary>
        /// thrird level cache, where we remember what is the last block accessed, and the last position queried by Read
        /// </summary>
        private long lastPos;

        /// <summary>
        /// thrird level cache, where we remember what is the last block accessed, and the last block required by Read
        /// </summary>
        private Block lastBlock;

        /// <summary>
        /// Gets the size of the chunk.
        /// </summary>
        /// <value>The size of the chunk.</value>
        public int ChunkSize => this.chunkSize;

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports reading.
        /// </summary>
        /// <value><c>true</c> if this instance can read; otherwise, <c>false</c>.</value>
        public override bool CanRead => true;

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports seeking.
        /// </summary>
        /// <value><c>true</c> if this instance can seek; otherwise, <c>false</c>.</value>
        public override bool CanSeek => true;

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports writing.
        /// </summary>
        /// <value><c>true</c> if this instance can write; otherwise, <c>false</c>.</value>
        public override bool CanWrite => false;

        /// <summary>
        /// When overridden in a derived class, gets the length in bytes of the stream.
        /// </summary>
        /// <value>The length.</value>
        public override long Length => this.readStream.Length;

        /// <summary>
        /// When overridden in a derived class, gets or sets the position within the current stream.
        /// </summary>
        /// <value>The position.</value>
        public override long Position
        {
            get
            {
                return this.position;
            }

            set
            {
                this.Seek(value, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedStream"/> class.
        /// This is a constructor for cloning a cachedStream
        /// </summary>
        /// <param name="originalStream">The original cached stream.</param>
        private CachedStream(CachedStream originalStream)
        {
            this.blocksInLocalFs = originalStream.blocksInLocalFs;
            this.blocksInMemory = originalStream.blocksInMemory;

            originalStream.mapstream?.AddRef();

            this.mapstream = originalStream.mapstream;

            this.getStream = originalStream.getStream;
            this.readStream = this.getStream();

            if (this.readStream == null)
            {
                throw new ArgumentException("getStream cannot return null");
            }

            this.chunkSize = originalStream.chunkSize;
            this.asyncWriteInFsQueue = originalStream.asyncWriteInFsQueue;
            this.position = 0;
            this.lastPos = -1;
            this.lastBlock = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedStream"/> class.
        /// </summary>
        /// <param name="getStream">The function to be used to get a copy of the base read stream.</param>
        /// <param name="tempFile">The temporary file for the local filesystem cache. If null, no local filesystem cache is used.</param>
        /// <param name="chunkSize">Size of the chunks to read at once.</param>
        /// <param name="maxLrUitems">maximum number of in-memory LRU block entries</param>
        /// <exception cref="System.ArgumentNullException">
        /// getStream
        /// </exception>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "LRU cache is stored in field")]
        public CachedStream(Func<Stream> getStream, string tempFile, int chunkSize, int maxLrUitems)
        {
            if (getStream == null)
            {
                throw new ArgumentNullException(nameof(getStream));
            }

            if (maxLrUitems < 0)
            {
                throw new ArgumentException("maxLRUItems must be >=0");
            }

            if (chunkSize <= 0)
            {
                throw new ArgumentException("chunkSize must be >0");
            }

            this.position = 0;
            this.chunkSize = chunkSize;
            this.lastPos = -1;
            this.lastBlock = null;

            this.blocksInMemory = maxLrUitems > 0 ? new LruCache<long, Block>(maxLrUitems) : null;

            FileStream f = null;

            try
            {
                if (tempFile != null)
                {
                    f = new FileStream(tempFile, FileMode.Create, FileAccess.ReadWrite, FileShare.Read | FileShare.Write, chunkSize, FileOptions.DeleteOnClose | FileOptions.RandomAccess);
                    this.mapstream = new SmartWriterBufferedStream(f);
                    f = null;

                    this.blocksInLocalFs = new HashSet<long>();
                    this.asyncWriteInFsQueue = new ExecutionQueue(1);
                }
                else
                {
                    this.mapstream = null;
                    this.blocksInLocalFs = null;
                    this.asyncWriteInFsQueue = null;
                }

                this.getStream = getStream;
                this.readStream = this.getStream();
                if (this.readStream == null)
                {
                    throw new ArgumentException("getStream cannot return null");
                }
            }
            catch (Exception)
            {
                f?.Dispose();
                this.mapstream?.Dispose();
                this.readStream?.Dispose();
                throw;
            }
        }

        /// <summary>
        /// When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush()
        {
        }

        /// <summary>
        /// Clones the stream. The local filesystem cache file and the in-memory cache file are both shared by all clones.
        /// </summary>
        /// <returns>cloned Stream</returns>
        public Stream CloneStream()
        {
            return new CachedStream(this);
        }

        /// <summary>
        /// Gets the number clones existing in memory
        /// </summary>
        /// <value>The number clones.</value>
        public int NumClonesSharingFileSystemCache
        {
            get
            {
                if (this.mapstream == null)
                {
                    return 0;
                }

                return this.mapstream.NumRefs + 1;
            }
        }

        /// <summary>
        /// When overridden in a derived class, sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the <paramref name="origin" /> parameter.</param>
        /// <param name="origin">A value of type <see cref="T:System.IO.SeekOrigin" /> indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Current)
            {
                offset += this.Position;
            }
            else if (origin == SeekOrigin.End)
            {
                offset = this.Length - offset;
            }

            if (offset > this.Length)
            {
                // since the base stream is readonly, we will not move the cursor past the end of the file
                offset = this.Length;
            }

            if (offset < 0)
            {
                offset = 0;
            }

            this.position = offset;

            return this.position;
        }

        /// <summary>
        /// Reads the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <returns>System.Int32.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            long pos = this.position;

            // we don't read more than a chunk at a time
            if (count > this.chunkSize)
            {
                count = this.chunkSize;
            }

            int mod = (int)(pos % this.chunkSize);
            Block block;

            if (this.lastPos == pos - mod)
            {
                block = this.lastBlock;
            }
            else
            {
                block = this.GetBlock(pos - mod);
                this.lastBlock = block;
                this.lastPos = pos - mod;
            }

            int read = block.Read(mod, buffer, offset, count);
            this.position = pos + read;

            return read;
        }

        /// <summary>
        /// Flushes the file system cache, blocking until all pending writes are completed
        /// </summary>
        public void FlushFileSystemCache()
        {
            this.asyncWriteInFsQueue?.Drain(ExecutionQueue.DrainMode.AllowEnqueuesAfterDrainPoint);
        }

        /// <summary>
        /// Gets the block addequate for the given stream position.
        /// If needed, it reads the block from the base read stream.
        /// If not in memory cache, it inserts it there.
        /// If not in the local filesystem cache, it inserts it there lazily.
        /// </summary>
        /// <param name="pos">The required position.</param>
        /// <returns>the Block containing that position.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2002:DoNotLockOnObjectsWithWeakIdentity", Justification = "Unnecessary")]
        private Block GetBlock(long pos)
        {
            Block block;

            // if the block is in memory, we are lucky.
            if (this.blocksInMemory != null)
            {
                lock (this.blocksInMemory)
                {
                    if (this.blocksInMemory.TryGetValue(pos, out block))
                    {
                        return block;
                    }
                }
            }

            // if we don't have local filesystem cache, then we skip the check on the hashset
            if (this.mapstream != null)
            {
                bool isInLocalFs = false;

                // otherwise, the block may be in the local filesystem
                lock (this.blocksInLocalFs)
                {
                    isInLocalFs = this.blocksInLocalFs.Contains(pos);
                }

                // note this algorithm may get a block twice if two threads ask for the same block at the same time,
                // using two different clones of the same CachedStream (which share mapstream, blocksInLocalFS and blocksInMemory objects).
                // for simplicity, we are fine with those cases.
                if (isInLocalFs)
                {
                    lock (this.mapstream)
                    {
                        block = new Block(this.mapstream, pos, this.chunkSize);
                    }

                    // don't forget we have the block in memory
                    if (this.blocksInMemory != null)
                    {
                        lock (this.blocksInMemory)
                        {
                            this.blocksInMemory.TryAdd(pos, ref block);
                        }
                    }

                    return block;
                }
            }

            // however, if we are really unlucky, we need to go to the readStream to fetch the block
            lock (this.readStream)
            {
                block = new Block(this.readStream, pos, this.chunkSize);
            }

            // don't forget we have the block in memory
            if (this.blocksInMemory != null)
            {
                lock (this.blocksInMemory)
                {
                    this.blocksInMemory.TryAdd(pos, ref block);
                }
            }

            // and now write asynchronously in the local filesystem the block
            this.asyncWriteInFsQueue?.Enqueue(this.AsyncCopyInFs, pos, block);

            return block;
        }

        /// <summary>
        /// Asynchronously the copy in fs.
        /// This method assumes that (this.mapstream != null && this.asyncWriteInFSQueue != null)
        /// </summary>
        /// <param name="pos">The position.</param>
        /// <param name="block">The block.</param>
        [SuppressMessage("Microsoft.Reliability", "CA2002:DoNotLockOnObjectsWithWeakIdentity", Justification = "Unnecessary")]
        private void AsyncCopyInFs(long pos, Block block)
        {
            // so, we have a mapstream, and also a blocksInLocalFS hashset
            lock (this.mapstream)
            {
                this.mapstream.Position = pos;
                block.Write(this.mapstream);
            }

            // and remember we have it in the local FS, of course
            lock (this.blocksInLocalFs)
            {
                this.blocksInLocalFs.Add(pos);
            }
        }

        /// <summary>
        /// When overridden in a derived class, sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        /// <exception cref="System.NotSupportedException"></exception>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Writes the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <exception cref="System.NotSupportedException"></exception>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Closes the current stream and releases any resources (such as sockets and file handles) associated with the current stream. Instead of calling this method, ensure that the stream is properly disposed.
        /// </summary>
        public override void Close()
        {
            if (this.mapstream != null)
            {
                if (this.mapstream.NumRefs == 1)
                {
                    // we are the last ones, so we should cancel all async writes
                    this.asyncWriteInFsQueue.Drain(ExecutionQueue.DrainMode.DisallowAllFurtherEnqueuesAndRemoveAllElements);
                }

                this.mapstream.Close();
            }

            this.readStream?.Close();

            this.readStream = null;
            this.mapstream = null;
            this.blocksInLocalFs = null;
            this.asyncWriteInFsQueue = null;
            this.blocksInMemory = null;
            this.asyncWriteInFsQueue = null;

            base.Close();
        }

        /// <summary>
        /// Class Block abstracts a block of data read from the base read Stream, or cached into the local filesystem file, or in memory.
        /// </summary>
        internal class Block
        {
            /// <summary>
            /// The chunk of data itself.
            /// </summary>
            private readonly byte[] chunk;

            /// <summary>
            /// Initializes a new instance of the <see cref="Block"/> class.
            /// </summary>
            /// <param name="stream">The stream to read the data from.</param>
            /// <param name="position">The position in the read stream to read from.</param>
            /// <param name="chunkSize">Size of the chunk to read.</param>
            /// <exception cref="System.IO.IOException">cannot read from stream</exception>
            internal Block(Stream stream, long position, int chunkSize)
            {
                if (chunkSize > stream.Length - position)
                {
                    chunkSize = (int)(stream.Length - position);
                }

                this.chunk = new byte[chunkSize];
                stream.Position = position;

                int p = 0;
                while (p != chunkSize)
                {
                    int r = stream.Read(this.chunk, p, chunkSize - p);
                    if (r == 0)
                    {
                        throw new IOException("cannot read from stream");
                    }

                    p += r;
                }
            }

            /// <summary>
            /// Reads a number of bytes from this block, given an offset, and copying the bytes into a given buffer.
            /// </summary>
            /// <param name="chunkoffset">The offset in this block to start copying from.</param>
            /// <param name="bytes">The buffer to copy the bytes into.</param>
            /// <param name="offset">The offset in the destination buffer to copy the bytes into.</param>
            /// <param name="count">The count of bytes to copy.</param>
            /// <returns>number of bytes copied</returns>
            public int Read(int chunkoffset, byte[] bytes, int offset, int count)
            {
                if (count > this.chunk.Length - chunkoffset)
                {
                    count = this.chunk.Length - chunkoffset;
                }

                Array.Copy(this.chunk, chunkoffset, bytes, offset, count);

                return count;
            }

            /// <summary>
            /// Writes this block into the specified stream.
            /// </summary>
            /// <param name="stream">The stream to copy into.</param>
            public void Write(Stream stream)
            {
                stream.Write(this.chunk, 0, this.chunk.Length);
            }
        }
    }
}