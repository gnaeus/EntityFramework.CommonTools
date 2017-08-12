using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if EF_CORE
namespace EntityFrameworkCore.ChangeTrackingExtensions.Tests
#else
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
                .Where(u => u.Posts
                    .AsQueryable()
                    .OfType<Post>()
                    .OrderBy(p => p.CreatedUtc)
                    .ThenBy(p => p.UpdatedUtc)
                    .ThenByDescending(p => p.Id)
                    .Select(p => p.Tags
                        .AsQueryable()
                        .Count())
                    .Average() > 10)
                .SelectMany(u => u.Posts
                    .AsQueryable()
                    .Where(p => !p.IsDeleted)
                    .SelectMany(p => p.Author.Posts
                        .Where(ap => ap.Title == "test")
                        .AsQueryable()
                        .Select(ap => ap.Author)));

            var expected = Enumerable.Empty<User>()
                .AsQueryable()
                .Where(u => u.Posts
                    .OfType<Post>()
                    .OrderBy(p => p.CreatedUtc)
                    .ThenBy(p => p.UpdatedUtc)
                    .ThenByDescending(p => p.Id)
                    .Select(p => p.Tags
                        .Count())
                    .Average() > 10)
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
