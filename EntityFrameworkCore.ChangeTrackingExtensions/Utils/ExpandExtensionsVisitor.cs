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
    /// <summary>
    /// <see cref="ExpressionVisitor"/> that expands <see cref="IQueryable{T}"/> extension methods inside Expression.
    /// </summary>
    internal class ExpandExtensionsVisitor : ExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            throw new NotImplementedException();
        }
    }
}
