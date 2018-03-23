using System;
using System.Collections.Generic;
using System.Linq;
using Jil;

#if EF_CORE
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EntityFrameworkCore.CommonTools
#elif EF_6
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using EntityEntry = System.Data.Entity.Infrastructure.DbEntityEntry;

namespace EntityFramework.CommonTools
#endif
{
    /// <summary>
    /// Utility for capturing transaction logs from <see cref="DbContext.SaveChanges()"/>.
    /// Tracked entities must implement <see cref="ITransactionLoggable"/> interface.
    /// </summary>
    internal class TransactionLogContext
    {
        private readonly DbContext _context;
        private readonly Guid _transactionId = Guid.NewGuid();
        private readonly DateTime _createdUtc = DateTime.UtcNow;

        private readonly List<EntityEntry> _insertedEntries = new List<EntityEntry>();
        private readonly List<EntityEntry> _updatedEntries = new List<EntityEntry>();
        private readonly List<TransactionLog> _deletedLogs = new List<TransactionLog>();
        
        public TransactionLogContext(DbContext context)
        {
            _context = context;

            StoreChangedEntries();
        }
        
        private void StoreChangedEntries()
        {
            var changedEntries = _context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added
                         || e.State == EntityState.Modified
                         || e.State == EntityState.Deleted);

            foreach (var entry in changedEntries)
            {
                if (entry.Entity is ITransactionLoggable)
                {
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            _insertedEntries.Add(entry);
                            break;

                        case EntityState.Modified:
                            _updatedEntries.Add(entry);
                            break;

                        case EntityState.Deleted:
                            _deletedLogs.Add(CreateTransactionLog(entry, TransactionLog.DELETE));
                            break;
                    }
                }
            }
        }

        public void AddTransactionLogEntities()
        {
            foreach (TransactionLog transactionLog in CreateTransactionLogs())
            {
                _context.Entry(transactionLog).State = EntityState.Added;
            }
        }

        private IEnumerable<TransactionLog> CreateTransactionLogs()
        {
            foreach (EntityEntry insertedEntry in _insertedEntries)
            {
                yield return CreateTransactionLog(insertedEntry, TransactionLog.INSERT);
            }
            foreach (EntityEntry updateEntry in _updatedEntries)
            {
                yield return CreateTransactionLog(updateEntry, TransactionLog.UPDATE);
            }
            foreach (TransactionLog deletedLog in _deletedLogs)
            {
                yield return deletedLog;
            }
        }

        private TransactionLog CreateTransactionLog(EntityEntry entry, string operation)
        {
            object entity = entry.Entity;

            Type entityType = entity.GetType();
#if EF_CORE
            var tableAndSchema = entry.Metadata.Relational();
#elif EF_6
            if (_context.Configuration.ProxyCreationEnabled)
            {
                entityType = ObjectContext.GetObjectType(entityType);
            }

            var tableAndSchema = _context.GetTableAndSchemaName(entityType);
#endif
            var log = new TransactionLog
            {
                TransactionId = _transactionId,
                CreatedUtc = _createdUtc,
                Operation = operation,
                Schema = tableAndSchema.Schema,
                TableName = tableAndSchema.TableName,
                EntityType = GetTypeName(entityType),
            };

            if (operation == TransactionLog.DELETE)
            {
#if EF_CORE
                var primaryKey = entry.Metadata
                    .FindPrimaryKey()
                    .Properties
                    .Select(p => entry.Property(p.Name))
                    .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
#elif EF_6
                var primaryKey = ((IObjectContextAdapter)_context)
                    .ObjectContext.ObjectStateManager
                    .GetObjectStateEntry(entity)
                    .EntityKey.EntityKeyValues
                    .ToDictionary(k => k.Key, k => k.Value);
#endif
                log.EntityJson = JSON.SerializeDynamic(primaryKey);
            }
            else
            {
                log.EntityJson = JSON.SerializeDynamic(
                    entry.CurrentValues.ToObject(), Options.IncludeInherited);
            }

            return log;
        }

        private string GetTypeName(Type type)
        {
            string name = type.AssemblyQualifiedName;

            return name.Substring(0, name.IndexOf(", Version="));
        }
    }
}
