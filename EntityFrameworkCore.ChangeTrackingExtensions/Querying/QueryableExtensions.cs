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
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ExpandableAttribute : Attribute
    {
    }

    public static partial class QueryableExtensions
    {
        /// <summary>
        /// Expand all <see cref="IQueryable{T}"/> extension methods that marked by <see cref="ExpandableAttribute"/>.
        /// </summary>
        public static IQueryable<T> AsExpandable<T>(this IQueryable<T> queryable)
        {
            return queryable.AsVisitable(new ExpandExtensionsVisitor());
        }

        /// <summary>
        /// Wrap <see cref="IQueryable{T}"/> to decorator that intercepts
        /// IQueryable.Expression with provided <see cref="ExpressionVisitor"/>.
        /// </summary>
        public static IQueryable<T> AsVisitable<T>(this IQueryable<T> queryable, ExpressionVisitor visitor)
        {
            if (queryable == null) throw new ArgumentNullException(nameof(queryable));
            if (visitor == null) throw new ArgumentNullException(nameof(visitor));

            Type visitorType = visitor.GetType();
            IQueryable<T> innerQuery = queryable;

            while (true)
            {
                var visitableQuery = innerQuery as VisitableQuery<T>;

                if (visitableQuery == null)
                {
                    return VisitableQueryFactory<T>.Create(queryable, visitor);
                }

                if (visitableQuery.Visitor.GetType() == visitorType)
                {
                    return queryable;
                }

                innerQuery = visitableQuery.InnerQuery;
            }
        }
    }
}
