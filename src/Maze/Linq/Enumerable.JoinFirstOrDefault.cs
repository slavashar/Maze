using System;
using System.Collections.Generic;
using System.Linq;

namespace Maze.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TResult> JoinFirstOrDefault<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector)
        {
            if (outer == null)
            {
                throw new ArgumentNullException(nameof(outer));
            }

            if (inner == null)
            {
                throw new ArgumentNullException(nameof(inner));
            }

            if (outerKeySelector == null)
            {
                throw new ArgumentNullException(nameof(outerKeySelector));
            }

            if (innerKeySelector == null)
            {
                throw new ArgumentNullException(nameof(innerKeySelector));
            }

            if (resultSelector == null)
            {
                throw new ArgumentNullException(nameof(resultSelector));
            }

            return JoinFirstOrDefaultIterator(outer, inner, outerKeySelector, innerKeySelector, resultSelector);
        }

        private static IEnumerable<TResult> JoinFirstOrDefaultIterator<TOuter, TInner, TKey, TResult>(IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector)
        {
            var lookup = inner.ToLookup(innerKeySelector);

            foreach (TOuter item in outer)
            {
                yield return resultSelector(item, lookup[outerKeySelector(item)].FirstOrDefault());
            }
        }
    }
}
