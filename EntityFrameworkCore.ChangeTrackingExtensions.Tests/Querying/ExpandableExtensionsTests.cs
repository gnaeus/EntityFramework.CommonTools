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

                    var expression = (MethodCallExpression)( (IQueryable)outputQueryable ).Expression;

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

    [TestClass]
    public class ExpandableExtensionsTests : TestInitializer
    {
        [TestMethod]
        public void Test()
        {
            using (var context = CreateSqliteDbContext())
            {
                context.Users.AddRange(new[]
                {
                    new User { Login = "admin", IsDeleted = false },
                    new User { Login = "admin", IsDeleted = true },
                });

                context.SaveChanges();

                int postsLimit = 5;

                context.Users.AsExpandable()
                    .FilterByLogin("admin")
                    .Select(u => new
                    {
                        User = u,
                        Posts = u.Posts
                            .FilterByEditor(1)
                            .FilterByEditor(u.Id)
                            .FilterByEditor(u.Id + 1)
                            .FilterToday(postsLimit)
                            .ToList(),
                    });
            }
        }
    }
}
