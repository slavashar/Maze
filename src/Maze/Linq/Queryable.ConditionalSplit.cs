using System;
using System.Linq;
using System.Linq.Expressions;

namespace Maze.Linq
{
    public interface ISplitedQueryable : IQueryable
    {
    }

    public interface ISplitedQueryable<out T> : IQueryable<T>, ISplitedQueryable
    {
    }

    public static partial class Queryable
    {
        public static ISplitedQueryable<TSource> ConditionalSplit<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            throw new NotImplementedException();
        }

        public static ISplitedQueryable<TSource> ThenSplit<TSource>(this ISplitedQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            throw new NotImplementedException();
        }

        public static IQueryable<TSource> ThenOther<TSource>(this ISplitedQueryable<TSource> source)
        {
            throw new NotImplementedException();
        }

        public static IQueryable<TResult> MergeSplitResults<TSource, TResult>(this ISplitedQueryable<TSource> source, params IQueryable<TResult>[] results)
        {
            throw new NotImplementedException();
        }
    }
}
