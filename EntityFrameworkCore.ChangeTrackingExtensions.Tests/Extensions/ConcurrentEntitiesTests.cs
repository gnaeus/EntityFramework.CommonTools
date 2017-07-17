#if EF_CORE
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EntityFrameworkCore.ChangeTrackingExtensions.Tests
#else
using System;
using System.Data.Entity.Infrastructure;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EntityFramework.ChangeTrackingExtensions.Tests
#endif
{
    [TestClass]
    public partial class ConcurrentEntitiesTests : TestInitializer
    {
        [TestMethod, ExpectedException(typeof(DbUpdateConcurrencyException))]
        public void TestConcurrencyCheckableGuidEntities()
        {
            using (var context = CreateSqliteDbContext())
            {
                var user = new User();
                context.Users.Add(user);

                // insert
                var post = new Post { Title = "first", Author = user };
                context.Posts.Add(post);

                context.SaveChanges();
                context.Entry(post).Reload();
                Assert.AreEqual(default(Guid), post.RowVersion);

                // update
                Guid oldRowVersion = post.RowVersion;
                post.Title = "second";

                try
                {
                    context.SaveChanges();
                    context.Entry(post).Reload();
                    Assert.AreNotEqual(oldRowVersion, post.RowVersion);
                }
                catch (DbUpdateConcurrencyException)
                {
                    Assert.Fail();
                }

                // concurrency error
                post.Title = "third";
                post.RowVersion = oldRowVersion;

                context.SaveChanges();
            }
        }

        [TestMethod, ExpectedException(typeof(DbUpdateConcurrencyException))]
        public void TestConcurrencyCheckableLongEntities()
        {
            using (var context = CreateSqliteDbContext())
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

        [TestMethod]
        public void TestSaveChangesIgnoreConcurrency()
        {
            Guid rowVersionFromClient = Guid.NewGuid();

            using (var context = CreateSqliteDbContext())
            {
                var user = new User();
                context.Users.Add(user);

                // insert
                var post = new Post { Title = "first", Author = user };
                context.Posts.Add(post);

                context.SaveChanges();
                
                // update
                post.RowVersion = rowVersionFromClient;
                post.Title = "second";

                context.SaveChangesIgnoreConcurrency();

                context.Entry(post).Reload();

                Assert.AreEqual("second", post.Title);
                Assert.AreNotEqual(rowVersionFromClient, post.RowVersion);
            }
        }

        [TestMethod]
        public async Task TestSaveChangesIgnoreConcurrencyAsync()
        {
            Guid rowVersionFromClient = Guid.NewGuid();

            using (var context = CreateSqliteDbContext())
            {
                var user = new User();
                context.Users.Add(user);

                // insert
                var post = new Post { Title = "first", Author = user };
                context.Posts.Add(post);

                await context.SaveChangesAsync();

                // update
                post.RowVersion = rowVersionFromClient;
                post.Title = "second";

                await context.SaveChangesIgnoreConcurrencyAsync();

                context.Entry(post).Reload();

                Assert.AreEqual("second", post.Title);
                Assert.AreNotEqual(rowVersionFromClient, post.RowVersion);
            }
        }
    }
}
