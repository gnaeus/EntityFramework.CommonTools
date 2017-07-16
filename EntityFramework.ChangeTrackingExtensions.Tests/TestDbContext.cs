using System;
using System.Data.Common;
using System.Data.Entity;
using System.Diagnostics;
using System.Threading.Tasks;

namespace EntityFramework.ChangeTrackingExtensions.Tests
{
    public class TestDbContext : DbContext
    {
        public DbSet<SettingsElement> Settings { get; set; }

        public TestDbContext(DbConnection connection)
            : base(connection, false)
        {
            Database.Log = s => Debug.WriteLine(s);
            Database.SetInitializer<TestDbContext>(null);
        }
    }
}
