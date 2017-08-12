using System.Collections.Generic;
using System.Linq.Expressions;

#if EF_CORE
namespace EntityFrameworkCore.ChangeTrackingExtensions
#elif EF_6
namespace EntityFramework.ChangeTrackingExtensions
#else
namespace System.Linq.CommonTools
#endif
{
    public static class VisitorExtensions
    {
        /// <summary>
        /// Apply all <paramref name="visitors"/> to Expression one by one.
        /// </summary>
        public static Expression Visit(this IEnumerable<ExpressionVisitor> visitors, Expression node)
        {
            if (visitors != null)
            {
                foreach (ExpressionVisitor visitor in visitors)
                {
                    node = visitor.Visit(node);
                }
            }
            return node;
        }
    }
}
