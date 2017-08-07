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
    public static class Extensions
    {
        public static IQueryable<Post> FilterActive(this IEnumerable<Post> posts)
        {
            return posts.Filter(p => p.Title == "123").Filter(p => !p.IsDeleted);
        }

        public static IQueryable<User> FilterActive(this IEnumerable<User> users)
        {
            return users.Filter(u => u.Posts.FilterActive().Any());
        }

        public static IQueryable<User> FilterByLogin(this IQueryable<User> queryable, string login, int id)
        {
            return queryable.Where(u => u.Login == login && u.Id == id);
        }
    }

    public static class QueryableExtensions
    {
        public static IQueryable<T> Filter<T>(this IEnumerable<T> enumerable, Expression<Func<T, bool>> predicate)
        {
            IQueryable<T> queryable = enumerable.AsQueryable();

            Expression expression = new ExtensionMethodVisitor().Visit(predicate);

            return queryable.Where((Expression<Func<T, bool>>)expression);
        }

        private class ExtensionMethodVisitor : ExpressionVisitor
        {
            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                MethodInfo method = node.Method;

                if (method.IsStatic && method.Name.StartsWith(nameof(Filter)))
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

        private class SourceReplacementVisitor : ExpressionVisitor
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

    [TestClass]
    public class ExpandableExtensionsTests : TestInitializer
    {
    }
}
