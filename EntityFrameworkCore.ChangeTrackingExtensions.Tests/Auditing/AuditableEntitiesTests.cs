using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if EF_CORE
namespace EntityFrameworkCore.ChangeTrackingExtensions.Tests
#elif EF_6
namespace EntityFramework.ChangeTrackingExtensions.Tests
#endif
{
    [TestClass]
    public class AuditableEntitiesTests : TestInitializer
    {
        [TestMethod]
        public void TestAuditableEntitiesGeneric()
        {
            using (var context = CreateInMemoryDbContext())
            {
                var author = new User();
                context.Users.Add(author);

                // insert
                var post = new Post { Title = "first", Author = author };
                context.Posts.Add(post);

                context.SaveChanges(author.Id);

                context.Entry(post).Reload();
                Assert.AreEqual(DateTime.UtcNow.Date, post.CreatedUtc.Date);
                Assert.AreEqual(author.Id, post.CreatorUserId);

                // update
                post.Title = "second";

                context.SaveChanges(author.Id);

                context.Entry(post).Reload();
                Assert.IsNotNull(post.UpdatedUtc);
                Assert.AreEqual(DateTime.UtcNow.Date, post.UpdatedUtc?.Date);
                Assert.AreEqual(author.Id, post.UpdaterUserId);

                // delete
                context.Posts.Remove(post);

                context.SaveChanges(author.Id);

                context.Entry(post).Reload();
                Assert.AreEqual(true, post.IsDeleted);
                Assert.IsNotNull(post.DeletedUtc);
                Assert.AreEqual(DateTime.UtcNow.Date, post.DeletedUtc?.Date);
                Assert.AreEqual(author.Id, post.DeleterUserId);
            }
        }

        [TestMethod]
        public async Task TestAuditableEntities()
        {
            using (var context = CreateSqliteDbContext())
            {
                // insert
                var settings = new Settings { Key = "first", Value = "first" };
                context.Settings.Add(settings);

                await context.SaveChangesAsync("admin");

                context.Entry(settings).Reload();
                Assert.AreEqual(DateTime.UtcNow.Date, settings.CreatedUtc.Date);
                Assert.AreEqual("admin", settings.CreatorUser);

                // update
                settings.Value = "second";

                await context.SaveChangesAsync("admin");

                context.Entry(settings).Reload();
                Assert.IsNotNull(settings.UpdatedUtc);
                Assert.AreEqual(DateTime.UtcNow.Date, settings.UpdatedUtc?.Date);
                Assert.AreEqual("admin", settings.UpdaterUser);

                // delete
                context.Settings.Remove(settings);

                await context.SaveChangesAsync("admin");

                context.Entry(settings).Reload();
                Assert.AreEqual(true, settings.IsDeleted);
                Assert.IsNotNull(settings.DeletedUtc);
                Assert.AreEqual(DateTime.UtcNow.Date, settings.DeletedUtc?.Date);
                Assert.AreEqual("admin", settings.DeleterUser);
            }
        }
    }
}