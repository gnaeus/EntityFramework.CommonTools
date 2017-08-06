// The MIT License
// Based on https://github.com/scottksmith95/LINQKit
// Original work: Copyright (c) 2007-2009 Joseph Albahari, Tomas Petricek
//                Copyright (c) 2013-2017 Scott Smith, Stef Heyenrath, Tuomas Hietanen
// Modified work: Copyright (c) 2017 Dmitry Panyushkin

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

#if EF_CORE
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace EntityFrameworkCore.ChangeTrackingExtensions
#elif EF_6
using System.Data.Entity;
using System.Data.Entity.Infrastructure;

namespace EntityFramework.ChangeTrackingExtensions
#else

namespace QueryableExtensions
#endif
{
    public static partial class QueryableExtensions
    {
        /// <summary>
        /// Wrap <see cref="IQueryable{T}"/> to decorator that intercepts
        /// IQueryable.Expression with provided <see cref="ExpressionVisitor"/>.
        /// </summary>
        public static IQueryable<T> AsVisitable<T>(this IQueryable<T> queryable, ExpressionVisitor visitor)
        {
            if (queryable == null) throw new ArgumentNullException(nameof(queryable));
            if (visitor == null) throw new ArgumentNullException(nameof(visitor));

            Type visitorType = visitor.GetType();
            IQueryable<T> innerQuery = queryable;

            while (true)
            {
                var visitableQuery = innerQuery as VisitableQuery<T>;

                if (visitableQuery == null)
                {
                    return VisitableQueryFactory<T>.Create(queryable, visitor);
                }
                
                if (visitableQuery.Visitor.GetType() == visitorType)
                {
                    return queryable;
                }

                innerQuery = visitableQuery.InnerQuery;
            }
        }
    }
    
    /// <summary>
    /// An <see cref="IQueryable{T}"/> wrapper that allows us to visit
    /// the query's expression tree just before LINQ to SQL gets to it.
    /// </summary>
    internal class VisitableQuery<T> : IQueryable<T>, IOrderedQueryable<T>, IOrderedQueryable
#if EF_CORE
        , IAsyncEnumerable<T>
#elif EF_6
        , IDbAsyncEnumerable<T>
#endif
    {
        readonly ExpressionVisitor _visitor;
        readonly IQueryable<T> _queryable;
        readonly VisitableQueryProvider<T> _provider;

        internal ExpressionVisitor Visitor => _visitor;
        internal IQueryable<T> InnerQuery => _queryable;

        public VisitableQuery(IQueryable<T> queryable, ExpressionVisitor visitor)
        {
            _queryable = queryable;
            _visitor = visitor;
            _provider = new VisitableQueryProvider<T>(this);
        }

        Expression IQueryable.Expression => _queryable.Expression;

        Type IQueryable.ElementType => typeof(T);

        IQueryProvider IQueryable.Provider => _provider;

        public IEnumerator<T> GetEnumerator()
        {
            return _queryable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _queryable.GetEnumerator();
        }

        public override string ToString()
        {
            return _queryable.ToString();
        }

#if EF_CORE
        IAsyncEnumerator<T> IAsyncEnumerable<T>.GetEnumerator()
        {
            return (_inner as IAsyncEnumerable<T>)?.GetEnumerator()
                ?? (_inner as IAsyncEnumerableAccessor<T>)?.AsyncEnumerable.GetEnumerator();
        }
#elif EF_6
        public IDbAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return (_queryable as IDbAsyncEnumerable<T>)?.GetAsyncEnumerator()
                ?? new DbAsyncEnumerator<T>(_queryable.GetEnumerator());
        }

        IDbAsyncEnumerator IDbAsyncEnumerable.GetAsyncEnumerator()
        {
            return GetAsyncEnumerator();
        }
#endif
    }

#if EF_CORE || EF_6
    internal class VisitableQueryOfClass<T> : VisitableQuery<T>
        where T : class
    {
        public VisitableQueryOfClass(IQueryable<T> queryable, ExpressionVisitor visitor)
            : base(queryable, visitor)
        {
        }

#if EF_CORE
        public IQueryable<T> Include<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath)
        {
            return InnerQuery.Include(navigationPropertyPath).AsVisitable(Visitor);
        }
#elif EF_6
        public IQueryable<T> Include(string path)
        {
            return InnerQuery.Include(path).AsVisitable(Visitor);
        }
#endif
    }

    internal static class VisitableQueryFactory<T>
    {
        public static readonly Func<IQueryable<T>, ExpressionVisitor, VisitableQuery<T>> Create;

        static VisitableQueryFactory()
        {
            if (!typeof(T).GetTypeInfo().IsClass)
            {
                Create = (query, visitor) => new VisitableQuery<T>(query, visitor);
                return;
            }

            var queryType = typeof(IQueryable<T>);
            var visitorType = typeof(ExpressionVisitor);
            var ctorInfo = typeof(VisitableQueryOfClass<>)
                .MakeGenericType(typeof(T))
                .GetConstructor(new[] { queryType, visitorType });

            var queryParam = Expression.Parameter(queryType);
            var visitorParam = Expression.Parameter(visitorType);
            var newExpr = Expression.New(ctorInfo, queryParam, visitorParam);
            var createExpr = Expression.Lambda<Func<IQueryable<T>, ExpressionVisitor, VisitableQuery<T>>>(
                newExpr, queryParam, visitorParam);

            Create = createExpr.Compile();
        }
    }
#endif

    internal class VisitableQueryProvider<T> : IQueryProvider
#if EF_CORE
        , IAsyncQueryProvider
#elif EF_6
        , IDbAsyncQueryProvider
#endif
    {
        readonly VisitableQuery<T> _query;

        public VisitableQueryProvider(VisitableQuery<T> query)
        {
            _query = query;
        }

        /// <summary>
        /// The following four methods first call ExpressionExpander to visit the expression tree,
        /// then call upon the inner query to do the remaining work.
        /// </summary>
        IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
        {
            expression = _query.Visitor.Visit(expression);
            return _query.InnerQuery.Provider.CreateQuery<TElement>(expression).AsVisitable(_query.Visitor);
        }

        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            expression = _query.Visitor.Visit(expression);
            return _query.InnerQuery.Provider.CreateQuery(expression);
        }

        TResult IQueryProvider.Execute<TResult>(Expression expression)
        {
            expression = _query.Visitor.Visit(expression);
            return _query.InnerQuery.Provider.Execute<TResult>(expression);
        }

        object IQueryProvider.Execute(Expression expression)
        {
            expression = _query.Visitor.Visit(expression);
            return _query.InnerQuery.Provider.Execute(expression);
        }

#if EF_CORE
        public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
        {
            expression = _query.Visitor.Visit(expression);
            var asyncProvider = _query.InnerQuery.Provider as IAsyncQueryProvider;
            return asyncProvider.ExecuteAsync<TResult>(expression);
        }

        public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            expression = _query.Visitor.Visit(expression);
            var asyncProvider = _query.InnerQuery.Provider as IAsyncQueryProvider;
            return asyncProvider?.ExecuteAsync<TResult>(expression, cancellationToken)
                ?? Task.FromResult(_query.InnerQuery.Provider.Execute<TResult>(expression));
        }
#elif EF_6
        public Task<object> ExecuteAsync(Expression expression, CancellationToken cancellationToken)
        {
            expression = _query.Visitor.Visit(expression);
            var asyncProvider = _query.InnerQuery.Provider as IDbAsyncQueryProvider;
            return asyncProvider?.ExecuteAsync(expression, cancellationToken)
                ?? Task.FromResult(_query.InnerQuery.Provider.Execute(expression));
        }

        public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            expression = _query.Visitor.Visit(expression);
            var asyncProvider = _query.InnerQuery.Provider as IDbAsyncQueryProvider;
            return asyncProvider?.ExecuteAsync<TResult>(expression, cancellationToken)
                ?? Task.FromResult(_query.InnerQuery.Provider.Execute<TResult>(expression));
        }
#endif
    }

#if EF_6
    /// <summary>
    /// Class for async-await style list enumeration support
    /// (e.g. <see cref="System.Data.Entity.QueryableExtensions.ToListAsync(IQueryable)"/>)
    /// </summary>
    internal class DbAsyncEnumerator<T> : IDisposable, IDbAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public DbAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public void Dispose()
        {
            _inner.Dispose();
        }

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_inner.MoveNext());
        }

        public T Current
        {
            get { return _inner.Current; }
        }

        object IDbAsyncEnumerator.Current
        {
            get { return Current; }
        }
    }
#endif
}
