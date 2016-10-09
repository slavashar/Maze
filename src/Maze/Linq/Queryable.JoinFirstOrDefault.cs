using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Maze.Linq
{
    public static partial class Queryable
    {
        public static IQueryable<TResult> JoinFirstOrDefault<TOuter, TInner, TKey, TResult>(this IQueryable<TOuter> outer, IQueryable<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, TInner, TResult>> resultSelector)
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

            return outer.Provider.CreateQuery<TResult>(
                System.Linq.Expressions.Expression.Call(
                    null,
                    ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(typeof(TOuter), typeof(TInner), typeof(TKey), typeof(TResult)),
                    new Expression[] { outer.Expression, inner.Expression, System.Linq.Expressions.Expression.Quote(outerKeySelector), System.Linq.Expressions.Expression.Quote(innerKeySelector), System.Linq.Expressions.Expression.Quote(resultSelector) }));
        }
    }
}
