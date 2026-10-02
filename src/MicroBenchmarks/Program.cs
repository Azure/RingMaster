namespace MicroBenchmarks
{
    using BenchmarkDotNet.Running;

    internal class Program
    {
        static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(EnumToStringFastBenchmark).Assembly).Run();
        }
    }
}
