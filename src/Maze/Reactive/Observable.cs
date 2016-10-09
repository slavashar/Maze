using System;

namespace Maze.Reactive
{
    public static class Observable
    {
        public static IObservable<TResult> Join<TOuter, TInner, TKey, TResult>(this IObservable<TOuter> outer, IObservable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector)
        {
            return new MergeJoin<TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector);
        }

        public static IObservable<TResult> Join<TOuter, TInner, TKey, TResult>(this IObservable<TOuter> outer, IObservable<TInner> inner, Func<TOuter, IObservable<TKey>> outerKeySelector, Func<TInner, IObservable<TKey>> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector)
        {
            throw new NotImplementedException();
        }

        public static IObservable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IObservable<TOuter> outer, IObservable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IObservable<TInner>, TResult> resultSelector)
        {
            return new MergeJoinGroup<TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector);
        }

        public static IObservable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IObservable<TOuter> outer, IObservable<TInner> inner, Func<TOuter, IObservable<TKey>> outerKeySelector, Func<TInner, IObservable<TKey>> innerKeySelector, Func<TOuter, IObservable<TInner>, TResult> resultSelector)
        {
            throw new NotImplementedException();
        }

        public static IObservable<TSource> Where<TSource>(this IObservable<TSource> source, Func<TSource, IObservable<bool>> predicate)
        {
            return new Where<TSource>(source, predicate);
        }
    }
}
