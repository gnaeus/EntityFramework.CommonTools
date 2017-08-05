#if EF_CORE
using System;
using System.Linq;
using System.Linq.Expressions;

namespace EntityFrameworkCore.ChangeTrackingExtensions
#else
using System;
using System.Linq;
using System.Linq.Expressions;

namespace EntityFramework.ChangeTrackingExtensions
#endif
{
    /// <summary>
    /// Implementation of Specification pattren,
    /// that can be used with <see cref="IQueryable{T}"/> expressions.
    /// https://en.wikipedia.org/wiki/Specification_pattern
    /// </summary>
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

        public static implicit operator Func<T, bool>(Specification<T> spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));

            return spec._lazyFunc.Value;
        }

        public static implicit operator Expression<Func<T, bool>>(Specification<T> spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));

            return spec.Predicate;
        }

        public static implicit operator Specification<T>(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return new Specification<T>(predicate);
        }

        /// <remarks>
        /// For user-defined conditional logical operators.
        /// https://msdn.microsoft.com/en-us/library/aa691312(v=vs.71).aspx
        /// </remarks>
        public static bool operator true(Specification<T> spec)
        {
            return false;
        }

        /// <remarks>
        /// For user-defined conditional logical operators.
        /// https://msdn.microsoft.com/en-us/library/aa691312(v=vs.71).aspx
        /// </remarks>
        public static bool operator false(Specification<T> spec)
        {
            return false;
        }

        public static Specification<T> operator !(Specification<T> spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));

            return new Specification<T>(
                Expression.Lambda<Func<T, bool>>(
                    Expression.Not(spec.Predicate.Body),
                    spec.Predicate.Parameters));
        }

        public static Specification<T> operator &(Specification<T> left, Specification<T> right)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            if (right == null) throw new ArgumentNullException(nameof(right));

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
            if (left == null) throw new ArgumentNullException(nameof(left));
            if (right == null) throw new ArgumentNullException(nameof(right));

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
