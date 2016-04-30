using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maze.Linq
{
    public interface IConditionalSelectEnumerable : IEnumerable
    {
    }

    public interface IConditionalSelectEnumerable<TSource, TResult> : IEnumerable<TResult>, IConditionalSelectEnumerable
    {
        IConditionalSelectEnumerable<TSource, TResult> CreateConditionalSelectEnumerable(Func<TSource, bool> predicate, Func<TSource, TResult> selector);
    }

    public static partial class Enumerable
    {
        public static IConditionalSelectEnumerable<TSource, TResult> ConditionalSelect<TSource, TResult>(
            this IEnumerable<TSource> source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
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

            return ConditionalIterator<TSource, TResult>.Create(source, predicate, selector);
        }

        public static IConditionalSelectEnumerable<TSource, TResult> ThenSelect<TSource, TResult>(
            this IConditionalSelectEnumerable<TSource, TResult> source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
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

            return source.CreateConditionalSelectEnumerable(predicate, selector);
        }

        public static IConditionalSelectEnumerable<TSource, TResult> ThenSelectOthers<TSource, TResult>(
            this IConditionalSelectEnumerable<TSource, TResult> source, Func<TSource, TResult> selector)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            return source.CreateConditionalSelectEnumerable(x => true, selector);
        }

        public class ConditionalIterator<TSource, TResult> : IConditionalSelectEnumerable<TSource, TResult>, IEnumerator<TResult>
        {
            private readonly IEnumerable<TSource> source;
            private readonly ImmutableList<Func<TSource, bool>> predicates;
            private readonly ImmutableList<Func<TSource, TResult>> selectors;

            private int state;
            private TResult current;
            private IEnumerator<TSource> enumerator;

            private ConditionalIterator(
                IEnumerable<TSource> source, ImmutableList<Func<TSource, bool>> predicates, ImmutableList<Func<TSource, TResult>> selectors)
            {
                this.source = source;
                this.predicates = predicates;
                this.selectors = selectors;
            }

            public TResult Current
            {
                get { return this.current; }
            }

            object IEnumerator.Current
            {
                get { return this.Current; }
            }

            public static ConditionalIterator<TSource, TResult> Create(
                IEnumerable<TSource> source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
            {
                return new ConditionalIterator<TSource, TResult>(
                    source,
                    ImmutableList<Func<TSource, bool>>.Empty.Add(predicate),
                    ImmutableList<Func<TSource, TResult>>.Empty.Add(selector));
            }

            public IConditionalSelectEnumerable<TSource, TResult> CreateConditionalSelectEnumerable(
                Func<TSource, bool> predicate, Func<TSource, TResult> selector)
            {
                return new ConditionalIterator<TSource, TResult>(
                    this.source,
                    this.predicates.Add(predicate),
                    this.selectors.Add(selector));
            }

            public void Dispose()
            {
                this.current = default(TResult);
                this.state = -1;

                if (this.enumerator != null)
                {
                    this.enumerator.Dispose();
                }

                this.enumerator = null;
            }

            public bool MoveNext()
            {
                switch (this.state)
                {
                    case 1:
                        this.enumerator = this.source.GetEnumerator();
                        this.state = 2;
                        goto case 2;

                    case 2:
                        while (this.enumerator.MoveNext())
                        {
                            TSource item = this.enumerator.Current;

                            for (int index = 0; index < this.predicates.Count; index++)
                            {
                                if (this.predicates[index].Invoke(item))
                                {
                                    this.current = this.selectors[index].Invoke(item);
                                    return true;
                                }
                            }
                        }

                        this.Dispose();
                        break;

                    default:
                        throw new InvalidOperationException();
                }

                return false;
            }

            public void Reset()
            {
                throw new InvalidOperationException();
            }

            public IEnumerator<TResult> GetEnumerator()
            {
                var duplicate = new ConditionalIterator<TSource, TResult>(this.source, this.predicates, this.selectors);
                duplicate.state = 1;
                return duplicate;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }
    }
}
