#if EF_CORE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EntityFrameworkCore.ChangeTrackingExtensions.Tests
#else
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EntityFramework.ChangeTrackingExtensions.Tests
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
                    .AsVisitable(new SpecificationExpander())
                    .Select(u => u.Posts.Where(p => !p.IsDeleted));

                Assert.AreEqual(expected.ToString(), query.ToString());

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

#if !EF_CORE
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
                    .AsVisitable(new SpecificationExpander())
                    .Select(u => u.Posts.AsQueryable().Where(postSpec.ToExpression()));
                
                var expected = context.Users
                    .AsVisitable(new SpecificationExpander())
                    .Select(u => u.Posts.Where(p => p.Title == title));
                Assert.AreEqual(expected.ToString(), query.ToString());
                Assert.IsNotNull(query.Single());
            }
        }
#endif

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
                    .AsVisitable(new SpecificationExpander())
                    .Select(u => u.Posts.Where(p => p.Content.Contains(content) || p.Content.Contains(content)));

                Assert.AreEqual(expected.ToString(), query.ToString());

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
                .AsVisitable(new SpecificationExpander())
                .SelectMany(u => u.Posts.Where(p => p.Author.Posts.Any(ap => ap.Content.Contains(content))));

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
