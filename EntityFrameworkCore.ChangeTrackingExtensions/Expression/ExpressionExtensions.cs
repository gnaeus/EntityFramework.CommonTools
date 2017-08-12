using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

#if EF_CORE
namespace EntityFrameworkCore.ChangeTrackingExtensions
#elif EF_6
namespace EntityFramework.ChangeTrackingExtensions
#else
namespace System.Linq.CommonTools
#endif
{
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Get computed value of Expression.
        /// </summary>
        /// <exception cref="InvalidOperationException" />
        public static object GetValue(this Expression expression)
        {
            if (expression == null)
            {
                return null;
            }
            if (expression.NodeType == ExpressionType.Constant)
            {
                return ((ConstantExpression)expression).Value;
            }
            if (expression.NodeType == ExpressionType.MemberAccess)
            {
                var member = (MemberExpression)expression;

                if (member.Expression.NodeType == ExpressionType.Constant)
                {
                    var instance = (ConstantExpression)member.Expression;

                    if (instance.Type.GetTypeInfo().IsDefined(typeof(CompilerGeneratedAttribute)))
                    {
                        return instance.Type
                            .GetField(member.Member.Name)
                            .GetValue(instance.Value);
                    }
                }
            }

            // we can't interpret the expression but we can compile and run it

            var objectMember = Expression.Convert(expression, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);

            return getterLambda.Compile().Invoke();
        }
    }
}
