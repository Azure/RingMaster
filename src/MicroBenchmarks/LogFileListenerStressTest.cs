namespace MicroBenchmarks;

using System.Diagnostics;
using System.Diagnostics.Tracing;
using Microsoft.Azure.Networking.Infrastructure.RingMaster;
using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend;
using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

public class LogFileListenerStressTest
{
    internal static readonly RequestResponse response = new RequestResponse()
    {
        CallId = 42,
        ResultCode = (int)RingMasterException.Code.Connectionloss,
        Stat = new Stat(czxid: 42, mzxid: 1, ctime: DateTime.Now.Ticks, version: 42, mtime: 42, cversion: 1,
            aversion: 2, ephemeralOwner: 3, dataLength: 4, numChildren: 1, pzxid: -1, uversion: 7),
    };

    public static void RunV1(int threadCount, int count)
    {
        // Wrote 10000000 messages with 1 threads in 23639ms. ReceivedTraces: 10000001, DroppedTraces: 0, WrittenChars (Mills): 3518
        // Wrote 10000000 messages with 4 threads in 10274ms. ReceivedTraces: 10000001, DroppedTraces: 1824379, WrittenChars (Mills): 2874
        var logDirectory = Path.Combine(Environment.CurrentDirectory, "TestLogsV1");
        CleanIfNeeded(logDirectory);
        LogFileEventTracing.Start(logDirectory);
        LogFileEventTracing.AddEventSource(RingMasterEventSource.Log.Name, EventLevel.Informational);

        var sw = Stopwatch.StartNew();

        Parallel.For(0, threadCount,
            _ =>
            {
                for (int i = 0; i < count; i++)
                {
                    RingMasterEventSource.Log.ProcessMessageSucceeded(42, 1, 2, 3, "foobar", 42, response.ToStringFast());
                }
            });

        var elapsed = sw.ElapsedMilliseconds;
        Console.WriteLine("Waiting for 5s to allow more events to flush.");

        Thread.Sleep(5000);
        Console.WriteLine("Done waiting");

        Console.WriteLine($"Wrote {count * threadCount} messages with {threadCount} threads in {elapsed}ms. ReceivedTraces: {LogFileEventTracing.ReceivedTraceCount}, DroppedTraces: {LogFileEventTracing.DroppedTraceCount}, WrittenChars (Mills): {LogFileEventTracing.TotalWrittenChars / 1_000_000}");
    }
    
    public static void RunV2(int threadCount, int count)
    {
        // Wrote 10000000 messages with 1 threads in 9177ms. ReceivedTraces: 10000001, DroppedTraces: 0, WrittenChars(Mills): 3538
        // Wrote 10000000 messages with 4 threads in 4297ms. ReceivedTraces: 10000001, DroppedTraces: 6447277, WrittenChars(Mills): 1256
        var logDirectory = Path.Combine(Environment.CurrentDirectory, "TestLogsV2");
        CleanIfNeeded(logDirectory);
        LogFileEventTracingV2.Start(logDirectory);
        LogFileEventTracingV2.AddEventSource(RingMasterEventSource.Log.Name, EventLevel.Informational);

        var sw = Stopwatch.StartNew();

        Parallel.For(0, threadCount,
            _ =>
            {
                for (int i = 0; i < count; i++)
                {
                    RingMasterEventSource.Log.ProcessMessageSucceeded(42, 1, 2, 3, "foobar", 42, response.ToStringFast());
                }
            });

        var elapsed = sw.ElapsedMilliseconds;
        Console.WriteLine("Waiting for 5s to allow more events to flush.");

        Thread.Sleep(5000);
        Console.WriteLine("Done waiting");
        Console.WriteLine($"Wrote {count * threadCount} messages with {threadCount} threads in {elapsed}ms. ReceivedTraces: {LogFileEventTracingV2.ReceivedTraceCount}, DroppedTraces: {LogFileEventTracingV2.DroppedTraceCount}, WrittenChars(Mills): {LogFileEventTracingV2.TotalWrittenChars / 1_000_000}");
    }

    private static void CleanIfNeeded(string logDirectory)
    {
        if (Directory.Exists(logDirectory))
        {
            Directory.Delete(logDirectory, recursive: true);
        }

        Directory.CreateDirectory(logDirectory);
    }
}
