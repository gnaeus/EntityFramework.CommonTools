// The MIT License
// Based on https://github.com/scottksmith95/LINQKit
// Original work: Copyright (c) 2007-2009 Joseph Albahari, Tomas Petricek
//                Copyright (c) 2013-2017 Scott Smith, Stef Heyenrath, Tuomas Hietanen
// Modified work: Copyright (c) 2017 Dmitry Panyushkin

#if EF_6
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity.Infrastructure;
using System.Threading.Tasks;
using System.Threading;

namespace EntityFramework.CommonTools
{
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
}
#endif
