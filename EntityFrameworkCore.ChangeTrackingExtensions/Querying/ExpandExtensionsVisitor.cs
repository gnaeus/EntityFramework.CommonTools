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
    internal class ExpandExtensionsVisitor : ExpressionVisitor
    {
        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Convert)
            {
                MethodInfo method = node.Method;

                if (method != null && method.Name == "op_Implicit")
                {
                    Type declaringType = method.DeclaringType;

                    if (declaringType.GetTypeInfo().IsGenericType
                        && declaringType.GetGenericTypeDefinition() == typeof(Specification<>))
                    {
                        return VisitSpecification(node.Operand);
                    }
                }
            }

            return base.VisitUnary(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            MethodInfo method = node.Method;

            if (method.IsDefined(typeof(ExtensionAttribute), true)
                && method.IsDefined(typeof(ExpandableAttribute), true))
            {
                return VisitExtensionMethod(node);
            }

            if (method.Name == nameof(Specification<object>.ToExpression))
            {
                Type declaringType = method.DeclaringType;

                if (declaringType.GetTypeInfo().IsGenericType
                    && declaringType.GetGenericTypeDefinition() == typeof(Specification<>))
                {
                    return VisitSpecification(node.Object);
                }
            }

            return base.VisitMethodCall(node);
        }

        private Expression VisitExtensionMethod(MethodCallExpression node)
        {
            return Visit(node);
        }

        private Expression VisitSpecification(Expression node)
        {
            return Visit(node);
        }
    }
}
