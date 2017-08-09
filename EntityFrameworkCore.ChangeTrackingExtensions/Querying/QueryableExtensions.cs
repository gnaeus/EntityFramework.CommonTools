using System;
using System.Linq;
using System.Linq.Expressions;

#if EF_CORE
namespace EntityFrameworkCore.ChangeTrackingExtensions
#elif EF_6
namespace EntityFramework.ChangeTrackingExtensions
#else
namespace QueryableExtensions
#endif
{
    public static partial class QueryableExtensions
    {
        /// <summary>
        /// Expand all <see cref="IQueryable{T}"/> extension methods that marked by <see cref="ExpandableAttribute"/>.
        /// </summary>
        public static IQueryable<T> AsExpandable<T>(this IQueryable<T> queryable)
        {
            if (queryable == null) throw new ArgumentNullException(nameof(queryable));

            return queryable.AsVisitable(new ExtensionExpander());
        }

        /// <summary>
        /// Wrap <see cref="IQueryable{T}"/> to decorator that intercepts
        /// IQueryable.Expression with provided <see cref="ExpressionVisitor"/>.
        /// </summary>
        public static IQueryable<T> AsVisitable<T>(
            this IQueryable<T> queryable, params ExpressionVisitor[] visitors)
        {
            if (queryable == null) throw new ArgumentNullException(nameof(queryable));
            if (visitors == null) throw new ArgumentNullException(nameof(visitors));

            return queryable as VisitableQuery<T>
                ?? VisitableQueryFactory<T>.Create(queryable, visitors);
        }
    }
}
