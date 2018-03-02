
namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using IRingMasterRequestHandler = Microsoft.Azure.Networking.Infrastructure.RingMaster.IRingMasterRequestHandler;
    using RedirectionPolicy = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestInit.RedirectionPolicy;
    using CommonRequestInit = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestInit;
    using BackendRequestInit = Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.RequestInit;

    /// <summary>
    /// CoreRequestHandler class implements <see cref="IRingMasterRequestHandler"/> interface
    /// for a <see cref="ClientSession"/>.
    /// </summary>
    public class CoreRequestHandler : IRingMasterRequestHandler
    {
        private readonly IRingMasterRequestExecutor executor;
        private readonly ClientSession session;
        private readonly MarshallerChannel marshaller = new MarshallerChannel(null);

        private long lastAssignedCallId = 0;
        private bool isDisposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoreRequestHandler"/> class.
        /// </summary>
        /// <param name="executor">Interface to an object that can execute RingMasterRequests</param>
        /// <param name="initRequest">Init request</param>
        public CoreRequestHandler(IRingMasterRequestExecutor executor, CommonRequestInit initRequest = null)
        {
            if (executor == null)
            {
                throw new ArgumentNullException(nameof(executor));
            }

            this.executor = executor;
            this.session = new ClientSession((requestCall, clientSession, responseAction) =>
            {
                this.executor.ProcessMessage(requestCall.Request, clientSession, responseAction);
            });

            BackendRequestInit backendInitRequest = (initRequest != null) 
                ? new BackendRequestInit(initRequest, null)
                : new BackendRequestInit(0, Guid.NewGuid().ToString(), null, true, RedirectionPolicy.RedirectPreferred);

            InitResponse = this.executor.ProcessSessionInitialization(
                 new RequestCall
                 {
                     CallId = 0,
                     Request = backendInitRequest
                 },
                this.session);
        }

        /// <summary>
        /// The init Response.
        /// </summary>
        public RequestResponse InitResponse { get; private set; }

        /// <summary>
        /// Gets the timeout value in milliseconds.
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Handles ringmaster requests.
        /// </summary>
        /// <param name="request">RingMaster request</param>
        /// <returns>Response for the request</returns>
        /// <remarks>
        /// Implementing the <see cref="IRingMasterRequestHandler"/> makes it possible for
        /// several libraries to work directly with the RingMasterBackendCore. This class
        /// is being implemented here to avoid having to expose internal classes outside of
        /// this library.
        /// </remarks>
        public Task<RequestResponse> Request(IRingMasterRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            RequestCall call = this.marshaller.LocalRequest(request);
            call.CallId = (ulong)Interlocked.Increment(ref this.lastAssignedCallId);

            var taskCompletionSource = new TaskCompletionSource<RequestResponse>();

            this.executor.ProcessMessage(call.Request, this.session, response =>
            {
                response.CallId = call.CallId;
                taskCompletionSource.SetResult(response);
            });

            return taskCompletionSource.Task;
        }

        /// <summary>
        /// Closes the request handler
        /// </summary>
        public void Close()
        {
            this.session.Close();
            this.marshaller.Close();
        }

        /// <summary>
        /// Disposes the request handler.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (isDisposing && !this.isDisposed)
            {
                this.isDisposed = true;
                this.Close();
                this.marshaller.Dispose();
            }
        }
    }
}