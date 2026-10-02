// <copyright file="RingMasterApplicationHelper.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterApplication.Utilities
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    /// <summary>
    /// Collection of helper methods
    /// </summary>
    public static class RingMasterApplicationHelper
    {
        /// <summary>
        /// Waits to attach the debugger
        /// </summary>
        /// <param name="debuggerAttachTimeoutInSeconds">The debugger attach timeout in seconds</param>
        public static void AttachDebugger(int debuggerAttachTimeoutInSeconds)
        {
            try
            {
                // Wait for debugger to connect if debugger timeout is specified
                if (debuggerAttachTimeoutInSeconds > 0)
                {
                    int iter = 0;
                    do
                    {
                        if ((iter > debuggerAttachTimeoutInSeconds) || System.Diagnostics.Debugger.IsAttached)
                        {
                            break;
                        }

                        iter++;
                        Thread.Sleep(1000);
                    }
                    while (true);
                }
            }
            catch (Exception)
            {
                ////
                // Swallow Exception and dont allow debugger to attach
                ////
            }
        }

        /// <summary>
        /// Converts a hostname into ip-address
        /// </summary>
        /// <param name="host">hostname that needs to be converted</param>
        /// <returns>an ip-address in s tring format</returns>
        public static string GetHostIp(string host)
        {
            IPAddress ip;
            if (IPAddress.TryParse(host, out ip))
            {
                return ip.ToString();
            }

            IPAddress[] ips = Dns.GetHostAddresses(host);
            foreach (IPAddress ipAddress in ips)
            {
                if (ipAddress.AddressFamily != AddressFamily.InterNetwork)
                {
                    continue;
                }

                ip = ipAddress;
                return ip.ToString();
            }

            return null;
        }
    }
}
