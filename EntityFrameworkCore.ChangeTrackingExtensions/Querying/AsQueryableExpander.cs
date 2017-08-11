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
    internal class AsQueryableExpander : ExpressionVisitor
    {
        readonly ExpressionVisitor _expressionExpander = new AsQueryableExpressionExpander();

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Quote)
            {
                return _expressionExpander.Visit(node);
            }
            return base.VisitUnary(node);
        }
    }

    internal class AsQueryableExpressionExpander : ExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            MethodInfo originalMethod = node.Method;

            if (originalMethod.DeclaringType == typeof(Queryable)
                && originalMethod.IsDefined(typeof(ExtensionAttribute), true))
            {
                if (originalMethod.Name == nameof(Queryable.AsQueryable))
                {
                    return Visit(node.Arguments[0]);
                }

                ParameterInfo[] originalParams = originalMethod.GetParameters();
                
                // TODO: cahce
                MethodInfo replacementMethod = typeof(Enumerable)
                    .GetMethods(BindingFlags.Static | BindingFlags.Public)
                    .First(m => m.Name == originalMethod.Name && m.IsGenericMethod
                        && m.GetParameters().Length == originalParams.Length);

                if (replacementMethod != null)
                {
                    replacementMethod = replacementMethod
                        .MakeGenericMethod(originalMethod.GetGenericArguments());

                    Expression[] expandedArguments = new Expression[node.Arguments.Count];

                    for (int i = 0; i < node.Arguments.Count; i++)
                    {
                        Expression argument = node.Arguments[i];

                        if (argument.NodeType == ExpressionType.Quote)
                        {
                            expandedArguments[i] = ((UnaryExpression)argument).Operand;
                        }
                        else
                        {
                            expandedArguments[i] = argument;
                        }
                    }
                    
                    return Visit(Expression.Call(replacementMethod, expandedArguments));
                }
            }
            return base.VisitMethodCall(node);
        }
    }
}