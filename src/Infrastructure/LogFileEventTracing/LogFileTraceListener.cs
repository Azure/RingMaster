// <copyright file="LogFileTraceListener.cs" company="Microsoft Corporation">
//    Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System.Diagnostics;
    using System.Text;

    /// <summary>
    /// Trace listener to write to log files
    /// </summary>
    public sealed class LogFileTraceListener : TraceListener
    {
        /// <summary>
        /// String buffer to save the incomplete trace
        /// </summary>
        private readonly StringBuilder stringBuffer = new StringBuilder(64 * 1024);

        /// <summary>
        /// Writes incomplete message to trace
        /// </summary>
        /// <param name="message">Message to be written</param>
        public override void Write(string message)
        {
            lock (this)
            {
                this.stringBuffer.Append(message);
            }
        }

        /// <summary>
        /// Write a trace message
        /// </summary>
        /// <param name="message">Message to be written</param>
        public override void WriteLine(string message)
        {
            string messageLine;
            lock (this)
            {
                this.stringBuffer.Append(message);
                messageLine = this.stringBuffer.ToString();
                this.stringBuffer.Clear();
            }

            LogFileEventTracing.Trace(messageLine);
        }
    }
}
