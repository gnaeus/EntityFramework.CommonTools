using System;
using System.Threading;
using System.Threading.Tasks;

#if EF_CORE
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.CommonTools
#elif EF_6
using System.Data.Entity;
using ModelBuilder = System.Data.Entity.DbModelBuilder;

namespace EntityFramework.CommonTools
#endif
{
    public static partial class DbContextExtensions
    {
        /// <summary>
        /// Wrapper for <see cref="DbContext.SaveChanges()"/> that saves <see cref="TransactionLog"/> to DB.
        /// </summary>
        public static int SaveChangesWithTransactionLog(
#if EF_CORE
            this DbContext dbContext, Func<bool, int> baseSaveChanges, bool acceptAllChangesOnSuccess = true)
#elif EF_6
            this DbContext dbContext, Func<int> baseSaveChanges)
#endif
        {
            return dbContext.ExecuteInTransaction(() =>
            {
                var logContext = new TransactionLogContext(dbContext);
#if EF_CORE
                // save main entities
                int count = baseSaveChanges.Invoke(acceptAllChangesOnSuccess);
#elif EF_6
                // save main entities
                int count = baseSaveChanges.Invoke();
#endif
                logContext.AddTransactionLogEntities();
#if EF_CORE
                // save TransactionLog entities
                baseSaveChanges.Invoke(acceptAllChangesOnSuccess);
#elif EF_6
                // save TransactionLog entities
                baseSaveChanges.Invoke();
#endif
                return count;
            });
        }

        /// <summary>
        /// Wrapper for <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>
        /// that saves <see cref="TransactionLog"/> to DB.
        /// </summary>
        public static Task<int> SaveChangesWithTransactionLogAsync(
#if EF_CORE
            this DbContext dbContext,
            Func<bool, CancellationToken, Task<int>> baseSaveChangesAsync,
            bool acceptAllChangesOnSuccess = true,
            CancellationToken cancellationToken = default(CancellationToken))
#elif EF_6
            this DbContext dbContext,
            Func<CancellationToken, Task<int>> baseSaveChangesAsync,
            CancellationToken cancellationToken = default(CancellationToken))
#endif
        {
            return dbContext.ExecuteInTransaction(async () =>
            {
                var logContext = new TransactionLogContext(dbContext);
#if EF_CORE
                // save main entities
                int count = await baseSaveChangesAsync.Invoke(acceptAllChangesOnSuccess, cancellationToken);
#elif EF_6
                // save main entities
                int count = await baseSaveChangesAsync.Invoke(cancellationToken);
#endif
                logContext.AddTransactionLogEntities();
#if EF_CORE
                // save TransactionLog entities
                await baseSaveChangesAsync.Invoke(acceptAllChangesOnSuccess, cancellationToken);
#elif EF_6
                // save TransactionLog entities
                await baseSaveChangesAsync.Invoke(cancellationToken);
#endif
                return count;
            });
        }
    }

    public static partial class ModelBuilderExtensions
    {
        /// <summary>
        /// Register <see cref="TransactionLog"/> table in <see cref="DbContext"/>.
        /// </summary>
        public static ModelBuilder UseTransactionLog(
            this ModelBuilder modelBuilder,
            string tableName = "_TransactionLog",
            string schemaName = null)
        {
            var transactionLog = modelBuilder.Entity<TransactionLog>();

            if (schemaName == null)
            {
                transactionLog.ToTable(tableName);
            }
            else
            {
                transactionLog.ToTable(tableName, schemaName);
            }

            transactionLog
                .HasKey(e => e.Id);

            transactionLog
                .Property(e => e.Operation)
                .IsRequired()
                .HasMaxLength(3);

            transactionLog
                .Property(e => e.TableName)
                .IsRequired();

            transactionLog
                .Property(e => e.EntityType)
                .IsRequired();

            transactionLog
                .Property(e => e.EntityJson)
                .IsRequired();

            return modelBuilder;
        }
    }
}
