using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EntityFrameworkCore.ChangeTrackingExtensions.Tests
{
    public class TestDbContext : DbContext
    {
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Settings> Settings { get; set; }
        
        public DbSet<TransactionLog> TransactionLogs { get; set; }
        
        public TestDbContext(string databaseName)
            : base(new DbContextOptionsBuilder<TestDbContext>()
                  .UseInMemoryDatabase(databaseName)
                  .UseLoggerFactory(TestLoggerProvider.MakeLoggerFactory())
                  .ConfigureWarnings(warnings =>
                  {
                      warnings.Log(InMemoryEventId.TransactionIgnoredWarning);
                  })
                  .Options)
        {
            Database.EnsureCreated();
        }

        public TestDbContext(SqliteConnection connection)
            : base(new DbContextOptionsBuilder<TestDbContext>()
                  .UseSqlite(connection)
                  .UseLoggerFactory(TestLoggerProvider.MakeLoggerFactory())
                  .ConfigureWarnings(warnings =>
                  {
                      warnings.Log(InMemoryEventId.TransactionIgnoredWarning);
                  })
                  .Options)
        {
            Database.EnsureCreated();

            Database.ExecuteSqlCommand(@"
                CREATE TRIGGER IF NOT EXISTS TRG_Settings_UPD
                    AFTER UPDATE ON Settings
                    WHEN old.RowVersion = new.RowVersion
                BEGIN
                    UPDATE Settings
                    SET RowVersion = RowVersion + 1;
                END;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseTransactionLog();

            modelBuilder.Entity<Settings>()
                .Property(s => s.RowVersion)
                .HasDefaultValue(0);
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

        public int SaveChanges(int editorUserId)
        {
            this.UpdateAuditableEntities(editorUserId);

            return SaveChanges();
        }

        public Task<int> SaveChangesAsync(string editorUser)
        {
            this.UpdateAuditableEntities(editorUser);

            return SaveChangesAsync();
        }

        public void OriginalSaveChanges()
        {
            base.SaveChanges(true);
        }
    }
}
