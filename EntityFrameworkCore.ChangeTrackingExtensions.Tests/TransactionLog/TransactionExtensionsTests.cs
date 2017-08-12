using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if EF_CORE
namespace EntityFrameworkCore.ChangeTrackingExtensions.Tests
#elif EF_6
namespace EntityFramework.ChangeTrackingExtensions.Tests
#endif
{
    [TestClass]
    public class TransactionExtensionsTests : TestInitializer
    {
        [TestMethod]
        public void ShouldCreateNewTransaction()
        {
            using (var context = CreateSqliteDbContext())
            {
                Assert.IsNull(context.Database.CurrentTransaction);

                int methodCalls = 0;

                context.ExecuteInTransaction(() =>
                {
                    methodCalls++;

                    Assert.IsNotNull(context.Database.CurrentTransaction);

                    return 0;
                });

                Assert.IsNull(context.Database.CurrentTransaction);
                Assert.AreEqual(1, methodCalls);
            }
        }

        [TestMethod]
        public async Task ShouldCreateNewTransactionAsync()
        {
            using (var context = CreateSqliteDbContext())
            {
                Assert.IsNull(context.Database.CurrentTransaction);

                int methodCalls = 0;

                await context.ExecuteInTransaction(async () =>
                {
                    methodCalls++;

                    Assert.IsNotNull(context.Database.CurrentTransaction);

                    await Task.Delay(1);

                    return 0;
                });

                Assert.IsNull(context.Database.CurrentTransaction);
                Assert.AreEqual(1, methodCalls);
            }
        }

        [TestMethod]
        public void ShouldPreserveExistingTransaction()
        {
            using (var context = CreateSqliteDbContext())
            using (var transaction = context.Database.BeginTransaction())
            {
                int methodCalls = 0;

                context.ExecuteInTransaction(() =>
                {
                    methodCalls++;

                    Assert.AreEqual(transaction, context.Database.CurrentTransaction);

                    return 0;
                });

                Assert.AreEqual(transaction, context.Database.CurrentTransaction);
                Assert.AreEqual(1, methodCalls);
            }
        }

        [TestMethod]
        public async Task ShouldPreserveExistingTransactionAsync()
        {
            using (var context = CreateSqliteDbContext())
            using (var transaction = context.Database.BeginTransaction())
            {
                int methodCalls = 0;

                await context.ExecuteInTransaction(async () =>
                {
                    methodCalls++;

                    Assert.AreEqual(transaction, context.Database.CurrentTransaction);

                    await Task.Delay(1);

                    return 0;
                });

                Assert.AreEqual(transaction, context.Database.CurrentTransaction);
                Assert.AreEqual(1, methodCalls);
            }
        }
    }
}