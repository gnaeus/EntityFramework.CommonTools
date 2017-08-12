using System;
using System.Linq;

#if EF_CORE
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EntityFrameworkCore.ChangeTrackingExtensions
#elif EF_6
using System.Data.Entity;
using EntityEntry = System.Data.Entity.Infrastructure.DbEntityEntry;

namespace EntityFramework.ChangeTrackingExtensions
#endif
{
    public static partial class DbContextExtensions
    {
        /// <summary>
        /// Populate special properties for all Auditable Entities in context.
        /// </summary>
        public static void UpdateAuditableEntities<TUserId>(this DbContext context, TUserId editorUserId)
            where TUserId : struct
        {
            DateTime utcNow = DateTime.UtcNow;

            var changedEntries = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added
                         || e.State == EntityState.Modified
                         || e.State == EntityState.Deleted);

            foreach (var dbEntry in changedEntries)
            {
                UpdateAuditableEntity(dbEntry, utcNow, editorUserId);
            }
        }

        /// <summary>
        /// Populate special properties for all Auditable Entities in context.
        /// </summary>
        public static void UpdateAuditableEntities(this DbContext context, string editorUser)
        {
            DateTime utcNow = DateTime.UtcNow;

            var changedEntries = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added
                         || e.State == EntityState.Modified
                         || e.State == EntityState.Deleted);

            foreach (var dbEntry in changedEntries)
            {
                UpdateAuditableEntity(dbEntry, utcNow, editorUser);
            }
        }
        
        private static void UpdateAuditableEntity<TUserId>(
            EntityEntry dbEntry, DateTime utcNow, TUserId editorUserId)
            where TUserId : struct
        {
            object entity = dbEntry.Entity;

            switch (dbEntry.State)
            {
                case EntityState.Added:
                    var creationAuditable = entity as ICreationAuditable<TUserId>;
                    if (creationAuditable != null)
                    {
                        UpdateTrackableEntity(dbEntry, utcNow);

                        creationAuditable.CreatorUserId = editorUserId;
                    }
                    break;

                case EntityState.Modified:
                    var modificationAuditable = entity as IModificationAuditable<TUserId>;
                    if (modificationAuditable != null)
                    {
                        UpdateTrackableEntity(dbEntry, utcNow);

                        modificationAuditable.UpdaterUserId = editorUserId;
                        dbEntry.CurrentValues[nameof(IModificationAuditable<TUserId>.UpdaterUserId)] = editorUserId;
                    }
                    break;

                case EntityState.Deleted:
                    var deletionAuditable = entity as IDeletionAuditable<TUserId>;
                    if (deletionAuditable != null)
                    {
                        UpdateTrackableEntity(dbEntry, utcNow);
                        
                        // change CurrentValues after dbEntry.State becomes EntityState.Unchanged
                        deletionAuditable.DeleterUserId = editorUserId;
                        dbEntry.CurrentValues[nameof(IDeletionAuditable<TUserId>.DeleterUserId)] = editorUserId;
                    }
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        private static void UpdateAuditableEntity(
            EntityEntry dbEntry, DateTime utcNow, string editorUser)
        {
            object entity = dbEntry.Entity;

            switch (dbEntry.State)
            {
                case EntityState.Added:
                    var creationAuditable = entity as ICreationAuditable;
                    if (creationAuditable != null)
                    {
                        UpdateTrackableEntity(dbEntry, utcNow);

                        creationAuditable.CreatorUser = editorUser;
                    }
                    break;

                case EntityState.Modified:
                    var modificationAuditable = entity as IModificationAuditable;
                    if (modificationAuditable != null)
                    {
                        UpdateTrackableEntity(dbEntry, utcNow);

                        modificationAuditable.UpdaterUser = editorUser;
                        dbEntry.CurrentValues[nameof(IModificationAuditable.UpdaterUser)] = editorUser;
                    }
                    break;

                case EntityState.Deleted:
                    var deletionAuditable = entity as IDeletionAuditable;
                    if (deletionAuditable != null)
                    {
                        UpdateTrackableEntity(dbEntry, utcNow);

                        // change CurrentValues after dbEntry.State becomes EntityState.Unchanged
                        deletionAuditable.DeleterUser = editorUser;
                        dbEntry.CurrentValues[nameof(IDeletionAuditable.DeleterUser)] = editorUser;
                    }
                    break;

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
