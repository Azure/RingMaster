// <copyright file="CachedStreamTests.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypesUnitTest
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using HelperTypes;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// This class contains tests for requests
    /// </summary>
    [TestClass]
    public class CachedStreamTests
    {
        /// <summary>
        /// This method tests that constructor guards against bad arguments
        /// </summary>
        [TestMethod]
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Tests")]
        public void TestConstructorArguments()
        {
            TestHelpers.AssertThrows<ArgumentNullException>(() =>
            {
                CachedStream str = new CachedStream(null, Path.GetTempFileName(), 1, 1);
                Assert.IsNotNull(str);
            });

            TestHelpers.AssertThrows<ArgumentException>(() =>
            {
                CachedStream str = new CachedStream(
                    () => null,
                    Path.GetTempFileName(),
                    10,
                    10);
                str.Dispose();
            });

            TestHelpers.AssertThrows<ArgumentException>(() =>
            {
                CachedStream str = new CachedStream(
                    () => new MemoryStream(),
                    Path.GetTempFileName(),
                    -1,
                    1);
                Assert.IsNotNull(str);
            });

            TestHelpers.AssertThrows<ArgumentException>(() =>
            {
                CachedStream str = new CachedStream(
                    () => new MemoryStream(),
                    Path.GetTempFileName(),
                    0,
                    1);
                Assert.IsNotNull(str);
            });

            TestHelpers.AssertThrows<ArgumentException>(() =>
            {
                CachedStream str = new CachedStream(
                    () => new MemoryStream(),
                    Path.GetTempFileName(),
                    10,
                    -1);
                str.Dispose();
            });

            TestHelpers.AssertThrows<ArgumentException>(() =>
            {
                CachedStream str = new CachedStream(
                    () => new MemoryStream(),
                    string.Empty,
                    10,
                    10);
                str.Dispose();
            });

            Stream[] list = { new MemoryStream(), null };
            int count = 0;
            CachedStream aux = new CachedStream(
                () => list[count++],
                null,
                10,
                10);

            TestHelpers.AssertThrows<ArgumentException>(() =>
            {
                aux.CloneStream();
            });
        }

        /// <summary>
        /// This method tests clone works
        /// </summary>
        [TestMethod]
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Tests")]
        public void TestCloneable()
        {
            MemoryStream[] list = { new MemoryStream(), new MemoryStream() };
            int pos = 0;

            CachedStream s = new CachedStream(
                () => list[pos++],
                null,
                1024,
                0);

            byte[] one = { 1, 1, 1, 1 };
            byte[] two = { 2, 2 };

            list[0].Write(one, 0, one.Length);

            Stream s2 = s.CloneStream();

            Assert.IsTrue(s.CanRead);
            Assert.IsFalse(s.CanWrite);
            Assert.IsTrue(s.CanSeek);

            Assert.AreEqual(list[0].Position, one.Length, "write didn't go to the first stream");
            Assert.AreEqual(list[1].Position, 0, "second stream was altered");
            Assert.AreEqual(s2.Position, 0, "cloned stream was altered");

            list[1].Write(two, 0, two.Length);
            Assert.AreEqual(list[0].Position, one.Length, "first stream was altered");
            Assert.AreEqual(list[1].Position, two.Length, "write didn't go to the second stream");

            s.Position = 2;
            s2.Position = 1;

            Assert.AreEqual(list[0].Length, one.Length, "first stream has wrong length");
            Assert.AreEqual(list[1].Length, two.Length, "second stream has wrong length");

            byte[] readOne = new byte[one.Length];
            byte[] readTwo = new byte[two.Length];

            readOne[0] = one[0];
            readOne[1] = one[1];
            readTwo[0] = two[0];

            int read = s.Read(readOne, 2, readOne.Length) + 2;
            Assert.AreEqual(read, one.Length, "not all bytes read from first stream");
            Assert.AreEqual(s.Position, one.Length, "position wrong on first stream");
            Assert.AreEqual(list[0].Position, one.Length, "first stream has not repositioned properly");

            read = s2.Read(readTwo, 1, readTwo.Length) + 1;
            Assert.AreEqual(read, two.Length, "not all bytes read from second stream");
            Assert.AreEqual(s2.Position, two.Length, "position wrong on second stream");
            Assert.AreEqual(list[1].Position, two.Length, "second stream has not repositioned properly");

            TestHelpers.AssertBytesEqual(readOne, one, "buffer read from first stream was wrong");
            TestHelpers.AssertBytesEqual(readTwo, two, "buffer read from second stream was wrong");

            list[0].Position = 0;
            list[1].Position = 0;

            readOne = new byte[one.Length];
            readTwo = new byte[two.Length];

            read = list[0].Read(readOne, 0, readOne.Length);
            Assert.AreEqual(read, one.Length, "not all bytes read from first stream");
            read = list[1].Read(readTwo, 0, readTwo.Length);
            Assert.AreEqual(read, two.Length, "not all bytes read from second stream");

            TestHelpers.AssertBytesEqual(readOne, one, "buffer read from first underlying stream was wrong");
            TestHelpers.AssertBytesEqual(readTwo, two, "buffer read from second underlying stream was wrong");

            // test flush doesn't throw
            s.Flush();

            s.Close();
            TestHelpers.AssertThrows<ObjectDisposedException>(() =>
            {
                list[0].Position = 0;
            });

            s2.Close();
            TestHelpers.AssertThrows<ObjectDisposedException>(() =>
            {
                list[1].Position = 0;
            });
        }

        /// <summary>
        /// This method tests caching works
        /// </summary>
        [TestMethod]
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Tests")]
        public void TestCaching()
        {
            MemoryStream list = new MemoryStream();

            CachedStream s = new CachedStream(
                () => list,
                null,
                1024,
                10);

            this.ValidateCaching(list, s);
        }

        /// <summary>
        /// This method tests caching works
        /// </summary>
        [TestMethod]
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Tests")]
        public void TestCachTwoBlocks()
        {
            MemoryStream list = new MemoryStream();

            CachedStream s = new CachedStream(
                () => list,
                null,
                5, // 5 bytes per block
                10);

            byte[] one = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            Stream s2 = s.CloneStream();

            list.Write(one, 0, one.Length);

            byte[] read = new byte[one.Length];

            int pending = read.Length;

            // read 2 first
            int bytesread = s.Read(read, 0, 2);
            Assert.AreEqual(bytesread, 2);
            pending -= bytesread;

            while (pending > 0)
            {
                bytesread = s.Read(read, one.Length - pending, pending);

                if (bytesread == 0)
                {
                    Assert.Fail("we didn't read anything from s and we have still pending bytes");
                }

                pending -= bytesread;
            }

            TestHelpers.AssertBytesEqual(one, read, "didnt read from s same bytes as written");

            read = new byte[one.Length];
            pending = read.Length;

            while (pending > 0)
            {
                bytesread = s2.Read(read, one.Length - pending, pending);

                if (bytesread == 0)
                {
                    Assert.Fail("we didn't read anything from s2 and we have still pending bytes");
                }

                pending -= bytesread;
            }

            TestHelpers.AssertBytesEqual(one, read, "didnt read from s2 same bytes as written");

            s2.Close();
        }

        /// <summary>
        /// This method tests FileSystem caching works
        /// </summary>
        [TestMethod]
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Tests")]
        public void TestFsCaching()
        {
            MemoryStream list = new MemoryStream();

            CachedStream s = new CachedStream(
                () => list,
                Path.GetTempFileName(),
                5,
                0);

            Assert.AreEqual(s.ChunkSize, 5, "wrong chunksize");

            this.ValidateCaching(list, s, true);
        }

        /// <summary>
        /// Validates the caching.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="s">The stream.</param>
        /// <param name="hasFileSystemCache">if set to <c>true</c> the stream has a file system cache.</param>
        private void ValidateCaching(MemoryStream list, CachedStream s, bool hasFileSystemCache = false)
        {
            Assert.IsFalse(s.CanWrite);
            Assert.IsTrue(s.CanRead);
            Assert.IsTrue(s.CanSeek);

            TestHelpers.AssertThrows<NotSupportedException>(() =>
            {
                s.Write(new byte[1], 0, 1);
            });

            TestHelpers.AssertThrows<NotSupportedException>(() =>
            {
                s.SetLength(100);
            });

            if (hasFileSystemCache)
            {
                Assert.AreEqual(s.NumClonesSharingFileSystemCache, 1, "number of clones is wrong");
            }

            byte[] one = { 1, 1, 1, 1 };
            byte[] zeroes = { 0, 0, 0, 0 };
            Stream s2 = s.CloneStream();

            if (hasFileSystemCache)
            {
                Assert.AreEqual(s.NumClonesSharingFileSystemCache, 2, "number of clones is wrong");
                Assert.AreEqual(((CachedStream)s2).NumClonesSharingFileSystemCache, 2, "number of clones is wrong");
            }
            else
            {
                Assert.AreEqual(s.NumClonesSharingFileSystemCache, 0, "number of clones is wrong");
                Assert.AreEqual(((CachedStream)s2).NumClonesSharingFileSystemCache, 0, "number of clones is wrong");
            }

            list.Write(one, 0, one.Length);

            Assert.AreEqual(list.Position, one.Length, "write didn't go to the first stream");
            Assert.AreEqual(s.Position, 0, "cloned stream was altered");
            Assert.AreEqual(s2.Position, 0, "cloned second stream was altered");

            Assert.AreEqual(s.Length, one.Length, "first stream has wrong length");
            Assert.AreEqual(s2.Length, one.Length, "second stream has wrong length");

            byte[] readOne = new byte[one.Length];

            int read = s.Read(readOne, 0, readOne.Length);
            Assert.AreEqual(read, one.Length, "not all bytes read from first stream");
            Assert.AreEqual(s.Position, one.Length, "position wrong on first stream");
            Assert.AreEqual(list.Position, one.Length, "first stream has not repositioned properly");
            TestHelpers.AssertBytesEqual(one, readOne, "bytes read were not the written ones");

            if (hasFileSystemCache)
            {
                s.FlushFileSystemCache();
            }

            list.Position = 0;
            list.Write(zeroes, 0, zeroes.Length);
            list.Position = 2;

            readOne = new byte[one.Length];
            s.Position = 0;
            read = s.Read(readOne, 0, readOne.Length);
            Assert.AreEqual(read, one.Length, "not all bytes read from first stream");
            Assert.AreEqual(s.Position, one.Length, "position wrong on first stream");
            TestHelpers.AssertBytesEqual(one, readOne, "bytes read were not the written ones");
            Assert.AreEqual(list.Position, 2, "first stream was unexpectedly repositioned");

            readOne = new byte[one.Length];
            s2.Position = 0;
            read = s2.Read(readOne, 0, readOne.Length);
            Assert.AreEqual(read, one.Length, "not all bytes read from first stream");
            Assert.AreEqual(s2.Position, one.Length, "position wrong on first stream");
            TestHelpers.AssertBytesEqual(one, readOne, "bytes read were not the written ones");
            Assert.AreEqual(list.Position, 2, "first stream was unexpectedly repositioned");

            long pos = s2.Seek(0, SeekOrigin.End);
            Assert.AreEqual(pos, s2.Length, "wrong seek from end");
            Assert.AreEqual(pos, s2.Position, "wrong position after seek from end");

            pos = s2.Seek(-2, SeekOrigin.Current);
            Assert.AreEqual(pos, s2.Length - 2, "wrong seek from current");
            Assert.AreEqual(pos, s2.Position, "wrong position after seek from current");

            pos = s2.Seek(-100, SeekOrigin.Current);
            Assert.AreEqual(pos, 0, "wrong seek from current");
            Assert.AreEqual(pos, s2.Position, "wrong position after seek from current");

            pos = s2.Seek(s2.Length + 100, SeekOrigin.Current);
            Assert.AreEqual(pos, s2.Length, "wrong seek past the end");
            Assert.AreEqual(pos, s2.Position, "wrong position after seek past the end");

            s.Close();

            if (hasFileSystemCache)
            {
                Assert.AreEqual(((CachedStream)s2).NumClonesSharingFileSystemCache, 1, "number of clones is wrong");
            }
            else
            {
                Assert.AreEqual(((CachedStream)s2).NumClonesSharingFileSystemCache, 0, "number of clones is wrong");
            }

            s2.Close();

            Assert.AreEqual(((CachedStream)s2).NumClonesSharingFileSystemCache, 0, "number of clones is wrong");
        }
    }
}