using System;
using System.Data.Common;
using System.Data.Entity;
using System.Diagnostics;
using System.Threading.Tasks;

namespace EntityFramework.ChangeTrackingExtensions.Tests
{
    public class TestDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Settings> Settings { get; set; }

        public TestDbContext(DbConnection connection)
            : base(connection, false)
        {
            Database.Log = s => Debug.WriteLine(s);
            Database.SetInitializer<TestDbContext>(null);
        }

        // TODO: what about ChangeTracker.DetectChanges() ?
        public override int SaveChanges()
        {
            bool autoDetectChangesEnabled = Configuration.AutoDetectChangesEnabled;
            Configuration.AutoDetectChangesEnabled = false;
            try
            {
                ChangeTracker.DetectChanges();

                this.UpdateTrackableEntities();

                this.UpdateConcurrentEntities();

                return this.SaveChangesWithTransactionLog(base.SaveChanges);
            }
            finally
            {
                Configuration.AutoDetectChangesEnabled = autoDetectChangesEnabled;
            }
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.UseTransactionLog();
        }
    }
}
