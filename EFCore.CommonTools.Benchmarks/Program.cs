using BenchmarkDotNet.Running;

#if EF_CORE
namespace EntityFrameworkCore.CommonTools.Benchmarks
#else
namespace EntityFramework.CommonTools.Benchmarks
#endif

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
