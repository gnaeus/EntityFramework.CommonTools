using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if EF_CORE
namespace EntityFrameworkCore.CommonTools.Tests
#elif EF_6
namespace EntityFramework.CommonTools.Tests
#endif
{
    [TestClass]
    public class SpecificationExpanderTests : TestInitializer
    {
        public class PostActiveSpec : Specification<Post>
        {
            public PostActiveSpec()
                : base(p => !p.IsDeleted) { }
        }

        [TestMethod]
        public void ShouldBeExpandedInExpressionTree()
        {
            using (var context = CreateSqliteDbContext())
            {
                context.Users.Add(new User
                {
                    Login = "admin", IsDeleted = false,
                    Posts = new List<Post>
                    {
                        new Post { Title = "test", IsDeleted = false },
                    },
                });
                
                context.SaveChanges();

                var postSpec = new PostActiveSpec();

                var query = context.Users
                    .AsVisitable(new SpecificationExpander())
                    .Select(u => u.Posts.Where(postSpec));

                var expected = context.Users
                    .Select(u => u.Posts.Where(p => !p.IsDeleted));

                Assert.AreEqual(expected.ToString(), query.ToString());

                Assert.That.MethodCallsAreMatch(expected.Expression, query.Expression);

                Assert.IsNotNull(query.Single());
            }
        }

        public class PostByTitleSpec : ISpecification<Post>
        {
            readonly string _title;

            public PostByTitleSpec(string title)
            {
                _title = title;
            }

            public bool IsSatisfiedBy(Post post)
            {
                return post.Title == _title;
            }

            public Expression<Func<Post, bool>> ToExpression()
            {
                return p => p.Title == _title;
            }
        }

        [TestMethod]
        public void ShouldCallToExpressionInExpressionTree()
        {
            using (var context = CreateSqliteDbContext())
            {
                context.Users.Add(new User
                {
                    Login = "admin",
                    IsDeleted = false,
                    Posts = new List<Post>
                    {
                        new Post { Title = "test", IsDeleted = false },
                    },
                });

                context.SaveChanges();

                string title = "test";

                var postSpec = new PostByTitleSpec(title);

                var query = context.Users
#if EF_CORE
                    .AsVisitable(new SpecificationExpander(), new AsQueryableExpander())
#elif EF_6
                    .AsVisitable(new SpecificationExpander())
#endif
                    .SelectMany(u => u.Posts.AsQueryable().Where(postSpec.ToExpression()));

                var expected = context.Users
#if EF_CORE
                    .AsVisitable(new AsQueryableExpander())
#endif
                    .SelectMany(u => u.Posts.AsQueryable().Where(p => p.Title == title));

                var e = expected.ToList();

                Assert.AreEqual(expected.ToString(), query.ToString());

                Assert.That.MethodCallsAreMatch(expected.Expression, query.Expression);

                Assert.IsNotNull(query.Single());
            }
        }

        public class PostByContentSpec : Specification<Post>
        {
            public PostByContentSpec(string content)
            {
                Predicate = p => p.Content.Contains(content);
            }
        }

        [TestMethod]
        public void ShouldSupportConditionalLogicInExpressionTree()
        {
            using (var context = CreateSqliteDbContext())
            {
                context.Users.Add(new User
                {
                    Login = "admin",
                    IsDeleted = false,
                    Posts = new List<Post>
                    {
                        new Post { Content = "content", IsDeleted = false },
                    },
                });

                context.SaveChanges();

                string content = "content";

                var query = context.Users
                    .AsVisitable(new SpecificationExpander())
                    .Select(u => u.Posts.Where(new PostByContentSpec(content) || new PostByContentSpec(content)));

                var expected = context.Users
                    .Select(u => u.Posts.Where(p => p.Content.Contains(content) || p.Content.Contains(content)));

                Assert.AreEqual(expected.ToString(), query.ToString());

                Assert.That.MethodCallsAreMatch(expected.Expression, query.Expression);

                Assert.IsNotNull(query.Single());
            }
        }
        
        public class PostRecursiveSpec : Specification<Post>
        {
            public PostRecursiveSpec(string content)
                : base(p => p.Author.Posts.Any(new PostByContentSpec(content))) { }
        }

        [TestMethod]
        public void ShouldSupportRecursiveSpecsInExpressionTree()
        {
            var users = new[]
            {
                new User
                {
                    Login = "admin",
                    IsDeleted = false,
                    Posts = new List<Post>
                    {
                        new Post { Content = "content", IsDeleted = false },
                    },
                }
            };

            users.First().Posts.First().Author = users.First();
            
            string content = "content";

            var query = users.AsQueryable()
                .AsVisitable(new SpecificationExpander())
                .SelectMany(u => u.Posts.Where(new PostRecursiveSpec(content)));

            var expected = users.AsQueryable()
                .SelectMany(u => u.Posts.Where(p => p.Author.Posts.Any(ap => ap.Content.Contains(content))));

            Assert.That.MethodCallsAreMatch(expected.Expression, query.Expression);

            Assert.AreEqual(expected.Single(), query.Single());
        }

        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public void ShouldNotSupportParametersInExpressionTree()
        {
            using (var context = CreateSqliteDbContext())
            {
                context.Users.Add(new User
                {
                    Login = "admin",
                    IsDeleted = false,
                    Posts = new List<Post>
                    {
                        new Post { Title = "test", IsDeleted = false },
                    },
                });

                context.SaveChanges();

                try
                {
                    var query = context.Users
                        .AsVisitable(new SpecificationExpander())
                        .Select(u => u.Posts.Where(new PostByContentSpec(u.Login)));
                }
                catch (InvalidOperationException ex)
                {
                    throw;
                }
            }
        }
    }
}
