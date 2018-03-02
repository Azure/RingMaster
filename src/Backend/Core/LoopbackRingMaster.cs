// ***********************************************************************
// Assembly         : RingMaster
// <copyright file="LoopbackRingMaster.cs" company="Microsoft">
//     Copyright ©  2017
// </copyright>
// <summary></summary>
// ***********************************************************************

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

        public LoopbackRingMaster(RingMasterBackendCore backend, bool readOnlyInterfaceRequiresLocks = true)
            : this(backend, readOnlyInterfaceRequiresLocks, true)
        {
        }

        public LoopbackRingMaster(RingMasterBackendCore backend, bool readOnlyInterfaceRequiresLocks, bool allowWrites)
            : base("loopback:0", 0, null)
        {
            if (backend == null)
            {
                throw new ArgumentNullException("backend");
            }

            this.backend = backend;
            this.session = backend.GetLoopbackSession("", false, allowWrites, readOnlyInterfaceRequiresLocks);
            this.executionQueue = new ExecutionQueue(maxThreads);
            this.session.ROInterfaceRequiresLocks = readOnlyInterfaceRequiresLocks;
        }

        public override int RequestTimeout
        {
            get;
        }

        public override int SessionTimeout
        {
            get;
        }

        public int MaxThreads
        {
            get
            {
                return maxThreads;
            }
            set
            {
                maxThreads = value;
                if (executionQueue != null)
                {
                    executionQueue.SetMaxThreads(value);
                }
            }
        }

        public override void AddAuthInfo(string scheme, byte[] auth)
        {
            base.AddAuthInfo(scheme, auth);
            byte[] digest;
            if (base.AuthsByScheme.TryGetValue(Scheme.Digest.ToString(), out digest))
            {
                session.SetClientDigest(string.Format("{0}:{1}", Scheme.Digest.ToString(), System.Text.Encoding.ASCII.GetString(digest)));
            }
        }

        public override void Close()
        {
            if (this.executionQueue == null)
            {
                return;
            }

            this.executionQueue.Drain(ExecutionQueue.DrainMode.DisallowAllFurtherEnqueues);
            session.Close();
            this.backend = null;
        }

        public override ISetDataOperationHelper GetSetDataOperationHelper()
        {
            return new SetDataOperationHelper();
        }

        public override void Initialize(int sessionTimeout, int requestTimeout)
        {
        }

        public override void Send(IRingMasterBackendRequest req)
        {
            executionQueue.Enqueue<IRingMasterBackendRequest>(SendSynchronously, req);
        }

        public void SetOnComplete(OnCompleteDelegate del)
        {
            this.del = del;
        }

        protected override void OnComplete(IRingMasterBackendRequest req, int resultcode, double timeInMillis)
        {
            OnCompleteDelegate d = del;
            if (d != null)
            {
                d(req, resultcode, timeInMillis);
            }
        }

        private void SendSynchronously(IRingMasterBackendRequest req)
        {
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                this.backend.ProcessMessage(
                    req,
                    this.session,
                    r =>
                    {
                        RequestResponse resp = r;
                        req.NotifyComplete(resp.ResultCode, resp.Content, resp.Stat);
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

        public delegate void OnCompleteDelegate(IRingMasterBackendRequest req, int resultcode, double timeInMillis);
    }
}