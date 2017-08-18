using System;
using System.Linq;
using BenchmarkDotNet.Attributes;

#if EF_CORE
namespace EntityFrameworkCore.CommonTools.Benchmarks
#else
namespace EntityFramework.CommonTools.Benchmarks
#endif
{
    public class EnumerableQueryBenchmark
    {
        [Benchmark(Baseline = true)]
        public object RawQuery()
        {
            DateTime today = DateTime.Now.Date;

            return Enumerable.Empty<User>()
                .AsQueryable()
                .Where(u => u.Posts.Any(p => p.Date > today))
                .ToList();
        }

        [Benchmark]
        public object ExpandableQuery()
        {
            return Enumerable.Empty<User>()
                .AsQueryable()
                .AsExpandable()
                .Where(u => u.Posts.FilterToday().Any())
                .ToList();
        }
    }
}
