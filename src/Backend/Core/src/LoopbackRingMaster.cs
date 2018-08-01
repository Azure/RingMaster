// <copyright file="LoopbackRingMaster.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    using RequestResponse = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestResponse;

    /// <summary>
    /// Class LoopbackRingMaster. Implements an abstractRingMaster suitable to call directly a backend with no serialization.
    /// </summary>
    public class LoopbackRingMaster : AbstractRingMaster
    {
        private RingMasterBackendCore backend;
        private ClientSession session;
        private int maxThreads = 100;
        private ExecutionQueue executionQueue = null;
        private OnCompleteDelegate del = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoopbackRingMaster"/> class.
        /// </summary>
        /// <param name="backend">Backend core</param>
        /// <param name="readOnlyInterfaceRequiresLocks">Whether readonly interface requires locks</param>
        public LoopbackRingMaster(RingMasterBackendCore backend, bool readOnlyInterfaceRequiresLocks = true)
            : this(backend, readOnlyInterfaceRequiresLocks, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoopbackRingMaster"/> class.
        /// </summary>
        /// <param name="backend">Backend core</param>
        /// <param name="readOnlyInterfaceRequiresLocks">Whether readonly interface requires locks</param>
        /// <param name="allowWrites">Whether write operation is allowed</param>
        public LoopbackRingMaster(RingMasterBackendCore backend, bool readOnlyInterfaceRequiresLocks, bool allowWrites)
            : base("loopback:0", 0, null)
        {
            this.backend = backend ?? throw new ArgumentNullException("backend");
            this.session = backend.GetLoopbackSession(string.Empty, false, allowWrites, readOnlyInterfaceRequiresLocks);
            this.executionQueue = new ExecutionQueue(this.maxThreads);
            this.session.ROInterfaceRequiresLocks = readOnlyInterfaceRequiresLocks;
        }

        /// <summary>
        /// Callback to run on completion
        /// </summary>
        /// <param name="req">Backend request object</param>
        /// <param name="resultcode">Result code</param>
        /// <param name="timeInMillis">Time in millisecond</param>
        public delegate void OnCompleteDelegate(IRingMasterBackendRequest req, int resultcode, double timeInMillis);

        /// <inheritdoc />
        public override int RequestTimeout
        {
            get;
        }

        /// <inheritdoc />
        public override int SessionTimeout
        {
            get;
        }

        /// <summary>
        /// Gets or sets the maximum thread used by the execution queue
        /// </summary>
        public int MaxThreads
        {
            get
            {
                return this.maxThreads;
            }

            set
            {
                this.maxThreads = value;
                if (this.executionQueue != null)
                {
                    this.executionQueue.SetMaxThreads(value);
                }
            }
        }

        /// <inheritdoc />
        public override void AddAuthInfo(string scheme, byte[] auth)
        {
            base.AddAuthInfo(scheme, auth);
            byte[] digest;
            if (this.AuthsByScheme.TryGetValue(Scheme.Digest.ToString(), out digest))
            {
                this.session.SetClientDigest(string.Format("{0}:{1}", Scheme.Digest.ToString(), System.Text.Encoding.ASCII.GetString(digest)));
            }
        }

        /// <inheritdoc />
        public override void Close()
        {
            if (this.executionQueue == null)
            {
                return;
            }

            this.executionQueue.Drain(ExecutionQueue.DrainMode.DisallowAllFurtherEnqueues);
            this.session.Close();
            this.backend = null;
        }

        /// <inheritdoc />
        public override ISetDataOperationHelper GetSetDataOperationHelper()
        {
            return new SetDataOperationHelper();
        }

        /// <inheritdoc />
        public override void Initialize(int sessionTimeout, int requestTimeout)
        {
        }

        /// <inheritdoc />
        public override void Send(IRingMasterBackendRequest req)
        {
            this.executionQueue.Enqueue<IRingMasterBackendRequest>(this.SendSynchronously, req);
        }

        /// <summary>
        /// Sets the delegate to run on completion
        /// </summary>
        /// <param name="del">delegate to set</param>
        public void SetOnComplete(OnCompleteDelegate del)
        {
            this.del = del;
        }

        /// <inheritdoc />
        protected override void OnComplete(IRingMasterBackendRequest req, int resultcode, double timeInMillis)
        {
            this.del?.Invoke(req, resultcode, timeInMillis);
        }

        private void SendSynchronously(IRingMasterBackendRequest req)
        {
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                this.backend.ProcessMessage(
                    req,
                    this.session,
                    (r, e) =>
                    {
                        RequestResponse resp = r;
                        req.NotifyComplete(resp.ResultCode, resp.Content, resp.Stat, resp.ResponsePath);
                        this.OnComplete(req, resp.ResultCode, sw.ElapsedMilliseconds);
                        sw.Stop();
                    });
            }
            catch (TimeoutException)
            {
                this.OnComplete(req, (int)RingMasterException.Code.Operationtimeout, sw.ElapsedMilliseconds);
                sw.Stop();
            }
            catch (Exception)
            {
                this.OnComplete(req, (int)RingMasterException.Code.Systemerror, sw.ElapsedMilliseconds);
                sw.Stop();
            }
        }
    }
}
