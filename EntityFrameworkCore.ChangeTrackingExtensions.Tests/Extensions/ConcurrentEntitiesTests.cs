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
            using (var context = CreateTestDbContext())
            {
                // insert
                var post = new Post { Title = "first" };
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
        
        [TestMethod]
        public void TestSaveChangesIgnoreConcurrency()
        {
            Guid rowVersionFromClient = Guid.NewGuid();

            using (var context = CreateTestDbContext())
            {
                // insert
                var post = new Post { Title = "first" };
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

            using (var context = CreateTestDbContext())
            {
                // insert
                var post = new Post { Title = "first" };
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
