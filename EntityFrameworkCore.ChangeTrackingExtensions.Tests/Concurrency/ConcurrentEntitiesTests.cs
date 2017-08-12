using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if EF_CORE
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.ChangeTrackingExtensions.Tests
#elif EF_6
using System.Data.Entity.Infrastructure;

namespace EntityFramework.ChangeTrackingExtensions.Tests
#endif
{
    [TestClass]
    public class ConcurrentEntitiesTests : TestInitializer
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

                // RowVersion is changed by client code
                post.Title = "third";
                post.RowVersion = oldRowVersion;

                // should throw DbUpdateConcurrencyException
                context.SaveChanges();
            }
        }

        [TestMethod, ExpectedException(typeof(DbUpdateConcurrencyException))]
        public void TestConcurrencyCheckableLongEntities()
        {
            using (var context = CreateSqliteDbContext())
            {
                // insert
                var settings = new Settings { Key = "first", Value = "first" };
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

                // RowVersion is changed by client code
                settings.Value = "third";
                settings.RowVersion = oldRowVersion;

                // should throw DbUpdateConcurrencyException
                context.SaveChanges();
            }
        }

        [TestMethod, ExpectedException(typeof(DbUpdateConcurrencyException))]
        public void TestConcurrencyCheckableDelete()
        {
            using (var context = CreateSqliteDbContext())
            {
                var first = new Role { Name = "first" };
                var second = new Role { Name = "second" };

                try
                {
                    context.Roles.Add(first);
                    context.Roles.Add(second);
                    context.SaveChanges();

                    first.Name = "changed";
                    second.Name = "changed";
                    context.SaveChanges();
                }
                catch (DbUpdateConcurrencyException)
                {
                    Assert.Fail();
                }
            }

            using (var context = CreateSqliteDbContext())
            {
                var role = context.Roles.First();
                
                context.Roles.Remove(role);

                try
                {
                    context.SaveChanges();
                }
                catch (DbUpdateConcurrencyException)
                {
                    Assert.Fail();
                }
            }

            using (var context = CreateSqliteDbContext())
            {
                var role = context.Roles.Single();

                // RowVersion is changed by client code
                role.RowVersion = Guid.NewGuid();

                context.Roles.Remove(role);

                // should throw DbUpdateConcurrencyException
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
