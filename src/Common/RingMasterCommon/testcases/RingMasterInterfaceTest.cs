// <copyright file="RingMasterInterfaceTest.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.TestCases
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Base class for RingMaster interface tests.
    /// </summary>
    public abstract class RingMasterInterfaceTest
    {
        public RingMasterInterfaceTest()
        {
        }

        /// <summary>
        /// Establishes a connection to the ring master instance that is being exercised
        /// by this test.
        /// </summary>
        /// <returns>A <see cref="IRingMasterRequestHandler"/> object that represents the connection.</returns>
        public Func<IRingMasterRequestHandler> ConnectToRingMaster { get; set; }

        /// <summary>
        /// Connects to ring master commander.
        /// </summary>
        /// <returns>A <see cref="IRingMasterRequestHandler"/> object that represents the connection.</returns>
        public virtual IRingMasterRequestHandler ConnectToRingMasterCommander()
        {
            IRingMasterRequestHandler rm = this.ConnectToRingMaster();
            rm.SetAuth(new Id("digest", "commander")).Wait();
            return rm;
        }

        /// <summary>
        /// Verify that the given task throws an exception of type T.
        /// </summary>
        /// <param name "expectedCode">Expected RingMasterException error code</param>
        /// <param name="task">Task that is expected to throw the exception</param>
        /// <param name="message">Error message to show if the task did not throw the exception</param>
        /// <returns>A <see cref="Task"/> that tracks execution of this function</returns>
        internal static async Task VerifyRingMasterException(RingMasterException.Code expectedCode, Func<Task> task, string message)
        {
            try
            {
                await task();
            }
            catch (RingMasterException ex)
            {
                Assert.AreEqual(expectedCode, ex.ErrorCode);
                return;
            }

            Assert.Fail(message);
        }

        /// <summary>
        /// Verify that the actual buffer contains the same bytes as the expected buffer.
        /// </summary>
        /// <param name="expected">Expected bytes</param>
        /// <param name="actual">Actual bytes</param>
        /// <param name="message">optional message on failure</param>
        internal static void VerifyBytesAreEqual(byte[] expected, byte[] actual, string message = null)
        {
            if (message != null)
            {
                Assert.AreEqual(expected.Length, actual.Length, message);
            }
            else
            {
                Assert.AreEqual(expected.Length, actual.Length);
            }

            for (int i = 0; i < expected.Length; i++)
            {
                if (message != null)
                {
                    Assert.AreEqual(expected[i], actual[i], message);
                }
                else
                {
                    Assert.AreEqual(expected[i], actual[i]);
                }
            }
        }

        /// <summary>
        /// Verify that the given Acl lists are equal.
        /// </summary>
        /// <param name="expected">Expected Acl list</param>
        /// <param name="actual">Actual Acl list</param>
        internal static void VerifyAclListsAreEqual(IReadOnlyList<Acl> expected, IReadOnlyList<Acl> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.IsTrue(Acl.AreEqual(expected[i], actual[i]));
            }
        }
    }
}