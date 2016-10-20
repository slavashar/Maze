using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Maze.Linq
{
    public static partial class Queryable
    {
        public static IQueryable<TResult> JoinFirstOrDefault<TOuter, TInner, TKey, TResult>(this IQueryable<TOuter> outer, IEnumerable<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, TInner, TResult>> resultSelector)
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
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.JoinFirstOrDefault(
                        default(IQueryable<TOuter>),
                        default(IEnumerable<TInner>),
                        default(Expression<Func<TOuter, TKey>>),
                        default(Expression<Func<TInner, TKey>>),
                        default(Expression<Func<TOuter, TInner, TResult>>))),
                    new Expression[] {
                        outer.Expression,
                        GetSourceExpression(inner),
                        Expression.Quote(outerKeySelector),
                        Expression.Quote(innerKeySelector),
                        Expression.Quote(resultSelector)
                    }
                ));
        }

        private static MethodInfo GetMethodInfoOf<T>(Expression<Func<T>> expression)
        {
            return ((MethodCallExpression)expression.Body).Method;
        }

        private static Expression GetSourceExpression<TSource>(IEnumerable<TSource> source)
        {
            var q = source as IQueryable<TSource>;
            return q != null ? q.Expression : Expression.Constant(source, typeof(IEnumerable<TSource>));
        }
    }
}
