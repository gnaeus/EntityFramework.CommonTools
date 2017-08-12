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
using System.Reflection;

#if EF_CORE
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace EntityFrameworkCore.CommonTools
#elif EF_6
using System.Data.Entity;
using System.Data.Entity.Infrastructure;

namespace EntityFramework.CommonTools
#else
namespace System.Linq.CommonTools
#endif
{
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
        readonly ExpressionVisitor[] _visitors;
        readonly IQueryable<T> _queryable;
        readonly VisitableQueryProvider<T> _provider;

        internal ExpressionVisitor[] Visitors => _visitors;
        internal IQueryable<T> InnerQuery => _queryable;

        public VisitableQuery(IQueryable<T> queryable, ExpressionVisitor[] visitors)
        {
            _queryable = queryable;
            _visitors = visitors;
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
            return (_queryable as IAsyncEnumerable<T>)?.GetEnumerator()
                ?? (_queryable as IAsyncEnumerableAccessor<T>)?.AsyncEnumerable.GetEnumerator();
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
        public VisitableQueryOfClass(IQueryable<T> queryable, ExpressionVisitor[] visitors)
            : base(queryable, visitors)
        {
        }

#if EF_CORE
        public IQueryable<T> Include<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath)
        {
            return InnerQuery.Include(navigationPropertyPath).AsVisitable(Visitors);
        }
#elif EF_6
        public IQueryable<T> Include(string path)
        {
            return InnerQuery.Include(path).AsVisitable(Visitors);
        }
#endif
    }

    internal static class VisitableQueryFactory<T>
    {
        public static readonly Func<IQueryable<T>, ExpressionVisitor[], VisitableQuery<T>> Create;

        static VisitableQueryFactory()
        {
            if (!typeof(T).GetTypeInfo().IsClass)
            {
                Create = (query, visitors) => new VisitableQuery<T>(query, visitors);
                return;
            }

            var queryType = typeof(IQueryable<T>);
            var visitorsType = typeof(ExpressionVisitor[]);
            var ctorInfo = typeof(VisitableQueryOfClass<>)
                .MakeGenericType(typeof(T))
                .GetConstructor(new[] { queryType, visitorsType });

            var queryParam = Expression.Parameter(queryType);
            var visitorsParam = Expression.Parameter(visitorsType);
            var newExpr = Expression.New(ctorInfo, queryParam, visitorsParam);
            var createExpr = Expression.Lambda<Func<IQueryable<T>, ExpressionVisitor[], VisitableQuery<T>>>(
                newExpr, queryParam, visitorsParam);

            Create = createExpr.Compile();
        }
    }
#endif
}
