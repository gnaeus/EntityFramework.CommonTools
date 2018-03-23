using System;
using System.Linq;
using System.Threading.Tasks;

#if EF_CORE
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EntityFrameworkCore.CommonTools
#elif EF_6
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using EntityEntry = System.Data.Entity.Infrastructure.DbEntityEntry;

namespace EntityFramework.CommonTools
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

                if (entity is IConcurrencyCheckable<byte[]> concurrencyCheckableTimestamp)
                {
                    // take row version from entity that modified by client
                    dbEntry.OriginalValues[ROW_VERSION] = concurrencyCheckableTimestamp.RowVersion;
                }
                else if (entity is IConcurrencyCheckable<long> concurrencyCheckableLong)
                {
                    // take row version from entity that modified by client
                    dbEntry.OriginalValues[ROW_VERSION] = concurrencyCheckableLong.RowVersion;
                }
                else if (entity is IConcurrencyCheckable<Guid> concurrencyCheckableGuid)
                {
                    // take row version from entity that modified by client
                    dbEntry.OriginalValues[ROW_VERSION] = concurrencyCheckableGuid.RowVersion;
                    // generate new row version
                    concurrencyCheckableGuid.RowVersion = Guid.NewGuid();
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
        /// <exception cref="DbUpdateConcurrencyException" />
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
        /// <exception cref="DbUpdateConcurrencyException" />
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

            if (entity is IConcurrencyCheckable<byte[]> concurrencyCheckableTimestamp)
            {
                concurrencyCheckableTimestamp.RowVersion = (byte[])dbEntry.OriginalValues[ROW_VERSION];
            }
            else if (entity is IConcurrencyCheckable<long> concurrencyCheckableLong)
            {
                concurrencyCheckableLong.RowVersion = (long)dbEntry.OriginalValues[ROW_VERSION];
            }
            else if (entity is IConcurrencyCheckable<Guid> concurrencyCheckableGuid)
            {
                concurrencyCheckableGuid.RowVersion = (Guid)dbEntry.OriginalValues[ROW_VERSION];
            }
        }
    }
}
