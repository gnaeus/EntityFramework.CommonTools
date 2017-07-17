using System.Data.Common;
using System.Data.Entity;
using System.Diagnostics;
using System.Threading;
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

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.UseTransactionLog();
        }

        public override int SaveChanges()
        {
            using (this.WithChangeTrackingOnce())
            {
                this.UpdateTrackableEntities();
                this.UpdateConcurrentEntities();

                return this.SaveChangesWithTransactionLog(base.SaveChanges);
            }
        }

        public int SaveChanges(int editorUserId)
        {
            using (this.WithChangeTrackingOnce())
            {
                this.UpdateAuditableEntities(editorUserId);
                this.UpdateTrackableEntities();
                this.UpdateConcurrentEntities();

                return this.SaveChangesWithTransactionLog(base.SaveChanges);
            }
        }

        public int SaveChanges(string editorUser)
        {
            using (this.WithChangeTrackingOnce())
            {
                this.UpdateAuditableEntities(editorUser);
                this.UpdateTrackableEntities();
                this.UpdateConcurrentEntities();

                return this.SaveChangesWithTransactionLog(base.SaveChanges);
            }
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            using (this.WithChangeTrackingOnce())
            {
                this.UpdateTrackableEntities();
                this.UpdateConcurrentEntities();

                return this.SaveChangesWithTransactionLogAsync(base.SaveChangesAsync, cancellationToken);
            }
        }

        public Task<int> SaveChangesAsync(int editorUserId)
        {
            using (this.WithChangeTrackingOnce())
            {
                this.UpdateAuditableEntities(editorUserId);
                this.UpdateTrackableEntities();
                this.UpdateConcurrentEntities();

                return this.SaveChangesWithTransactionLogAsync(base.SaveChangesAsync);
            }
        }

        public Task<int> SaveChangesAsync(string editorUser)
        {
            using (this.WithChangeTrackingOnce())
            {
                this.UpdateAuditableEntities(editorUser);
                this.UpdateTrackableEntities();
                this.UpdateConcurrentEntities();

                return this.SaveChangesWithTransactionLogAsync(base.SaveChangesAsync);
            }
        }
    }
}
