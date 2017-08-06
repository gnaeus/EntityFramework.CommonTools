#if EF_CORE
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.ChangeTrackingExtensions
#else
using System;
using System.Threading.Tasks;
using System.Data.Entity;

namespace EntityFramework.ChangeTrackingExtensions
#endif
{
    public static partial class DbContextExtensions
    {
        /// <summary>
        /// Execute <paramref name="method"/> in existing transaction or create and use new transaction.
        /// </summary>
        internal static T ExecuteInTransaction<T>(this DbContext context, Func<T> method)
        {
            var currentTransaction = context.Database.CurrentTransaction;
            var transaction = currentTransaction ?? context.Database.BeginTransaction();

            try
            {
                T result = method.Invoke();
                if (transaction != currentTransaction)
                {
                    transaction.Commit();
                }
                return result;
            }
            catch
            {
                if (transaction != currentTransaction)
                {
                    transaction.Rollback();
                }
                throw;
            }
            finally
            {
                if (transaction != currentTransaction)
                {
                    transaction.Dispose();
                }
            }
        }
        
        /// <summary>
        /// Execute <paramref name="asyncMethod"/> in existing transaction or create and use new transaction.
        /// </summary>
        internal static async Task<T> ExecuteInTransaction<T>(this DbContext context, Func<Task<T>> asyncMethod)
        {
            var currentTransaction = context.Database.CurrentTransaction;
            var transaction = currentTransaction ?? context.Database.BeginTransaction();

            try
            {
                T result = await asyncMethod.Invoke();
                if (transaction != currentTransaction)
                {
                    transaction.Commit();
                }
                return result;
            }
            catch
            {
                if (transaction != currentTransaction)
                {
                    transaction.Rollback();
                }
                throw;
            }
            finally
            {
                if (transaction != currentTransaction)
                {
                    transaction.Dispose();
                }
            }
        }
    }
}
