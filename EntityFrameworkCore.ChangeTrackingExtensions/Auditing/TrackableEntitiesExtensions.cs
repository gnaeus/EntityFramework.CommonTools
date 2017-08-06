#if EF_CORE
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EntityFrameworkCore.ChangeTrackingExtensions
#else
using System;
using System.Linq;
using System.Data.Entity;
using EntityEntry = System.Data.Entity.Infrastructure.DbEntityEntry;

namespace EntityFramework.ChangeTrackingExtensions
#endif
{
    public static partial class DbContextExtensions
    {
        /// <summary>
        /// Populate special properties for all Trackable Entities in context.
        /// </summary>
        public static void UpdateTrackableEntities(this DbContext context)
        {
            DateTime utcNow = DateTime.UtcNow;

            var changedEntries = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added
                         || e.State == EntityState.Modified
                         || e.State == EntityState.Deleted);

            foreach (var dbEntry in changedEntries)
            {
                UpdateTrackableEntity(dbEntry, utcNow);
            }
        }
        
        private static void UpdateTrackableEntity(EntityEntry dbEntry, DateTime utcNow)
        {
            object entity = dbEntry.Entity;

            switch (dbEntry.State)
            {
                case EntityState.Added:
                    var creationTrackable = entity as ICreationTrackable;
                    if (creationTrackable != null)
                    {
                        creationTrackable.CreatedUtc = utcNow;
                    }
                    break;

                case EntityState.Modified:
                    var modificatonTrackable = entity as IModificationTrackable;
                    if (modificatonTrackable != null)
                    {
                        modificatonTrackable.UpdatedUtc = utcNow;
                        dbEntry.CurrentValues[nameof(IModificationTrackable.UpdatedUtc)] = utcNow;
                    }
                    break;

                case EntityState.Deleted:
                    var softDeletable = entity as ISoftDeletable;
                    if (softDeletable != null)
                    {
                        dbEntry.State = EntityState.Unchanged;

                        softDeletable.IsDeleted = true;
                        dbEntry.CurrentValues[nameof(ISoftDeletable.IsDeleted)] = true;

                        var deletionTrackable = entity as IDeletionTrackable;
                        if (deletionTrackable != null)
                        {
                            deletionTrackable.DeletedUtc = utcNow;
                            dbEntry.CurrentValues[nameof(IDeletionTrackable.DeletedUtc)] = utcNow;
                        }
                    }
                    break;

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
