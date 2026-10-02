namespace MicroBenchmarks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend;

/// <summary>
/// The results:
/// As you can see, the struct-based enumeration is almost twice as fast and also has no allocations, compared to a normal enumeration.
/// | Method                    | Mean      | Error    | Ratio | Allocated |
/// |-------------------------- |----------:|---------:|------:|----------:|
/// | EnumerateSortedList       | 112.94 ns | 0.243 ns |  1.00 |      48 B |
/// | StructEnumerateSortedList |  65.72 ns | 0.141 ns |  0.58 |         - |
/// </summary>
[HideColumns(Column.StdDev, Column.RatioSD, Column.AllocRatio)]
[MemoryDiagnoser]
public class SortedListBenchmarks
{
    private readonly SortedList<int, int> sortedList = CreateSortedList();

    private static SortedList<int, int> CreateSortedList()
    {
        var result = new SortedList<int, int>();
        for (int i = 10; i >= 0; i--)
        {
            result.Add(i, i + 5);
        }
        return result;
    }

    [Benchmark(Baseline = true)]
    public void EnumerateSortedList()
    {
        foreach (var pair in sortedList) { /* Do nothing*/ }
    }

    [Benchmark]
    public void StructEnumerateSortedList()
    {
        foreach (var pair in sortedList.AsStructEnumerable()) { /* Do nothing*/ }
    }
}
