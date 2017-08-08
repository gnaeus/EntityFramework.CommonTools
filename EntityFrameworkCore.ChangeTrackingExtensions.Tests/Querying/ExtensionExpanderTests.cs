#if EF_CORE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EntityFrameworkCore.ChangeTrackingExtensions.Tests
#else
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EntityFramework.ChangeTrackingExtensions.Tests
#endif
{
    public static class Q
    {
        private class ExtensionMethodVisitor : System.Linq.Expressions.ExpressionVisitor
        {
            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                MethodInfo method = node.Method;

                if (method.IsStatic && method.Name.StartsWith("Filter"))
                {
                    Type queryableType = method.GetParameters().First().ParameterType;

                    Type entityType = queryableType.GetGenericArguments().Single();

                    object inputQueryable = typeof(ExtensionMethodVisitor)
                        .GetMethod(nameof(EmptyQueryable))
                        .MakeGenericMethod(entityType)
                        .Invoke(null, null);

                    if (method.IsGenericMethod)
                    {
                        method = method.MakeGenericMethod(entityType);
                    }

                    object outputQueryable = method.Invoke(null, new[] { inputQueryable });

                    var expression = (MethodCallExpression)((IQueryable)outputQueryable).Expression;

                    Expression sourceArg = node.Arguments[0];

                    if (!typeof(IQueryable).IsAssignableFrom(sourceArg.Type))
                    {
                        MethodInfo asQueryable = typeof(Queryable)
                            .GetMethods(BindingFlags.Static | BindingFlags.Public)
                            .First(m => m.Name == nameof(Queryable.AsQueryable) && m.IsGenericMethod)
                            .MakeGenericMethod(entityType);

                        sourceArg = Expression.Call(asQueryable, sourceArg);
                    }

                    var outputExpression = new SourceReplacementVisitor(sourceArg).Visit(expression);

                    return outputExpression;
                }

                return base.VisitMethodCall(node);
            }

            public static IQueryable<T> EmptyQueryable<T>()
            {
                return Enumerable.Empty<T>().AsQueryable();
            }
        }

        private class SourceReplacementVisitor : System.Linq.Expressions.ExpressionVisitor
        {
            readonly Expression _replacementArg;

            public SourceReplacementVisitor(Expression replacementArg)
            {
                _replacementArg = replacementArg;
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                Expression sourceArg = node.Arguments.FirstOrDefault();

                if (sourceArg != null && typeof(EnumerableQuery).IsAssignableFrom(sourceArg.Type))
                {
                    return node.Update(
                        null, new[] { _replacementArg }.Concat(node.Arguments.Skip(1)));
                }

                return base.VisitMethodCall(node);
            }
        }
    }

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
                        .FilterByEditor(1)
                        .FilterByEditor(u.Id)
                        .FilterByEditor(u.Id + 1)
                        .FilterToday(5)
                        .Count());

                var expected = context.Users.AsExpandable()
                    .Where(u => !u.IsDeleted)
                    .Where(u => u.Login == login)
                    .Select(u => u.Posts
                        .Where(p => p.UpdaterUserId == updaterId)
                        .Where(p => p.UpdaterUserId == u.Id)
                        .Where(p => p.UpdaterUserId == u.Id + 1)
                        .Where(p => !p.IsDeleted)
                        .Where(p => p.CreatedUtc > today)
                        .Take(5)
                        .Count());

                Assert.AreEqual(expected.ToString(), query.ToString());

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
                        .Where(p => !p.IsDeleted)
                        .Select(p => p.Author)
                        .SelectMany(a => a.Posts));

                Assert.AreEqual(expected.ToString(), query.ToString());

                Assert.IsNull(query.FirstOrDefault());
            }
        }
    }
}
