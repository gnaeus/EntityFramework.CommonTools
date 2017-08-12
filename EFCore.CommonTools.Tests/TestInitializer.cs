using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EntityFrameworkCore.CommonTools.Tests
{
    public abstract class TestInitializer
    {
        private SqliteConnection _connection;

        public TestContext TestContext { get; set; }
        
        protected string GetInMemoryDbName()
        {
            return $"{TestContext.FullyQualifiedTestClassName}.{TestContext.TestName}";
        }

        protected TestDbContext CreateInMemoryDbContext()
        {
            return new TestDbContext(GetInMemoryDbName());
        }

        protected TestDbContext CreateSqliteDbContext()
        {
            return new TestDbContext(_connection);
        }

        [TestInitialize]
        public void TestInitialize()
        {
            _connection = new SqliteConnection("data source=:memory:");

            _connection.Open();
        }
        
        [TestCleanup]
        public void TestCleanup()
        {
            _connection.Close();

            using (var context = CreateInMemoryDbContext())
            {
                context.Database.EnsureDeleted();
            }
        }
    }
}
