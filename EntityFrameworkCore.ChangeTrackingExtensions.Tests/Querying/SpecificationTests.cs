#if EF_CORE
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EntityFrameworkCore.ChangeTrackingExtensions.Tests
#else
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EntityFramework.ChangeTrackingExtensions.Tests
#endif
{
    [TestClass]
    public class SpecificationTests : TestInitializer
    {
        public class UserActiveSpec : Specification<User>
        {
            public UserActiveSpec()
            {
                Predicate = u => !u.IsDeleted;
            }
        }

        public class UserByLoginSpec : Specification<User>
        {
            public UserByLoginSpec(string login)
            {
                Predicate = u => u.Login == login;
            }
        }

        public class UserAndSpec : Specification<User>
        {
            public UserAndSpec(string login)
                : base(new UserActiveSpec() && new UserByLoginSpec(login)) { }
        }

        public class UserOrSpec : Specification<User>
        {
            public UserOrSpec(string login)
            {
                Predicate = new UserActiveSpec() || new UserByLoginSpec(login);
            }
        }

        [TestMethod]
        public void SouldConsumePlainObjects()
        {
            var activeUser = new User { IsDeleted = false };
            var deletedUser = new User { IsDeleted = true };

            var spec = new UserActiveSpec();

            Assert.IsTrue(spec.IsSatisfiedBy(activeUser));
            Assert.IsFalse(spec.IsSatisfiedBy(deletedUser));
        }

        [TestMethod]
        public void SouldAcceptParameters()
        {
            var admin = new User { Login = "admin" };

            var spec = new UserByLoginSpec("admin");

            Assert.IsTrue(spec.IsSatisfiedBy(admin));
        }

        [TestMethod]
        public void ShouldSupportComposition()
        {
            var activeAdmin = new User { Login = "admin", IsDeleted = false };
            var deletedAdmin = new User { Login = "admin", IsDeleted = true };

            var andSpec = new UserAndSpec("admin");

            Assert.IsTrue(andSpec.IsSatisfiedBy(activeAdmin));
            Assert.IsFalse(andSpec.IsSatisfiedBy(deletedAdmin));

            var orSpec = new UserOrSpec("admin");

            Assert.IsTrue(orSpec.IsSatisfiedBy(activeAdmin));
            Assert.IsTrue(orSpec.IsSatisfiedBy(deletedAdmin));
        }

        [TestMethod]
        public void ShouldSupportConditionalLogic()
        {
            var activeAdmin = new User { Login = "admin", IsDeleted = false };
            var deletedAdmin = new User { Login = "admin", IsDeleted = true };

            var andSpec = new UserActiveSpec() && new UserByLoginSpec("admin");

            Assert.IsTrue(andSpec.IsSatisfiedBy(activeAdmin));
            Assert.IsFalse(andSpec.IsSatisfiedBy(deletedAdmin));

            var orSpec = new UserActiveSpec() || new UserByLoginSpec("admin");

            Assert.IsTrue(orSpec.IsSatisfiedBy(activeAdmin));
            Assert.IsTrue(orSpec.IsSatisfiedBy(deletedAdmin));

            var notSpec = !new UserByLoginSpec("admin");

            Assert.IsFalse(notSpec.IsSatisfiedBy(activeAdmin));
            Assert.IsFalse(notSpec.IsSatisfiedBy(deletedAdmin));
        }

        [TestMethod]
        public void ShouldSupportComplexExpressions()
        {
            var user = new User
            {
                IsDeleted = false,
                Posts = new List<Post>
                {
                    new Post { IsDeleted = false },
                },
            };

            var activeUserSpec = new Specification<User>(u => !u.IsDeleted);
            var hasPostsSpec = new Specification<User>(u => u.Posts.Any(p => !p.IsDeleted));
            var validUserSpec = activeUserSpec && hasPostsSpec;

            Assert.IsTrue(validUserSpec.IsSatisfiedBy(user));
        }

        [TestMethod]
        public void ShouldWorkWithEnumerable()
        {
            var users = new[]
            {
                new User { Login = "admin", IsDeleted = false },
                new User { Login = "admin", IsDeleted = true },
            };

            var andSpec = new UserAndSpec("admin");

            users.Where(andSpec.IsSatisfiedBy).Single();

            users.Where(andSpec).Single();
        }

        [TestMethod]
        public void ShouldWorkWithQueryable()
        {
            using (var context = CreateSqliteDbContext())
            {
                context.Users.AddRange(new[]
                {
                    new User { Login = "admin", IsDeleted = false },
                    new User { Login = "admin", IsDeleted = true },
                });

                context.SaveChanges();

                var andSpec = new UserAndSpec("admin");

                context.Users.Where(andSpec.ToExpression()).Single();

                context.Users.Where(andSpec).Single();

            }
        }
    }
}
