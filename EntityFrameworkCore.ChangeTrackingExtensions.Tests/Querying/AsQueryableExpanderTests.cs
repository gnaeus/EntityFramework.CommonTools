#if EF_CORE
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EntityFrameworkCore.ChangeTrackingExtensions.Tests
#else
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EntityFramework.ChangeTrackingExtensions.Tests
#endif
{
    [TestClass]
    public class AsQueryableExpanderTests
    {
        [TestMethod]
        public void ShouldExpandAsQueryable()
        {
            var query = Enumerable.Empty<User>()
                .AsQueryable()
                .AsVisitable(new AsQueryableExpander())
                .SelectMany(u => u.Posts
                    .AsQueryable()
                    .Where(p => !p.IsDeleted)
                    .SelectMany(p => p.Author.Posts
                        .Where(ap => ap.Title == "test")
                        .AsQueryable()
                        .Select(ap => ap.Author)));

            var expected = Enumerable.Empty<User>()
                .AsQueryable()
                .AsVisitable(new AsQueryableExpander())
                .SelectMany(u => u.Posts
                    .Where(p => !p.IsDeleted)
                    .SelectMany(p => p.Author.Posts
                        .Where(ap => ap.Title == "test")
                        .Select(ap => ap.Author)));

            Assert.AreNotSame(expected.Expression, query.Expression);

            Assert.That.MethodCallsAreMatch(expected.Expression, query.Expression);
        }
    }
}
