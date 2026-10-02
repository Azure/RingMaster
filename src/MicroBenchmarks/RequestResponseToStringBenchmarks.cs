using BenchmarkDotNet.Attributes;
using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend;
using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

/// <summary>
/// The benchmark that shows the perf benefits of ISpanFormattable.
/// </summary>
/// <remarks>
/// | Method      | Mean     | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
/// |------------ |---------:|--------:|--------:|------:|-------:|----------:|------------:|
/// | OldToString | 735.3 ns | 7.67 ns | 7.17 ns |  1.00 | 0.0029 |     776 B |        1.00 |
/// | NewToString | 331.7 ns | 3.03 ns | 2.84 ns |  0.45 | 0.0005 |     224 B |        0.29 |
/// </remarks>
[MemoryDiagnoser]
public class RequestResponseToStringBenchmarks
{
    internal static readonly RequestResponse Response = new RequestResponse()
    {
        CallId = 42,
        ResultCode = (int)RingMasterException.Code.Connectionloss,
        Stat = new Stat(czxid: 42, mzxid: 1, ctime: DateTime.Now.Ticks, version: 42, mtime: 42, cversion: 1,
            aversion: 2, ephemeralOwner: 3, dataLength: 4, numChildren: 1, pzxid: -1, uversion: 7),
    };

    [Benchmark(Baseline = true)]
    public string OldToString() => Response.ToString();

    [Benchmark]
    public string NewToString() => Response.ToStringFast();
}
