// <copyright file="RMAssert.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    /// <summary>
    /// Class RmAssert.
    /// </summary>
    public class RmAssert
    {
        /// <summary>
        /// Determines whether the specified expression is true.
        /// </summary>
        /// <param name="expression">if set to <c>true</c> [expression].</param>
        /// <param name="reason">if provided, the reason for the fault</param>
        public static void IsTrue(bool expression, string reason = null)
        {
            if (!expression)
            {
                if (reason == null)
                {
                    reason = "Exiting due to failed expression";
                }

                string m = reason + " " + new StackTrace(true);
                Fail(m);
            }
        }

        /// <summary>
        /// Fails the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void Fail(string message)
        {
            Trace.WriteLine(message);
            Debug.WriteLine(message);
            Console.WriteLine(message);
            Console.Out.Flush();

            Thread t = new Thread(
                () =>
                {
                    throw new InvalidOperationException(message);
                });

            t.Start();
            t.Join();
        }
    }
}