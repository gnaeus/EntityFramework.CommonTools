using System;
using System.Linq;
using BenchmarkDotNet.Attributes;

#if EF_CORE
using Microsoft.Data.Sqlite;

namespace EntityFrameworkCore.CommonTools.Benchmarks
#else
using System.Data.Common;

namespace EntityFramework.CommonTools.Benchmarks
#endif
{
    public class DatabaseQueryBenchmark
    {
#if EF_CORE
        private readonly SqliteConnection _connection = Context.CreateConnection();
#else
        private readonly DbConnection _connection = Context.CreateConnection();
#endif

        [Benchmark(Baseline = true)]
        public object RawQuery()
        {
            using (var context = new Context(_connection))
            {
                DateTime today = DateTime.Now.Date;
                
                return context.Users
                    .Where(u => u.Posts.Any(p => p.Date > today))
                    .FirstOrDefault();
            }
        }
        
        [Benchmark]
        public object ExpandableQuery()
        {
            using (var context = new Context(_connection))
            {
                return context.Users
                    .AsExpandable()
                    .Where(u => u.Posts.FilterToday().Any())
                    .ToList();
            }
        }

        private readonly Random _random = new Random();

        [Benchmark]
        public object NotCachedQuery()
        {
            using (var context = new Context(_connection))
            {
                int[] postIds = new[] { _random.Next(), _random.Next() };

                return context.Users
                    .Where(u => u.Posts.Any(p => postIds.Contains(p.Id)))
                    .ToList();
            }
        }
    }
}
