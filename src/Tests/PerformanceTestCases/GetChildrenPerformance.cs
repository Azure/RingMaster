// <copyright file="GetChildrenPerformance.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Performance
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    public class GetChildrenPerformance : IDisposable
    {
        /// <summary>
        /// Minimum valid code point that can be used in a string.
        /// </summary>
        private const int MinCodePoint = 0x0;

        /// <summary>
        /// Maximum valid code point that can be used in a string.
        /// </summary>
        private const int MaxCodePoint = 0x10FFFF;

        private readonly CancellationToken cancellationToken;
        private readonly IInstrumentation instrumentation;
        private readonly SemaphoreSlim semaphore;

        public GetChildrenPerformance(IInstrumentation instrumentation, int maxConcurrentRequests, CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;
            this.semaphore = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
            this.instrumentation = instrumentation;
        }

        public interface IInstrumentation
        {
            void GetChildrenSucceeded(string nodePath, int childrenCount, TimeSpan elapsed);

            void GetChildrenFailed(string nodePath);
        }

        /// <summary>
        /// Gets or sets the minimum number of codepoints in a node name.
        /// </summary>
        public int MinNodeNameLength { get; set; } = 2;

        /// <summary>
        /// Gets or sets the maximum number of codepoints in a node name.
        /// </summary>
        public int MaxNodeNameLength { get; set; } = 8;

        /// <summary>
        /// Queue GetChildren requests.
        /// </summary>
        /// <param name="ringMaster">RingMaster client</param>
        /// <param name="rootPath">Path to enumerate</param>
        /// <param name="maxChildren">Maximum number of children to retrieve</param>
        public void QueueRequests(IRingMasterRequestHandler ringMaster, string rootPath, int maxChildren)
        {
            if (ringMaster == null)
            {
                throw new ArgumentNullException(nameof(ringMaster));
            }

            var random = new RandomGenerator();
            ulong requestId = 0;
            Trace.TraceInformation($"Queue GetChildren: path={rootPath}");

            while (!this.cancellationToken.IsCancellationRequested)
            {
                string startingChildName = random.GetRandomName(this.MinNodeNameLength, this.MaxNodeNameLength);
                var getChildrenRequest = new RequestGetChildren(rootPath, watcher: null, retrievalCondition: $">:{maxChildren}:{startingChildName}", uid: requestId++);

                this.semaphore.Wait();
                var timer = Stopwatch.StartNew();

                ringMaster.Request(getChildrenRequest).ContinueWith(responseTask =>
                {
                    this.semaphore.Release();
                    timer.Stop();

                    try
                    {
                        RequestResponse response = responseTask.Result;
                        if (response.ResultCode == (int)RingMasterException.Code.Ok)
                        {
                            var children = (IReadOnlyList<string>)response.Content;
                            this.instrumentation?.GetChildrenSucceeded(rootPath, children.Count, timer.Elapsed);
                        }
                        else
                        {
                            this.instrumentation?.GetChildrenFailed(rootPath);
                        }
                    }
                    catch (Exception)
                    {
                        this.instrumentation?.GetChildrenFailed(rootPath);
                    }
                });
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                this.semaphore.Dispose();
            }
        }
    }
}
