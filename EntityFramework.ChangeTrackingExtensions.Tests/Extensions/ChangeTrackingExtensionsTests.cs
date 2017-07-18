using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EntityFramework.ChangeTrackingExtensions.Tests
{
    [TestClass]
    public class ChangeTrackingExtensionsTests : TestInitializer
    {
        [TestMethod]
        public void ShouldNotDetectChangesInsideUsingBlock()
        {
            using (var context = CreateTestDbContext())
            {
                var user = new User();
                context.Users.Add(user);
                context.SaveChanges();

                using (context.WithoutChangeTracking())
                {
                    user.Login = "admin";

                    var changedEntries = context.ChangeTracker.Entries()
                        .Where(e => e.State == EntityState.Modified)
                        .ToArray();

                    Assert.AreEqual(0, changedEntries.Length);
                }
            }
        }

        [TestMethod]
        public void ShouldDetectChangesOutsideOfUsingBlock()
        {
            using (var context = CreateTestDbContext())
            {
                var user = new User();
                context.Users.Add(user);
                context.SaveChanges();

                using (context.WithoutChangeTracking())
                {
                    user.Login = "admin";
                }

                var changedEntries = context.ChangeTracker.Entries()
                    .Where(e => e.State == EntityState.Modified)
                    .ToArray();

                Assert.AreEqual(1, changedEntries.Length);
            }
        }

        [TestMethod]
        public void ShouldDetectChangesOnceInsideUsingBlock()
        {
            using (var context = CreateTestDbContext())
            {
                var first = new User();
                var second = new User();
                context.Users.Add(first);
                context.Users.Add(second);
                context.SaveChanges();

                first.Login = "first";

                using (context.WithChangeTrackingOnce())
                {
                    second.Login = "second";

                    var changedEntries = context.ChangeTracker.Entries()
                        .Where(e => e.State == EntityState.Modified)
                        .ToArray();

                    Assert.AreEqual(1, changedEntries.Length);
                }
            }
        }

        [TestMethod]
        public void ShouldDetectChangesAnyTimesOutsideOfUsingBlock()
        {
            using (var context = CreateTestDbContext())
            {
                var first = new User();
                var second = new User();
                context.Users.Add(first);
                context.Users.Add(second);
                context.SaveChanges();

                first.Login = "first";

                using (context.WithChangeTrackingOnce())
                {
                    second.Login = "second";
                }

                var changedEntries = context.ChangeTracker.Entries()
                        .Where(e => e.State == EntityState.Modified)
                        .ToArray();

                Assert.AreEqual(2, changedEntries.Length);
            }
        }
    }
}
