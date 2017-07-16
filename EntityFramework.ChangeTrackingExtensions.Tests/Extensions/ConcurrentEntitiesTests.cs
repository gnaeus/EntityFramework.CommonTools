using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EntityFramework.ChangeTrackingExtensions.Tests
{
    [TestClass]
    public class ConcurrentEntitiesTests : TestInitializer
    {
        [TestMethod]
        public void Test()
        {
            using (var context = new TestDbContext(_connection))
            {
                context.SaveChanges();
            }
        }
    }
}
