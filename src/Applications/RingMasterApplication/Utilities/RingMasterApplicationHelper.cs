// <copyright file="RingMasterApplicationHelper.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterApplication.Utilities
{
    using System;
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
    }
}
