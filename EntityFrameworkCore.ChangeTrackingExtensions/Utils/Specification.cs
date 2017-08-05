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

        protected Expression<Func<T, bool>> Predicate;

        protected Specification()
        {
            _lazyFunc = new Lazy<Func<T, bool>>(() => Predicate.Compile());
        }

        public Specification(Expression<Func<T, bool>> predicate)
            : this()
        {
            Predicate = predicate;
        }

        public bool IsSatisfiedBy(T entity)
        {
            return _lazyFunc.Value.Invoke(entity);
        }

        public static implicit operator Func<T, bool>(Specification<T> specification)
        {
            if (specification == null) throw new ArgumentNullException(nameof(specification));

            return specification._lazyFunc.Value;
        }

        public static implicit operator Expression<Func<T, bool>>(Specification<T> specification)
        {
            if (specification == null) throw new ArgumentNullException(nameof(specification));

            return specification.Predicate;
        }

        public static implicit operator Specification<T>(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return new Specification<T>(predicate);
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
                Expression.Lambda<Func<T, bool>>(
                    Expression.Not(specification.Predicate.Body),
                    specification.Predicate.Parameters));
        }

        public static Specification<T> operator &(Specification<T> left, Specification<T> right)
        {
            ParameterExpression parameter = left.Predicate.Parameters[0];

            return new Specification<T>(
                Expression.Lambda<Func<T, bool>>(
                    Expression.AndAlso(
                        left.Predicate.Body,
                        new ParameterReplacer(parameter).Visit(right.Predicate.Body)),
                    parameter));
        }

        public static Specification<T> operator |(Specification<T> left, Specification<T> right)
        {
            ParameterExpression parameter = left.Predicate.Parameters[0];

            return new Specification<T>(
                Expression.Lambda<Func<T, bool>>(
                    Expression.OrElse(
                        left.Predicate.Body,
                        new ParameterReplacer(parameter).Visit(right.Predicate.Body)),
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
