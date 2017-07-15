using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.ChangeTrackingExtensions
{
    class Experiments
    {
        class TestDbContext : DbContext
        {

        }

        void Test()
        {
            var ctx = new TestDbContext();

            using (ctx.Database.BeginTransaction())
            {

            }
        }
    }
}
