// <copyright file="CloneableStreamTests.cs" company="Microsoft">
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
    public class CloneableStreamTests
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
                CloneableStream str = new CloneableStream(null);
                Assert.IsNotNull(str);
            });

            TestHelpers.AssertThrows<ArgumentNullException>(() =>
            {
                MemoryStream ms = new MemoryStream();
                CloneableStream str = new CloneableStream(ms, null);
                Assert.AreNotEqual(str, ms);
            });

            TestHelpers.AssertThrows<ArgumentNullException>(() =>
            {
                CloneableStream str = new CloneableStream(
                    null,
                    () => new MemoryStream());
                Assert.IsNotNull(str);
            });

            TestHelpers.AssertThrows<ArgumentNullException>(() =>
            {
                CloneableStream str = new CloneableStream(() => null);
                str.Dispose();
            });
        }

        /// <summary>
        /// This method tests clone works
        /// </summary>
        [TestMethod]
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Tests")]
        public void TestClone()
        {
            MemoryStream[] list = { new MemoryStream(), new MemoryStream() };
            int pos = 0;

            CloneableStream s = new CloneableStream(() => list[pos++]);

            byte[] one = { 1, 1, 1, 1 };
            byte[] two = { 2, 2 };

            s.Write(one, 0, one.Length);

            Assert.AreEqual(list[0].Position, one.Length, "write didn't go to the first stream");
            Assert.AreEqual(list[1].Position, 0, "second stream was altered");

            Stream s2 = s.CloneStream();

            Assert.IsTrue(s.CanRead);
            Assert.IsTrue(s.CanWrite);
            Assert.IsTrue(s.CanSeek);

            s2.Write(two, 0, two.Length);
            Assert.AreEqual(list[0].Position, one.Length, "first stream was altered");
            Assert.AreEqual(list[1].Position, two.Length, "write didn't go to the second stream");

            s.Position = 0;
            Assert.AreEqual(list[0].Position, 0, "first stream was not reset");
            s2.Position = 0;
            Assert.AreEqual(list[1].Position, 0, "second stream was not reset");

            Assert.AreEqual(list[0].Length, one.Length, "first stream has wrong length");
            Assert.AreEqual(list[1].Length, two.Length, "second stream has wrong length");

            byte[] readOne = new byte[one.Length];
            byte[] readTwo = new byte[two.Length];

            int read = s.Read(readOne, 0, readOne.Length);
            Assert.AreEqual(read, one.Length, "not all bytes read from first stream");
            Assert.AreEqual(s.Position, one.Length, "position wrong on first stream");
            Assert.AreEqual(list[0].Position, one.Length, "first stream has not repositioned properly");

            read = s2.Read(readTwo, 0, readTwo.Length);
            Assert.AreEqual(read, two.Length, "not all bytes read from second stream");
            Assert.AreEqual(s2.Position, two.Length, "position wrong on second stream");
            Assert.AreEqual(list[1].Position, two.Length, "second stream has not repositioned properly");

            TestHelpers.AssertBytesEqual(readOne, one, "buffer read from first stream was wrong");
            TestHelpers.AssertBytesEqual(readTwo, two, "buffer read from second stream was wrong");

            list[0].Position = 0;
            Assert.AreEqual(s.Position, 0, "first stream was not reset");
            list[1].Position = 0;
            Assert.AreEqual(s2.Position, 0, "second stream was not reset");

            readOne = new byte[one.Length];
            readTwo = new byte[two.Length];

            read = list[0].Read(readOne, 0, readOne.Length);
            Assert.AreEqual(read, one.Length, "not all bytes read from first stream");
            read = s2.Read(readTwo, 0, readTwo.Length);
            Assert.AreEqual(read, two.Length, "not all bytes read from second stream");

            TestHelpers.AssertBytesEqual(readOne, one, "buffer read from first underlying stream was wrong");
            TestHelpers.AssertBytesEqual(readTwo, two, "buffer read from second underlying stream was wrong");

            s.SetLength(100);
            Assert.AreEqual(list[0].Length, 100, "stream was not resized properly");
            Assert.AreEqual(s.Length, 100, "stream was not resized properly");

            s.Position = 2;
            Assert.AreEqual(list[0].Position, 2, "base stream was not repositioned ");
            Assert.AreEqual(s.Position, 2, "stream was not repositioned ");

            s.Flush();
            Assert.AreEqual(s.Position, 2, "stream is not flushed");

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
    }
}