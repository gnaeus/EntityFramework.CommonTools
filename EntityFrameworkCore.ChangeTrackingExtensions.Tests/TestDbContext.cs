using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EntityFrameworkCore.ChangeTrackingExtensions.Tests
{
    public class TestDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Settings> Settings { get; set; }

        public TestDbContext(string databaseName)
            : base(new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(warnings =>
                {
                    warnings.Log(InMemoryEventId.TransactionIgnoredWarning);
                })
                .Options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseTransactionLog();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            this.UpdateTrackableEntities();
            this.UpdateConcurrentEntities();

            return this.SaveChangesWithTransactionLog(base.SaveChanges, acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            this.UpdateTrackableEntities();
            this.UpdateConcurrentEntities();

            return this.SaveChangesWithTransactionLogAsync(
                base.SaveChangesAsync, acceptAllChangesOnSuccess, cancellationToken);
        }
    }
}
