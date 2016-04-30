using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maze.Linq
{
    public interface IAttachableEnumerable : IEnumerable
    {
    }

    public interface IAttachableEnumerable<out TSource, out T> : IEnumerable<T>, IAttachableEnumerable
    {
        IAttachableEnumerable<TSource, TResult> AddSelector<TResult>(Func<T, TResult> selector);
    }

    public interface IBranchEnumerable : IEnumerable
    {
    }

    public interface IBranchEnumerable<out T> : IAttachableEnumerable<T, T>, IBranchEnumerable
    {
        IBranchEnumerable<T> AddBranch(Func<T, bool> predicate);
    }

    public static partial class Enumerable
    {
        public static IBranchEnumerable<TSource> ConditionalSplit<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new BranchEnumerable<TSource>(source, predicate);
        }

        public static IBranchEnumerable<TSource> ThenSplit<TSource>(this IBranchEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.AddBranch(predicate);
        }

        public static IBranchEnumerable<TSource> ThenOther<TSource>(this IBranchEnumerable<TSource> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.AddBranch(x => true);
        }

        public static IAttachableEnumerable<T, TResult> Select<T, TSource, TResult>(this IAttachableEnumerable<T, TSource> source, Func<TSource, TResult> selector)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.AddSelector(selector);
        }

        public static IEnumerable<TResult> MergeSplitResults<TSource, TResult>(this IBranchEnumerable<TSource> source, params IAttachableEnumerable<TSource, TResult>[] results)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            throw new NotImplementedException();
        }

        private class BranchEnumerable<TSource> : IBranchEnumerable<TSource>
        {
            private readonly IEnumerable<TSource> source;
            private readonly BranchEnumerable<TSource> sibling;
            private readonly Func<TSource, bool> predicate;

            private IEnumerable<TSource> output;

            public BranchEnumerable(IEnumerable<TSource> source, Func<TSource, bool> predicate)
            {
                this.source = source;
                this.predicate = predicate;
            }

            private BranchEnumerable(BranchEnumerable<TSource> sibling, Func<TSource, bool> predicate)
            {
                this.source = sibling.source;
                this.sibling = sibling;
                this.predicate = predicate;
            }

            public IBranchEnumerable<TSource> AddBranch(Func<TSource, bool> predicate)
            {
                return new BranchEnumerable<TSource>(this, predicate);
            }

            public IAttachableEnumerable<TSource, TResult> AddSelector<TResult>(Func<TSource, TResult> selector)
            {
                throw new NotImplementedException();
            }

            public IEnumerator<TSource> GetEnumerator()
            {
                if (this.output == null)
                {
                    var o = this.source.Where(this.predicate);
                    var s = this.sibling;

                    while (s != null)
                    {
                        var p = s.predicate;
                        o = o.Where(x => !p(x));
                        s = s.sibling;
                    }

                    this.output = o;
                }

                return this.output.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        private class MergedSplitedEnumerable<TSource, TResult> : IEnumerable<TResult>
        {
            private IEnumerable<TSource> source;
            private ImmutableArray<BranchEnumerable<TSource>> branches;

            public MergedSplitedEnumerable(IEnumerable<TSource> source)
            {
                this.source = source;
            }

            public IEnumerator<TResult> GetEnumerator()
            {
                throw new NotImplementedException();
                //return new Iterator(this.source, this.spliters.Select(x => Tuple.));
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            private class Iterator : IEnumerator<TResult>
            {
                private readonly IEnumerable<TSource> source;
                private readonly List<Tuple<Func<TSource, bool>, Func<TSource, TResult>>> spliters;

                private IEnumerator<TSource> enumerator;
                private TResult current;
                private bool init = false;

                public Iterator(IEnumerable<TSource> source, List<Tuple<Func<TSource, bool>, Func<TSource, TResult>>> spliters)
                {
                    this.source = source;
                    this.spliters = spliters;
                }

                public TResult Current
                {
                    get { return this.current; }
                }

                object IEnumerator.Current
                {
                    get { return this.Current; }
                }

                public void Dispose()
                {
                    if (this.enumerator is IDisposable)
                    {
                        this.enumerator.Dispose();
                    }

                    this.current = default(TResult);
                    this.enumerator = null;
                }

                public bool MoveNext()
                {
                    if (!this.init)
                    {
                        this.enumerator = this.source.GetEnumerator();
                        this.init = true;
                    }

                    while (this.enumerator.MoveNext())
                    {
                        var sourceCurrent = this.enumerator.Current;

                        for (int i = 0; i < this.spliters.Count; i++)
                        {
                            var s = this.spliters[i];

                            if (s.Item1(sourceCurrent))
                            {
                                this.current = s.Item2(sourceCurrent);
                                return true;
                            }
                        }
                    }

                    this.current = default(TResult);
                    return false;
                }

                public void Reset()
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }
}
