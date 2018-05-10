using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if EF_CORE
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.CommonTools.Tests
#elif EF_6
using System.Data.Entity;

namespace EntityFramework.CommonTools.Tests
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
                var user = new User();
                context.Users.Add(user);
                context.SaveChanges();

                // insert
                var post = new Post { Title = "first" };
                context.Posts.Add(post);
                context.SaveChanges(user.Id);

                context.Entry(post).Reload();
                Assert.AreEqual(DateTime.UtcNow.Date, post.CreatedUtc.ToUniversalTime().Date);
                Assert.AreEqual(user.Id, post.CreatorUserId);

                // update
                post.Title = "second";

                context.SaveChanges(user.Id);

                context.Entry(post).Reload();
                Assert.IsNotNull(post.UpdatedUtc);
                Assert.AreEqual(DateTime.UtcNow.Date, post.UpdatedUtc?.ToUniversalTime().Date);
                Assert.AreEqual(user.Id, post.UpdaterUserId);

                // delete
                context.Posts.Remove(post);

                context.SaveChanges(user.Id);

                context.Entry(post).Reload();
                Assert.AreEqual(true, post.IsDeleted);
                Assert.IsNotNull(post.DeletedUtc);
                Assert.AreEqual(DateTime.UtcNow.Date, post.DeletedUtc?.ToUniversalTime().Date);
                Assert.AreEqual(user.Id, post.DeleterUserId);
            }
        }

        [TestMethod]
        public async Task TestAuditableEntitiesString()
        {
            using (var context = CreateSqliteDbContext())
            {
                // insert
                var settings = new Settings { Key = "first", Value = "first" };
                context.Settings.Add(settings);

                await context.SaveChangesAsync("admin");

                context.Entry(settings).Reload();
                Assert.AreEqual(DateTime.UtcNow.Date, settings.CreatedUtc.ToUniversalTime().Date);
                Assert.AreEqual("admin", settings.CreatorUserId);

                // update
                settings.Value = "second";

                await context.SaveChangesAsync("admin");

                context.Entry(settings).Reload();
                Assert.IsNotNull(settings.UpdatedUtc);
                Assert.AreEqual(DateTime.UtcNow.Date, settings.UpdatedUtc?.ToUniversalTime().Date);
                Assert.AreEqual("admin", settings.UpdaterUserId);

                // delete
                context.Settings.Remove(settings);

                await context.SaveChangesAsync("admin");

                context.Entry(settings).Reload();
                Assert.AreEqual(true, settings.IsDeleted);
                Assert.IsNotNull(settings.DeletedUtc);
                Assert.AreEqual(DateTime.UtcNow.Date, settings.DeletedUtc?.ToUniversalTime().Date);
                Assert.AreEqual("admin", settings.DeleterUserId);
            }
        }

        // https://github.com/gnaeus/EntityFramework.CommonTools/issues/4
        [TestMethod]
        public void TestAuditableEntitiesUpdateExisting()
        {
            using (var context = CreateSqliteDbContext())
            {
                var firstUser = new User();
                var secondUser = new User();
                context.Users.Add(firstUser);
                context.Users.Add(secondUser);
                context.SaveChanges();

                // insert
                var post = new Post { Title = "first" };
                context.Posts.Add(post);
                context.SaveChanges(firstUser.Id);
                context.Entry(post).Reload();
                DateTime createdUtc = post.CreatedUtc;

                // set empty CreatedUtc and CreatorUserId
                post.CreatedUtc = default(DateTime);
                post.CreatorUserId = default(int);
                post.Title = "second";
#if EF_CORE
                context.Posts.Update(post);
#elif EF_6
                context.Entry(post).State = EntityState.Modified;
#endif
                context.SaveChanges(firstUser.Id);

                // CreatedUtc and CreatorUserId should not be changed
                context.Entry(post).Reload();
                Assert.AreEqual(createdUtc, post.CreatedUtc);
                Assert.AreEqual(firstUser.Id, post.CreatorUserId);

                // explicitely change CreatedUtc and CreatorUserId
                post.CreatedUtc = new DateTime(2018, 01, 01);
                post.CreatorUserId = secondUser.Id;
                post.Title = "third";
#if EF_CORE
                context.Posts.Update(post);
#elif EF_6
                context.Entry(post).State = EntityState.Modified;
#endif
                context.SaveChanges(firstUser.Id);

                // CreatedUtc and CreatorUserId should equals to explicitely passed values
                context.Entry(post).Reload();
                Assert.AreEqual(new DateTime(2018, 01, 01), post.CreatedUtc);
                Assert.AreEqual(secondUser.Id, post.CreatorUserId);
            }
        }

        // https://github.com/gnaeus/EntityFramework.CommonTools/issues/4
        [TestMethod]
        public void TestAuditableEntitiesUpdateDetached()
        {
            using (var context = CreateSqliteDbContext())
            {
                var firstUser = new User();
                var secondUser = new User();
                context.Users.Add(firstUser);
                context.Users.Add(secondUser);
                context.SaveChanges();

                // insert
                var post = new Post { Title = "first" };
                context.Posts.Add(post);
                context.SaveChanges(firstUser.Id);
                context.Entry(post).Reload();
                DateTime createdUtc = post.CreatedUtc;

                // attach modified entity with empty CreatedUtc and CreatorUserId
                context.Entry(post).State = EntityState.Detached;
                post = new Post
                {
                    Id = post.Id,
                    Title = "second",
                    RowVersion = post.RowVersion,
                };
#if EF_CORE
                context.Posts.Update(post);
#elif EF_6
                context.Entry(post).State = EntityState.Modified;
#endif
                context.SaveChanges(firstUser.Id);

                // CreatedUtc and CreatorUserId should not be changed
                context.Entry(post).Reload();
                Assert.AreEqual(createdUtc, post.CreatedUtc);
                Assert.AreEqual(firstUser.Id, post.CreatorUserId);

                // attach modified entity with explicitely set CreatedUtc and CreatorUserId
                context.Entry(post).State = EntityState.Detached;
                post = new Post
                {
                    Id = post.Id,
                    Title = "third",
                    RowVersion = post.RowVersion,
                    CreatedUtc = new DateTime(2018, 01, 01),
                    CreatorUserId = secondUser.Id,
                };
#if EF_CORE
                context.Posts.Update(post);
#elif EF_6
                context.Entry(post).State = EntityState.Modified;
#endif
                context.SaveChanges(firstUser.Id);

                // CreatedUtc and CreatorUserId should equals to explicitely passed values
                context.Entry(post).Reload();
                Assert.AreEqual(new DateTime(2018, 01, 01), post.CreatedUtc);
                Assert.AreEqual(secondUser.Id, post.CreatorUserId);
            }
        }
    }
}
