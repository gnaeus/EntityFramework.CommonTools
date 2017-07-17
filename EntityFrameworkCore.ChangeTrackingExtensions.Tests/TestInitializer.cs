using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EntityFrameworkCore.ChangeTrackingExtensions.Tests
{
    public abstract class TestInitializer
    {
        public TestContext TestContext { get; set; }
        
        protected string GetDbName()
        {
            return $"{TestContext.FullyQualifiedTestClassName}.{TestContext.TestName}";
        }

        [TestInitialize]
        public void TestInitialize()
        {
            using (var context = CreateTestDbContext())
            {
                context.Database.EnsureCreated();
            }
        }
        
        protected TestDbContext CreateTestDbContext()
        {
            return new TestDbContext(GetDbName());
        }

        [TestCleanup]
        public void TestCleanup()
        {
            using (var context = CreateTestDbContext())
            {
                context.Database.EnsureDeleted();
            }
        }
    }
}
