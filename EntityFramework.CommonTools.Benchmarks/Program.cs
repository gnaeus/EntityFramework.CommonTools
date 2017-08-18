using BenchmarkDotNet.Running;

namespace EntityFramework.CommonTools.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            new BenchmarkSwitcher(new[] {
                typeof(EnumerableQueryBenchmark),
                typeof(DatabaseQueryBenchmark),
            }).Run(args);
        }
    }
}
