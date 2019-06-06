using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

#if EF_CORE
namespace EntityFrameworkCore.CommonTools
#elif EF_6
namespace EntityFramework.CommonTools
#else
namespace System.Linq.CommonTools
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
    [DebuggerDisplay("{Predicate}")]
    public class Specification<T> : ISpecification<T>
    {
        private Func<T, bool> _function;

        private Func<T, bool> Function => _function ?? (_function = Predicate.Compile());
        
        protected Expression<Func<T, bool>> Predicate;

        protected Specification() { }

        public Specification(Expression<Func<T, bool>> predicate)
        {
            Predicate = predicate;
        }

        public bool IsSatisfiedBy(T entity)
        {
            return Function.Invoke(entity);
        }

        public Expression<Func<T, bool>> ToExpression()
        {
            return Predicate;
        }
        
        public static implicit operator Func<T, bool>(Specification<T> spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));

            return spec.Function;
        }

        public static implicit operator Expression<Func<T, bool>>(Specification<T> spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));

            return spec.Predicate;
        }

        /// <summary>
        /// <remarks>
        /// For user-defined conditional logical operators.
        /// https://msdn.microsoft.com/en-us/library/aa691312(v=vs.71).aspx
        /// </remarks>
        /// </summary>
        public static bool operator true(Specification<T> spec)
        {
            return false;
        }


        /// <summary>
        /// <remarks>
        /// For user-defined conditional logical operators.
        /// https://msdn.microsoft.com/en-us/library/aa691312(v=vs.71).aspx
        /// </remarks>
        /// </summary>
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

            var leftExpr = left.Predicate;
            var rightExpr = right.Predicate;
            var leftParam = leftExpr.Parameters[0];
            var rightParam = rightExpr.Parameters[0];

            return new Specification<T>(
                Expression.Lambda<Func<T, bool>>(
                    Expression.AndAlso(
                        leftExpr.Body,
                        new ParameterReplacer(rightParam, leftParam).Visit(rightExpr.Body)),
                    leftParam));
        }

        public static Specification<T> operator |(Specification<T> left, Specification<T> right)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            if (right == null) throw new ArgumentNullException(nameof(right));

            var leftExpr = left.Predicate;
            var rightExpr = right.Predicate;
            var leftParam = leftExpr.Parameters[0];
            var rightParam = rightExpr.Parameters[0];
            
            return new Specification<T>(
                Expression.Lambda<Func<T, bool>>(
                    Expression.OrElse(
                        leftExpr.Body,
                        new ParameterReplacer(rightParam, leftParam).Visit(rightExpr.Body)),
                    leftParam));
        }
    }
}
