using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

#if EF_CORE
namespace EntityFrameworkCore.ChangeTrackingExtensions
#elif EF_6
namespace EntityFramework.ChangeTrackingExtensions
#else
namespace System.Linq.CommonTools
#endif
{
    /// <summary>
    /// <see cref="ExpressionVisitor"/> that expands
    /// <see cref="Queryable.AsQueryable{TElement}(IEnumerable{TElement})"/> inside Expression.
    /// </summary>
    public class AsQueryableExpander : ExpressionVisitor
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

                Type[] genericArguments = null;

                if (originalMethod.IsGenericMethod)
                {
                    genericArguments = originalMethod.GetGenericArguments();
                    originalMethod = originalMethod.GetGenericMethodDefinition();
                }

                MethodInfo replacementMethod;

                if (MethodReplacements.TryGetValue(originalMethod, out replacementMethod))
                {
                    if (genericArguments != null)
                    {
                        replacementMethod = replacementMethod.MakeGenericMethod(genericArguments);
                    }

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
                    
                    if (typeof(IOrderedQueryable).IsAssignableFrom(expandedArguments[0].Type))
                    {
                        expandedArguments[0] = Visit(expandedArguments[0]);
                    }
                    
                    return Visit(Expression.Call(replacementMethod, expandedArguments));
                }
            }
            return base.VisitMethodCall(node);
        }

        static readonly Dictionary<MethodInfo, MethodInfo> MethodReplacements;

        static AsQueryableExpressionExpander()
        {
            MethodReplacements = new Dictionary<MethodInfo, MethodInfo>();

            var queryableMethods = typeof(Queryable)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(method => method.IsDefined(typeof(ExtensionAttribute), true))
                .Select(method => new
                {
                    Name = method.Name,
                    Method = method,
                    Signature = GetMethodSignature(method),
                });

            var enumerableLookup = typeof(Enumerable)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(method => method.IsDefined(typeof(ExtensionAttribute), true))
                .ToLookup(method => method.Name, method => new
                {
                    Method = method,
                    Signature = GetMethodSignature(method),
                });
            
            foreach (var queryable in queryableMethods)
            {
                var enumerableMethods = enumerableLookup[queryable.Name];

                if (enumerableMethods != null)
                {
                    var enumerable = enumerableMethods
                        .FirstOrDefault(method => method.Signature == queryable.Signature);
                    
                    if (enumerable != null)
                    {
                        MethodReplacements[queryable.Method] = enumerable.Method;
                    }
                }
            }
        }

        private static string GetMethodSignature(MethodInfo method)
        {
            var sb = new StringBuilder();
            
            foreach (ParameterInfo param in method.GetParameters())
            {
                AddTypeSignature(sb, param.ParameterType);
            }

            return sb.ToString();
        }

        private static void AddTypeSignature(StringBuilder sb, Type type)
        {
            if (type == typeof(IQueryable))
            {
                type = typeof(IEnumerable);
            }

            if (type.GetTypeInfo().IsGenericType)
            {
                Type generic = type.GetGenericTypeDefinition();

                if (generic == typeof(Expression<>))
                {
                    type = type.GetGenericArguments().First();
                }
                else if (generic == typeof(IQueryable<>))
                {
                    type = typeof(IEnumerable<>).MakeGenericType(type.GetGenericArguments());
                }
                else if (generic == typeof(IOrderedQueryable<>))
                {
                    type = typeof(IOrderedEnumerable<>).MakeGenericType(type.GetGenericArguments());
                }
            }

            sb.Append(type.Name);

            if (type.GetTypeInfo().IsGenericType)
            {
                sb.Append("[ ");

                foreach (Type argument in type.GetGenericArguments())
                {
                    AddTypeSignature(sb, argument);
                }

                sb.Append("]");
            }

            sb.Append(" ");
        }
    }
}