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
    /// Specification pattren https://en.wikipedia.org/wiki/Specification_pattern.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISpecification<T>
    {
        bool IsSatisfiedBy(T entity);

        Expression<Func<T, bool>> ToExpression();
    }

    /// <summary>
    /// Implementation of Specification pattren, that can be used with <see cref="IQueryable{T}"/> expressions.
    /// </summary>
    public class Specification<T> : ISpecification<T>
    {
        readonly Lazy<Func<T, bool>> _lazyFunc;

        protected Expression<Func<T, bool>> Predicate;

        protected Specification()
        {
            _lazyFunc = new Lazy<Func<T, bool>>(() => ToExpression().Compile());
        }

        public Specification(Expression<Func<T, bool>> predicate)
            : this()
        {
            Predicate = predicate;
        }

        public virtual bool IsSatisfiedBy(T entity)
        {
            return _lazyFunc.Value.Invoke(entity);
        }

        public virtual Expression<Func<T, bool>> ToExpression()
        {
            return Predicate;
        }

        public static implicit operator Expression<Func<T, bool>>(Specification<T> spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));

            return spec.ToExpression();
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
                    Expression.Not(spec.ToExpression().Body),
                    spec.ToExpression().Parameters));
        }

        public static Specification<T> operator &(Specification<T> left, Specification<T> right)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            if (right == null) throw new ArgumentNullException(nameof(right));

            ParameterExpression parameter = left.ToExpression().Parameters[0];

            return new Specification<T>(
                Expression.Lambda<Func<T, bool>>(
                    Expression.AndAlso(
                        left.ToExpression().Body,
                        new ParameterReplacer(parameter).Visit(right.ToExpression().Body)),
                    parameter));
        }

        public static Specification<T> operator |(Specification<T> left, Specification<T> right)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            if (right == null) throw new ArgumentNullException(nameof(right));

            ParameterExpression parameter = left.ToExpression().Parameters[0];

            return new Specification<T>(
                Expression.Lambda<Func<T, bool>>(
                    Expression.OrElse(
                        left.ToExpression().Body,
                        new ParameterReplacer(parameter).Visit(right.ToExpression().Body)),
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
