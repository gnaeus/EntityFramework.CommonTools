#if EF_CORE
using System;
using System.Linq.Expressions;

namespace EntityFrameworkCore.ChangeTrackingExtensions
#else
using System;
using System.Linq.Expressions;

namespace EntityFramework.ChangeTrackingExtensions
#endif
{
    public class Specification<T>
    {
        readonly Lazy<Func<T, bool>> _lazyFunc;

        protected Expression<Func<T, bool>> Expression;

        protected Specification()
        {
            _lazyFunc = new Lazy<Func<T, bool>>(() => Expression.Compile());
        }

        public Specification(Expression<Func<T, bool>> expression)
            : this()
        {
            Expression = expression;
        }

        public bool IsSatisfiedBy(T entity)
        {
            return _lazyFunc.Value.Invoke(entity);
        }

        public static implicit operator Specification<T>(Expression<Func<T, bool>> expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            return new Specification<T>(expression);
        }

        public static implicit operator Expression<Func<T, bool>>(Specification<T> specification)
        {
            if (specification == null) throw new ArgumentNullException(nameof(specification));

            return specification.Expression;
        }

        public static implicit operator Func<T, bool>(Specification<T> specification)
        {
            if (specification == null) throw new ArgumentNullException(nameof(specification));

            return specification._lazyFunc.Value;
        }

        public static bool operator true(Specification<T> specification)
        {
            return false;
        }

        public static bool operator false(Specification<T> specification)
        {
            return false;
        }

        public static Specification<T> operator !(Specification<T> specification)
        {
            return new Specification<T>(
                System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(
                    System.Linq.Expressions.Expression.Not(specification.Expression.Body),
                    specification.Expression.Parameters));
        }

        public static Specification<T> operator &(Specification<T> left, Specification<T> right)
        {
            ParameterExpression parameter = left.Expression.Parameters[0];

            return new Specification<T>(
                System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(
                    System.Linq.Expressions.Expression.AndAlso(
                        left.Expression.Body,
                        new ParameterReplacer(parameter).Visit(right.Expression.Body)),
                    parameter));
        }

        public static Specification<T> operator |(Specification<T> left, Specification<T> right)
        {
            ParameterExpression parameter = left.Expression.Parameters[0];

            return new Specification<T>(
                System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(
                    System.Linq.Expressions.Expression.OrElse(
                        left.Expression.Body,
                        new ParameterReplacer(parameter).Visit(right.Expression.Body)),
                    parameter));
        }
    }

    internal class ParameterReplacer : ExpressionVisitor
    {
        readonly ParameterExpression _parameter;

        public ParameterReplacer(ParameterExpression parameter)
        {
            _parameter = parameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return base.VisitParameter(_parameter);
        }
    }
}
