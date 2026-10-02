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

        private readonly bool useLogFileEventTracingV2;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogFileTraceListener"/> class.
        /// </summary>
        /// <param name="useLogFileEventTracingV2">Whether to use <see cref="LogFileEventTracingV2"/> or not.</param>
        public LogFileTraceListener(bool useLogFileEventTracingV2 = false)
        {
            this.useLogFileEventTracingV2 = useLogFileEventTracingV2;
        }

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

            if (this.useLogFileEventTracingV2)
            {
                LogFileEventTracingV2.Trace(messageLine);
            }
            else
            {
                LogFileEventTracing.Trace(messageLine);
            }
        }
    }
}
