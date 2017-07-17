using System.Data.Entity.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EntityFramework.ChangeTrackingExtensions.Tests
{
    public partial class ConcurrentEntitiesTests : TestInitializer
    {
        [TestMethod, ExpectedException(typeof(DbUpdateConcurrencyException))]
        public void TestConcurrencyCheckableLongEntities()
        {
            using (var context = CreateTestDbContext())
            {
                // insert
                var settings = new Settings { Key = "first", Value = "test" };
                context.Settings.Add(settings);

                context.SaveChanges();
                context.Entry(settings).Reload();
                Assert.AreEqual(default(long), settings.RowVersion);

                // update
                long oldRowVersion = settings.RowVersion;
                settings.Value = "second";

                try
                {
                    context.SaveChanges();
                    context.Entry(settings).Reload();
                    Assert.AreNotEqual(oldRowVersion, settings.RowVersion);
                }
                catch (DbUpdateConcurrencyException)
                {
                    Assert.Fail();
                }

                // concurrency error
                settings.Value = "third";
                settings.RowVersion = oldRowVersion;

                context.SaveChanges();
            }
        }
    }
}
