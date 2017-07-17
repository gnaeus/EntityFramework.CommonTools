using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;

namespace EntityFramework.ChangeTrackingExtensions
{
    public static partial class DbContextExtensions
    {
        /// <summary>
        /// Disposable region where <paramref name="dbContext"/> AutoDetectChanges is disabled.
        /// </summary>
        public static IDisposable WithoutChangeTracking(this DbContext dbContext)
        {
            return new AutoDetectChangesContext(dbContext.Configuration);
        }

        /// <summary>
        /// Run <see cref="DbChangeTracker.DetectChanges"/> once and return
        /// disposable region where <paramref name="dbContext"/> AutoDetectChanges is disabled.
        /// </summary>
        public static IDisposable WithChangeTrackingOnce(this DbContext dbContext)
        {
            IDisposable result = dbContext.WithoutChangeTracking();

            dbContext.ChangeTracker.DetectChanges();

            return result;
        }

        private struct AutoDetectChangesContext : IDisposable
        {
            readonly DbContextConfiguration _configuration;
            readonly bool _autoDetectChangesEnabled;

            public AutoDetectChangesContext(DbContextConfiguration configuration)
            {
                _configuration = configuration;
                _autoDetectChangesEnabled = _configuration.AutoDetectChangesEnabled;
                _configuration.AutoDetectChangesEnabled = false;
            }

            public void Dispose()
            {
                _configuration.AutoDetectChangesEnabled = _autoDetectChangesEnabled;
            }
        }
    }
}
