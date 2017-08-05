using System;
using System.Linq;

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
        public static IQueryable<T> AsExtendable<T>(this IQueryable<T> queryable)
        {
            return queryable.AsVisitable(new ExpandExtensionsVisitor());
        }
    }
}
