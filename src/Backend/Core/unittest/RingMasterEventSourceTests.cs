namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterBackendCoreUnitTest
{
    using System;
    using System.Diagnostics.Tracing;
    using Backend;
    using Data;
    using Requests;
    using VisualStudio.TestTools.UnitTesting;
    
    // We can't run the tests in parallel since all of them use shared state.
    [DoNotParallelize]
    [TestClass]
    public class RingMasterEventSourceTests
    {
        [TestMethod]
        public void TryRemoveOnTerminateAction_Does_Nothing_If_Disabled()
        {
            var log = RingMasterEventSource.Log;

            var listener = new RingMasterEventListener(log.Name);

            // The event is Verbose, so this should not trace anything.
            listener.EnableEvents(log, EventLevel.Informational);
            var input = (sessionId: (ulong)42, actionName: "actionName", wasRemoved: true);
            log.TryRemoveOnTerminateAction(input.sessionId, input.actionName, input.wasRemoved);

            Assert.IsNull(listener.LastEventArgs);
        }
        
        [TestMethod]
        public void TryRemoveOnTerminate_Logs_If_Enabled()
        {
            var log = RingMasterEventSource.Log;

            var listener = new RingMasterEventListener(log.Name);

            listener.EnableEvents(log, EventLevel.Verbose);
            var input = (sessionId: (ulong)42, actionName: "actionName", wasRemoved: true);
            log.TryRemoveOnTerminateAction(input.sessionId, input.actionName, input.wasRemoved);
            
            Assert.IsNotNull(listener.LastEventArgs);
            var payload = listener.LastEventArgs.Payload;
            var data = ((ulong)payload[0], (string)payload[1], (bool)payload[2]);
            Assert.AreEqual(input, data);
        }

        [TestMethod]
        public void ProcessMessageSucceeded_Propagates_All_Parameters_Correctly()
        {
            var log = RingMasterEventSource.Log;
            
            var listener = new RingMasterEventListener(log.Name);
            
            listener.EnableEvents(log, EventLevel.Informational);
            var response = CreateFakeResponse();
            
            var oldArgs = new ProcessMessageArgs(SessionId: 1, RequestId: 2, Zxid: 3, RequestType: 4, Path: "a", ElapsedMilliseconds: 5, Response: response.ToString());
            log.ProcessMessageSucceeded(oldArgs.SessionId, oldArgs.RequestId, oldArgs.Zxid, oldArgs.RequestType, oldArgs.Path, oldArgs.ElapsedMilliseconds, response);

            Assert.AreEqual(oldArgs, listener.LastProcessMessageArgs);
        }
        
        [TestMethod]
        public void ProcessMessageSucceeded_No_Messages_Logged_If_Disabled()
        {
            NoMessagesLogged((EventKeywords)(-2));
            ulong keywords = 18446744073709551614UL;
            NoMessagesLogged((EventKeywords)(long)keywords);
            NoMessagesLogged(RingMasterEventSourceKeywords.ExcludeProcessMessage);
        }

        private void NoMessagesLogged(EventKeywords keywords)
        {
            var log = RingMasterEventSource.Log;

            var listener = new RingMasterEventListener(log.Name);

            listener.EnableEvents(log, EventLevel.Informational, keywords);
            var response = CreateFakeResponse();

            var oldArgs = new ProcessMessageArgs(SessionId: 1, RequestId: 2, Zxid: 3, RequestType: 4, Path: "a", ElapsedMilliseconds: 5, Response: response.ToString());
            log.ProcessMessageSucceeded(oldArgs.SessionId, oldArgs.RequestId, oldArgs.Zxid, oldArgs.RequestType, oldArgs.Path, oldArgs.ElapsedMilliseconds, response);

            Assert.IsNull(listener.LastProcessMessageArgs);
        }

        private static RequestResponse CreateFakeResponse() => new RequestResponse()
        {
            CallId = 42,
            ResultCode = (int)RingMasterException.Code.Connectionloss,
            Stat = new Stat(czxid: 42, mzxid: 1, ctime: DateTime.Now.Ticks, version: 42, mtime: 42, cversion: 1,
                aversion: 2, ephemeralOwner: 3, dataLength: 4, numChildren: 1, pzxid: -1, uversion: 7),
        };
    }

    internal record ProcessMessageArgs(ulong SessionId, ulong RequestId, long Zxid, int RequestType, string Path, long ElapsedMilliseconds, string Response);

    internal class RingMasterEventListener : EventListener
    {
        private readonly string targetEventSource;

        public RingMasterEventListener(string targetEventSource)
        {
            this.targetEventSource = targetEventSource;
        }

        public ProcessMessageArgs LastProcessMessageArgs { get; private set; }

        public EventWrittenEventArgs LastEventArgs { get; private set; }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventSource.Name == targetEventSource)
            {
                LastEventArgs = eventData;

                var payload = eventData.Payload;
                if (eventData.EventName == nameof(RingMasterEventSource.ProcessMessageSucceeded))
                {
                    LastProcessMessageArgs = new ProcessMessageArgs(SessionId: (ulong)payload[0],
                        RequestId: (ulong)payload[1], Zxid: (long)payload[2], RequestType: (int)payload[3],
                        Path: (string)payload[4], ElapsedMilliseconds: (long)payload[5], Response: (string)payload[6]);
                }
            }
        }
    }
}
