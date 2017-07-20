#if EF_CORE
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EntityFrameworkCore.ChangeTrackingExtensions
#else
using System;
using System.Linq;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Data.Entity.Infrastructure;
using EntityEntry = System.Data.Entity.Infrastructure.DbEntityEntry;

namespace EntityFramework.ChangeTrackingExtensions
#endif
{
    public static partial class DbContextExtensions
    {
        private const string ROW_VERSION = nameof(IConcurrencyCheckable<Guid>.RowVersion);

        /// <summary>
        /// Populate RowVersion propertiy of <see cref="IConcurrencyCheckable{T}"/>
        /// Entities in context from client-side values.
        /// </summary>
        /// <remarks>
        /// EF automatically detects if byde[] RowVersion is changed by reference (not only by value)
        /// and gentrates code like 'DECLARE @p int; UPDATE [Table] SET @p = 0 WHERE RowWersion = ...'
        /// </remarks>
        public static void UpdateConcurrentEntities(this DbContext dbContext)
        {
            DateTime utcNow = DateTime.UtcNow;

            var changedEntries = dbContext.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Modified
                         || e.State == EntityState.Deleted);

            foreach (var dbEntry in changedEntries)
            {
                object entity = dbEntry.Entity;

                var concurrencyCheckableTimestamp = entity as IConcurrencyCheckable<byte[]>;
                if (concurrencyCheckableTimestamp != null)
                {
                    // take row version from entity that modified by client
                    dbEntry.OriginalValues[ROW_VERSION] = concurrencyCheckableTimestamp.RowVersion;
                    continue;
                }
                var concurrencyCheckableLong = entity as IConcurrencyCheckable<long>;
                if (concurrencyCheckableLong != null)
                {
                    // take row version from entity that modified by client
                    dbEntry.OriginalValues[ROW_VERSION] = concurrencyCheckableLong.RowVersion;
                    continue;
                }
                var concurrencyCheckableGuid = entity as IConcurrencyCheckable<Guid>;
                if (concurrencyCheckableGuid != null)
                {
                    // take row version from entity that modified by client
                    dbEntry.OriginalValues[ROW_VERSION] = concurrencyCheckableGuid.RowVersion;
                    // generate new row version
                    concurrencyCheckableGuid.RowVersion = Guid.NewGuid();
                    continue;
                }
            }
#if !EF_CORE
            if (!dbContext.Configuration.AutoDetectChangesEnabled)
            {
                dbContext.ChangeTracker.DetectChanges();
            }
#endif
        }

        /// <summary>
        /// Save changes regardless of <see cref="DbUpdateConcurrencyException"/>.
        /// http://msdn.microsoft.com/en-us/data/jj592904.aspx
        /// </summary>
        public static void SaveChangesIgnoreConcurrency(
            this DbContext dbContext, int retryCount = 3)
        {
            int errorCount = 0;
            for (;;)
            {
                try
                {
                    dbContext.SaveChanges();
                    break;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (++errorCount > retryCount)
                    {
                        throw;
                    }
                    // update original values from the database 
                    EntityEntry dbEntry = ex.Entries.Single();
                    dbEntry.OriginalValues.SetValues(dbEntry.GetDatabaseValues());

                    UpdateRowVersionFromDb(dbEntry);
                }
            };
        }

        /// <summary>
        /// Save changes regardless of <see cref="DbUpdateConcurrencyException"/>.
        /// http://msdn.microsoft.com/en-us/data/jj592904.aspx
        /// </summary>
        public static async Task SaveChangesIgnoreConcurrencyAsync(
            this DbContext dbContext, int retryCount = 3)
        {
            int errorCount = 0;
            for (;;)
            {
                try
                {
                    await dbContext.SaveChangesAsync();
                    break;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (++errorCount > retryCount)
                    {
                        throw;
                    }
                    // update original values from the database 
                    EntityEntry dbEntry = ex.Entries.Single();
                    dbEntry.OriginalValues.SetValues(await dbEntry.GetDatabaseValuesAsync());

                    UpdateRowVersionFromDb(dbEntry);
                }
            };
        }

        private static void UpdateRowVersionFromDb(EntityEntry dbEntry)
        {
            object entity = dbEntry.Entity;

            var concurrencyCheckableTimestamp = entity as IConcurrencyCheckable<byte[]>;
            if (concurrencyCheckableTimestamp != null)
            {
                concurrencyCheckableTimestamp.RowVersion = (byte[])dbEntry.OriginalValues[ROW_VERSION];
                return;
            }
            var concurrencyCheckableLong = entity as IConcurrencyCheckable<long>;
            if (concurrencyCheckableLong != null)
            {
                concurrencyCheckableLong.RowVersion = (long)dbEntry.OriginalValues[ROW_VERSION];
                return;
            }
            var concurrencyCheckableGuid = entity as IConcurrencyCheckable<Guid>;
            if (concurrencyCheckableGuid != null)
            {
                concurrencyCheckableGuid.RowVersion = (Guid)dbEntry.OriginalValues[ROW_VERSION];
                return;
            }
        }
    }
}
