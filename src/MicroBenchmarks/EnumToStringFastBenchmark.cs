namespace MicroBenchmarks;

using BenchmarkDotNet.Attributes;
using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;

/// <summary>
/// Here are the results:
/// | Method          | Mean     | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
/// |---------------- |---------:|---------:|---------:|------:|-------:|----------:|------------:|
/// | DefaultToString | 751.8 ns | 14.54 ns | 12.14 ns |  1.00 | 0.0410 |     696 B |        1.00 |
/// | ToStringFast    | 252.2 ns |  2.48 ns |  2.32 ns |  0.34 |      - |         - |        0.00 |
/// </summary>
[MemoryDiagnoser]
public class EnumToStringFastBenchmark
{
    private static Code[] allStatusCodes = Enum.GetValues<Code>().ToArray();

    [Benchmark(Baseline = true)]
    public void DefaultToString()
    {
        foreach (var c in allStatusCodes)
        {
            c.ToString();
        }
    }

    [Benchmark]
    public void ToStringFast()
    {
        foreach (var c in allStatusCodes)
        {
            c.ToStringFast();
        }
    }

    /// <summary>
    /// This is a copy of the result codes from RingMaster common. We don't need to sync it in the future, we just need an enum with a reasonable amount of elements
    /// in it in order to make sure the benchmark results are relevant.
    /// </summary>
    public enum Code
    {
        /// <summary>
        /// An API was not used correctly.
        /// </summary>
        Apierror,

        /// <summary>
        /// Client authentication failed.
        /// </summary>
        Authfailed,

        /// <summary>
        /// Invalid arguments.
        /// </summary>
        Badarguments,

        /// <summary>
        /// Version conflict.
        /// </summary>
        Badversion,

        /// <summary>
        /// Connection to the server has been lost.
        /// </summary>
        Connectionloss,

        /// <summary>
        /// A data inconsistency was found.
        /// </summary>
        Datainconsistency,

        /// <summary>
        /// Invalid <see cref="Acl"/> was specified.
        /// </summary>
        Invalidacl,

        /// <summary>
        /// Invalid callback specified
        /// </summary>
        Invalidcallback,

        /// <summary>
        /// Error while marshaling or un-marshaling data.
        /// </summary>
        Marshallingerror,

        /// <summary>
        /// Not authenticated.
        /// </summary>
        Noauth,

        /// <summary>
        /// Ephemeral nodes are not allowed to have children.
        /// </summary>
        Nochildrenforephemerals,

        /// <summary>
        /// The node already exists.
        /// </summary>
        Nodeexists,

        /// <summary>
        /// Node does not exist.
        /// </summary>
        Nonode,

        /// <summary>
        /// The node has children.
        /// </summary>
        Notempty,

        /// <summary>
        /// Everything is OK.
        /// </summary>
        Ok,

        /// <summary>
        /// The request wasn't sent to backend and timed out from client side.
        /// </summary>
        Operationtimeout,

        /// <summary>
        /// A runtime inconsistency was found.
        /// </summary>
        Runtimeinconsistency,

        /// <summary>
        /// The session has been expired by the server.
        /// </summary>
        Sessionexpired,

        /// <summary>
        /// Session moved to another server, so operation is ignored.
        /// </summary>
        Sessionmoved,

        /// <summary>
        /// System and server-side errors.
        /// </summary>
        Systemerror,

        /// <summary>
        /// Operation is unimplemented.
        /// </summary>
        Unimplemented,

        /// <summary>
        /// Unknown error.
        /// </summary>
        Unknown,

        /// <summary>
        /// Participants did not agree on the transaction.
        /// </summary>
        TransactionNotAgreed,

        /// <summary>
        /// Operation timeout on server (the request comes with a max timeout for the execution queue at the server that was not met).
        /// </summary>
        Waitqueuetimeoutonserver,

        /// <summary>
        /// The server is in lockdown
        /// </summary>
        InLockDown,

        /// <summary>
        /// The requested node has too many children to be enumerated in a single request.
        /// </summary>
        TooManyChildren,

        /// <summary>
        /// The operation was cancelled.
        /// </summary>
        OperationCancelled,

        /// <summary>
        /// Operation timeout in backend
        /// </summary>
        ServerOperationTimeout,

        /// <summary>
        /// Root is null.
        /// </summary>
        RootNull,
    }
}
