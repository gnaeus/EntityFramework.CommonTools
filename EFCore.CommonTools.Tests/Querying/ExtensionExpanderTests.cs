using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
    public static class UserQueryableExtensions
    {
        [Expandable]
        public static IQueryable<User> FilterIsActive(this IEnumerable<User> users)
        {
            return users.AsQueryable().Where(u => !u.IsDeleted);
        }

        [Expandable]
        public static IQueryable<User> FilterByLogin(this IEnumerable<User> users, string login)
        {
            return users.AsQueryable().FilterIsActive().Where(u => u.Login == login);
        }
    }

    public static class PostQueryableExtensions
    {
        [Expandable]
        public static IQueryable<Post> FilterIsActive(this IEnumerable<Post> posts)
        {
            return posts.AsQueryable().Where(p => !p.IsDeleted);
        }

        [Expandable]
        public static IQueryable<Post> FilterToday(this IEnumerable<Post> posts, int limit = 10)
        {
            DateTime today = DateTime.UtcNow.Date;

            return posts.AsQueryable().FilterIsActive().Where(p => p.CreatedUtc > today).Take(limit);
        }

        [Expandable]
        public static IQueryable<Post> FilterByEditor(this IEnumerable<Post> posts, int editorId)
        {
            return posts.AsQueryable().Where(p => p.UpdaterUserId == editorId);
        }
    }

    public static class GenericExtensions
    {
        [Expandable]
        public static IQueryable<T> Filter<T>(
            this IEnumerable<T> enumerable, Expression<Func<T, bool>> predicate)
        {
            return enumerable.AsQueryable().Where(predicate);
        }

        [Expandable]
        public static IQueryable<TResult> Map<T, TResult>(
            this IEnumerable<T> enumerable, Expression<Func<T, TResult>> projection)
        {
            return enumerable.AsQueryable().Select(projection);
        }

        [Expandable]
        public static IQueryable<TResult> FlatMap<T, TResult>(
            this IEnumerable<T> enumerable, Expression<Func<T, IEnumerable<TResult>>> projection)
        {
            return enumerable.AsQueryable().SelectMany(projection);
        }
    }

    public static class NestedExtensions
    {
        [Expandable]
        public static IQueryable<Post> HasPostsWithAuthorByLogin(
            this IEnumerable<Post> posts, string login)
        {
            return posts.AsQueryable().Where(p => p.Author.Login == login);
        }

        [Expandable]
        public static IQueryable<Post> SelectPostsThat_HasPostsWithAuthorByLogin(
            this IEnumerable<Post> posts, string login)
        {
            return posts.AsQueryable()
                .SelectMany(p => p.Author.Posts
                    .HasPostsWithAuthorByLogin(login));
        }
    }

    [TestClass]
    public class ExtensionExpanderTests : TestInitializer
    {
        [TestMethod]
        public void ShouldExpandExtensions()
        {
            using (var context = CreateSqliteDbContext())
            {
                context.Users.AddRange(new[]
                {
                    new User { Login = "admin", IsDeleted = false },
                    new User { Login = "admin", IsDeleted = true },
                });

                context.SaveChanges();

                string login = "admin";
                int updaterId = 1;
                DateTime today = DateTime.UtcNow.Date;

                var query = context.Users.AsExpandable()
                    .FilterByLogin(login)
                    .Select(u => u.Posts
                        .FilterByEditor(updaterId)
                        .FilterByEditor(u.Id)
                        .FilterByEditor(u.Id + 1)
                        .FilterToday(5)
                        .Count());

                var expected = context.Users
                    .Where(u => !u.IsDeleted)
                    .Where(u => u.Login == login)
                    .Select(u => u.Posts
#if !EF_CORE
                        .AsQueryable()
#endif
                        .Where(p => p.UpdaterUserId == updaterId)
                        .Where(p => p.UpdaterUserId == u.Id)
                        .Where(p => p.UpdaterUserId == u.Id + 1)
                        .Where(p => !p.IsDeleted)
                        .Where(p => p.CreatedUtc > today)
                        .Take(5)
                        .Count());

                Assert.AreEqual(expected.ToString(), query.ToString());

                Assert.AreNotSame(expected.Expression, query.Expression);

                Assert.That.MethodCallsAreMatch(expected.Expression, query.Expression);

                Assert.IsNotNull(query.FirstOrDefault());
            }
        }

        [TestMethod]
        public void ShouldExpandGeneric()
        {
            using (var context = CreateSqliteDbContext())
            {
                var query = context.Users.AsExpandable()
                    .FlatMap(u => u.Posts
                        .Filter(p => !p.IsDeleted)
                        .Map(p => p.Author)
                        .FlatMap(a => a.Posts));

                var expected = context.Users
                    .SelectMany(u => u.Posts
#if !EF_CORE
                        .AsQueryable()
#endif
                        .Where(p => !p.IsDeleted)
                        .Select(p => p.Author)
                        .SelectMany(a => a.Posts));

                Assert.AreEqual(expected.ToString(), query.ToString());

                Assert.AreNotSame(expected.Expression, query.Expression);

                Assert.That.MethodCallsAreMatch(expected.Expression, query.Expression);

                Assert.IsNull(query.FirstOrDefault());
            }
        }

        [TestMethod]
        public void ShouldExpandBoundLambdas()
        {
            using (var context = CreateSqliteDbContext())
            {
                var user = new User();
                context.Users.Add(user);

                var post = new Post { Title = "first", Author = user };
                context.Posts.Add(post);

                context.SaveChanges();

                var query = context.Users.AsExpandable()
                    .SelectMany(u => context.Posts
                        .Filter(p => p.CreatorUserId == u.Id && !p.IsDeleted));

                var expected = context.Users.AsExpandable()
                    .SelectMany(u => context.Posts
                        .Where(p => p.CreatorUserId == u.Id && !p.IsDeleted));

                Assert.AreEqual(expected.ToString(), query.ToString());

                Assert.AreNotSame(expected.Expression, query.Expression);

                Assert.That.MethodCallsAreMatch(expected.Expression, query.Expression);

                var queryPosts = query.ToList();
                var expectedPosts = expected.ToList();

                Assert.That.SequenceEqual(expectedPosts.Select(p => p.Id), queryPosts.Select(p => p.Id));
            }
        }

        [TestMethod]
        public void ShouldExpandNestedExtensions()
        {
            var query = Enumerable.Empty<User>()
                .AsQueryable()
                .AsExpandable()
                .SelectMany(u => u.Posts
                    .SelectPostsThat_HasPostsWithAuthorByLogin(u.Login));

            var expected = Enumerable.Empty<User>()
                .AsQueryable()
                .SelectMany(u => u.Posts
#if !EF_CORE
                    .AsQueryable()
#endif
                    .SelectMany(p => p.Author.Posts
#if !EF_CORE
                        .AsQueryable()
#endif
                        .Where(ap => ap.Author.Login == u.Login)));

            Assert.AreNotSame(expected.Expression, query.Expression);

            Assert.That.MethodCallsAreMatch(expected.Expression, query.Expression);

            Assert.IsNull(query.FirstOrDefault());
        }

        [TestMethod]
        public void ShouldWorkWithInclude()
        {
            using (var context = CreateSqliteDbContext())
            {
                var user = new User();
                context.Users.Add(user);

                var post = new Post { Title = "first", Author = user };
                context.Posts.Add(post);

                context.SaveChanges();

                var query = context.Users.AsExpandable()
                    .SelectMany(u => u.Posts.FilterIsActive())
                    .Include(p => p.Author);

                var expected = context.Users.AsExpandable()
                    .SelectMany(u => u.Posts.Where(p => !p.IsDeleted))
                    .Include(p => p.Author);

                Assert.AreEqual(expected.ToString(), query.ToString());

                Assert.AreNotSame(expected.Expression, query.Expression);

                Assert.That.MethodCallsAreMatch(expected.Expression, query.Expression);

                var queryPosts = query.ToList();
                var expectedPosts = expected.ToList();

                Assert.That.SequenceEqual(expectedPosts.Select(p => p.Id), queryPosts.Select(p => p.Id));
                Assert.That.SequenceEqual(expectedPosts.Select(p => p.Author.Id), queryPosts.Select(p => p.Author.Id));
            }
        }

        [TestMethod]
        public void ShouldWorkAsNonGeneric()
        {
            using (var context = CreateSqliteDbContext())
            {
                var user = new User();
                context.Users.Add(user);

                var post = new Post { Title = "first", Author = user };
                context.Posts.Add(post);

                context.SaveChanges();

                var query = context.Users.AsExpandable()
                    .SelectMany(u => u.Posts.FilterIsActive());

                var posts = new List<Post>();

                foreach (Post p in query as IEnumerable)
                {
                    posts.Add(p);
                }

                Assert.AreEqual(1, posts.Count);
                Assert.AreEqual("first", posts[0].Title);
                Assert.AreEqual(typeof(Post), (query as IQueryable).ElementType);
            }
        }

        [TestMethod]
        public async Task ShouldWorkAsAsync()
        {
            using (var context = CreateSqliteDbContext())
            {
                var user = new User();
                context.Users.Add(user);

                var post = new Post { Title = "first", Author = user };
                context.Posts.Add(post);

                context.SaveChanges();

                var query = context.Users.AsExpandable()
                    .SelectMany(u => u.Posts.FilterIsActive());

                var posts = await query.ToListAsync();

                Assert.AreEqual(1, posts.Count);
                Assert.AreEqual("first", posts[0].Title);

                var groupQuery = query
                    .GroupBy(p => p.CreatorUserId)
                    .Select(g => g.ToList());

                var groupPosts = await groupQuery.FirstOrDefaultAsync();

                Assert.AreEqual(1, groupPosts.Count);
                Assert.AreEqual("first", groupPosts[0].Title);
            }
        }
    }
}
