using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

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
    internal class ExtensionExpander : ExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            MethodInfo method = node.Method;

            if (method.IsDefined(typeof(ExtensionAttribute), true)
                && method.IsDefined(typeof(ExpandableAttribute), true))
            {
                return base.VisitMethodCall(node);
            }
            
            return base.VisitMethodCall(node);
        }

        // http://stackoverflow.com/a/2616980/1402923
        private static object GetValue(Expression expression)
        {
            var objectMember = Expression.Convert(expression, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            return getterLambda.Compile().Invoke();
        }
    }
}
