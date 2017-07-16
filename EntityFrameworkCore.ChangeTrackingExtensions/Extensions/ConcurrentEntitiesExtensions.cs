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
        /// <summary>
        /// Populate RowVersion propertiy for <see cref="IConcurrencyCheckable"/> or
        /// <see cref="ITimestampCheckable"/> Entities in context from client-side values.
        /// </summary>
        /// <remarks>
        /// EF automatically detects if byde[] RowVersion is changed by reference (not only by value)
        /// and gentrates code like 'DECLARE @p int; UPDATE [Table] SET @p = 0 WHERE RowWersion = ...'
        /// </remarks>
        public static void UpdateConcurrentEntities(this DbContext context)
        {
            DateTime utcNow = DateTime.UtcNow;

            var changedEntries = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Modified
                         || e.State == EntityState.Deleted);

            foreach (var dbEntry in changedEntries)
            {
                object entity = dbEntry.Entity;

                if ((dbEntry.State & (EntityState.Modified | EntityState.Deleted)) != 0)
                {
                    var concurrencyCheckable = entity as IConcurrencyCheckable;
                    if (concurrencyCheckable != null)
                    {
                        // take row version from entity that modified by client
                        dbEntry.Property("RowVersion").OriginalValue = concurrencyCheckable.RowVersion;
                    }
                    var timestampCheckable = entity as ITimestampCheckable;
                    if (timestampCheckable != null)
                    {
                        // take row version from entity that modified by client
                        dbEntry.Property("RowVersion").OriginalValue = timestampCheckable.RowVersion;
                    }
                }
            }
        }

        /// <summary>
        /// Save changes regardless of <see cref="DbUpdateConcurrencyException"/>.
        /// http://msdn.microsoft.com/en-us/data/jj592904.aspx
        /// </summary>
        public static void SaveChangesIgnoreConcurrency(
            this DbContext context, int retryCount = 3)
        {
            int errorCount = 0;
            for (;;)
            {
                try
                {
                    context.SaveChanges();
                    break;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (++errorCount > retryCount)
                    {
                        throw;
                    }
                    // update original values from the database 
                    EntityEntry entry = ex.Entries.Single();
                    entry.OriginalValues.SetValues(entry.GetDatabaseValues());

                    UpdateRowVersionFromDb(entry);
                }
            };
        }

        /// <summary>
        /// Save changes regardless of <see cref="DbUpdateConcurrencyException"/>.
        /// http://msdn.microsoft.com/en-us/data/jj592904.aspx
        /// </summary>
        public static async Task SaveChangesIgnoreConcurrencyAsync(
            this DbContext context, int retryCount = 3)
        {
            int errorCount = 0;
            for (;;)
            {
                try
                {
                    await context.SaveChangesAsync();
                    break;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (++errorCount > retryCount)
                    {
                        throw;
                    }
                    // update original values from the database 
                    EntityEntry entry = ex.Entries.Single();
                    entry.OriginalValues.SetValues(await entry.GetDatabaseValuesAsync());

                    UpdateRowVersionFromDb(entry);
                }
            };
        }

        private static void UpdateRowVersionFromDb(EntityEntry entry)
        {
            var concurrencyCheckable = entry.Entity as IConcurrencyCheckable;
            if (concurrencyCheckable != null)
            {
                concurrencyCheckable.RowVersion = (long)entry.Property("RowVersion").OriginalValue;
            }
            var timestampCheckable = entry.Entity as ITimestampCheckable;
            if (timestampCheckable != null)
            {
                timestampCheckable.RowVersion = (byte[])entry.Property("RowVersion").OriginalValue;
            }
        }
    }
}
