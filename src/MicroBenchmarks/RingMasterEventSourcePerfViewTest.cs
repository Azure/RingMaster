namespace MicroBenchmarks;

using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend;

/// <summary>
/// This is not a benchmark, but for simplicity its placed in this project and intended for manual runs to check that we can see the events in PerfView if configured.
/// </summary>
public class RingMasterEventSourcePerfViewTest
{
    public static void EmitProcessMessageSucceededTraces()
    {
        for (int i = 0; i < 100; i++)
        {
            RingMasterEventSource.Log.ProcessMessageSucceeded(sessionId: (ulong)i, requestId: (ulong)i, zxid: 1, requestType: 1, path: "foo/bar", elapsedMilliseconds: 42, RequestResponseToStringBenchmarks.Response);
        }
    }
}
