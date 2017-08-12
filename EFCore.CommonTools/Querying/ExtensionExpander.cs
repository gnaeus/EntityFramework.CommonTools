using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

#if EF_CORE
namespace EntityFrameworkCore.CommonTools
#elif EF_6
namespace EntityFramework.CommonTools
#else
namespace System.Linq.CommonTools
#endif
{
    /// <summary>
    /// <see cref="ExpressionVisitor"/> that expands <see cref="IQueryable{T}"/> extension methods inside Expression.
    /// </summary>
    public class ExtensionExpander : ExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            MethodInfo method = node.Method;

            if (method.IsDefined(typeof(ExtensionAttribute), true)
                && method.IsDefined(typeof(ExpandableAttribute), true))
            {
                ParameterInfo[] methodParams = method.GetParameters();
                Type queryableType = methodParams.First().ParameterType;
                Type entityType = queryableType.GetGenericArguments().Single();
                
                object inputQueryable = MakeEnumerableQuery(entityType);
                
                object[] arguments = new object[methodParams.Length];

                arguments[0] = inputQueryable;

                var argumentReplacements = new List<KeyValuePair<string, Expression>>();

                for (int i = 1; i < methodParams.Length; i++)
                {
                    try
                    {
                        arguments[i] = node.Arguments[i].GetValue();
                    }
                    catch (InvalidOperationException)
                    {
                        ParameterInfo paramInfo = methodParams[i];
                        Type paramType = paramInfo.GetType();
                        
                        arguments[i] = paramType.GetTypeInfo().IsValueType
                            ? Activator.CreateInstance(paramType) : null;

                        argumentReplacements.Add(
                            new KeyValuePair<string, Expression>(paramInfo.Name, node.Arguments[i]));
                    }
                }

                object outputQueryable = method.Invoke(null, arguments);

                Expression expression = ((IQueryable)outputQueryable).Expression;

                Expression realQueryable = node.Arguments[0];

                if (!typeof(IQueryable).IsAssignableFrom(realQueryable.Type))
                {
                    MethodInfo asQueryable = _asQueryable.MakeGenericMethod(entityType);
                    realQueryable = Expression.Call(asQueryable, realQueryable);
                }

                expression = new ExtensionRebinder(
                    inputQueryable, realQueryable, argumentReplacements).Visit(expression);
                
                return Visit(expression);
            }
            return base.VisitMethodCall(node);
        }
        
        private static object MakeEnumerableQuery(Type entityType)
        {
            return _queryableEmpty.MakeGenericMethod(entityType).Invoke(null, null);
        }

        private static readonly MethodInfo _asQueryable = typeof(Queryable)
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .First(m => m.Name == nameof(Queryable.AsQueryable) && m.IsGenericMethod);

        private static readonly MethodInfo _queryableEmpty = (typeof(ExtensionExpander))
            .GetMethod(nameof(QueryableEmpty), BindingFlags.Static | BindingFlags.NonPublic);
        
        private static IQueryable<T> QueryableEmpty<T>()
        {
            return Enumerable.Empty<T>().AsQueryable();
        }
    }
}
