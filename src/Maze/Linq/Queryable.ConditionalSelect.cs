using System;
using System.Linq;
using System.Linq.Expressions;

namespace Maze.Linq
{
    public interface IConditionalSelectQueryable : IQueryable
    {
    }

    public interface IConditionalSelectQueryable<TSource, TResult> : IQueryable<TResult>, IConditionalSelectQueryable
    {
    }

    public static partial class Queryable
    {
        public static IConditionalSelectQueryable<TSource, TResult> ConditionalSelect<TSource, TResult>(
            this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, Expression<Func<TSource, TResult>> selector)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            throw new NotImplementedException();
        }

        public static IConditionalSelectQueryable<TSource, TResult> ThenSelect<TSource, TResult>(
            this IConditionalSelectQueryable<TSource, TResult> source, Expression<Func<TSource, bool>> predicate, Expression<Func<TSource, TResult>> selector)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            throw new NotImplementedException();
        }

        public static IConditionalSelectQueryable<TSource, TResult> ThenSelectOthers<TSource, TResult>(
            this IConditionalSelectQueryable<TSource, TResult> source, Expression<Func<TSource, TResult>> selector)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            throw new NotImplementedException();
        }
    }
}
