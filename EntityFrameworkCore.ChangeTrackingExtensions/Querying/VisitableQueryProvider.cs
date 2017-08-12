// The MIT License
// Based on https://github.com/scottksmith95/LINQKit
// Original work: Copyright (c) 2007-2009 Joseph Albahari, Tomas Petricek
//                Copyright (c) 2013-2017 Scott Smith, Stef Heyenrath, Tuomas Hietanen
// Modified work: Copyright (c) 2017 Dmitry Panyushkin

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

#if EF_CORE
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace EntityFrameworkCore.ChangeTrackingExtensions
#elif EF_6
using System.Data.Entity;
using System.Data.Entity.Infrastructure;

namespace EntityFramework.ChangeTrackingExtensions
#else
namespace System.Linq.CommonTools
#endif
{
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
            expression = _query.Visitors.Visit(expression);
            return _query.InnerQuery.Provider.CreateQuery<TElement>(expression).AsVisitable(_query.Visitors);
        }

        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            expression = _query.Visitors.Visit(expression);
            return _query.InnerQuery.Provider.CreateQuery(expression);
        }

        TResult IQueryProvider.Execute<TResult>(Expression expression)
        {
            expression = _query.Visitors.Visit(expression);
            return _query.InnerQuery.Provider.Execute<TResult>(expression);
        }

        object IQueryProvider.Execute(Expression expression)
        {
            expression = _query.Visitors.Visit(expression);
            return _query.InnerQuery.Provider.Execute(expression);
        }

#if EF_CORE
        public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
        {
            expression = _query.Visitors.Visit(expression);
            var asyncProvider = _query.InnerQuery.Provider as IAsyncQueryProvider;
            return asyncProvider.ExecuteAsync<TResult>(expression);
        }

        public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            expression = _query.Visitors.Visit(expression);
            var asyncProvider = _query.InnerQuery.Provider as IAsyncQueryProvider;
            return asyncProvider?.ExecuteAsync<TResult>(expression, cancellationToken)
                ?? Task.FromResult(_query.InnerQuery.Provider.Execute<TResult>(expression));
        }
#elif EF_6
        public Task<object> ExecuteAsync(Expression expression, CancellationToken cancellationToken)
        {
            expression = _query.Visitors.Visit(expression);
            var asyncProvider = _query.InnerQuery.Provider as IDbAsyncQueryProvider;
            return asyncProvider?.ExecuteAsync(expression, cancellationToken)
                ?? Task.FromResult(_query.InnerQuery.Provider.Execute(expression));
        }

        public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            expression = _query.Visitors.Visit(expression);
            var asyncProvider = _query.InnerQuery.Provider as IDbAsyncQueryProvider;
            return asyncProvider?.ExecuteAsync<TResult>(expression, cancellationToken)
                ?? Task.FromResult(_query.InnerQuery.Provider.Execute<TResult>(expression));
        }
#endif
    }
}
