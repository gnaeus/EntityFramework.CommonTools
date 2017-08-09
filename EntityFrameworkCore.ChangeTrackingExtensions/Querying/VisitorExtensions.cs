using System.Linq.Expressions;

#if EF_CORE
namespace EntityFrameworkCore.ChangeTrackingExtensions
#elif EF_6
namespace EntityFramework.ChangeTrackingExtensions
#else
namespace QueryableExtensions
#endif
{
    internal static class VisitorExtensions
    {
        public static Expression Visit(this ExpressionVisitor[] visitors, Expression node)
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
