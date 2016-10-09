using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Subjects;

namespace Maze.Reactive
{
    public sealed class MergeJoin<TOuter, TInner, TKey, TResult> : IObservable<TResult>
    {
        private readonly IObservable<TOuter> outer;
        private readonly IObservable<TInner> inner;

        private readonly Func<TOuter, TKey> outerKeySelector;
        private readonly Func<TInner, TKey> innerKeySelector;

        private readonly Func<TOuter, TInner, TResult> resultSelector;

        public MergeJoin(IObservable<TOuter> outer, IObservable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector)
        {
            this.outer = outer;
            this.inner = inner;

            this.outerKeySelector = outerKeySelector;
            this.innerKeySelector = innerKeySelector;

            this.resultSelector = resultSelector;
        }

        public IDisposable Subscribe(IObserver<TResult> observer)
        {
            return Executor.Run(this, observer);
        }

        private class Executor : IDisposable
        {
            private readonly MergeJoin<TOuter, TInner, TKey, TResult> parent;

            private readonly IObserver<TResult> observer;

            private readonly object gate = new object();

            private readonly Lazy<ResultObserver> nullKeySubject;
            private readonly ConcurrentDictionary<TKey, ResultObserver> map;
            private readonly CompositeDisposable disposable = new CompositeDisposable(2);

            private volatile bool outerDone = false;
            private volatile bool innerDone = false;
            private volatile bool disposed = false;

            private Executor(MergeJoin<TOuter, TInner, TKey, TResult> parent, IObserver<TResult> observer)
            {
                this.parent = parent;
                this.observer = observer;

                this.nullKeySubject = new Lazy<ResultObserver>(() => new ResultObserver(this));
                this.map = new ConcurrentDictionary<TKey, ResultObserver>();
            }

            public static IDisposable Run(MergeJoin<TOuter, TInner, TKey, TResult> parent, IObserver<TResult> observer)
            {
                var executor = new Executor(parent, observer);

                SingleAssignmentDisposable outerDisposable, innerDisposable;

                executor.disposable.Add(outerDisposable = new SingleAssignmentDisposable());
                executor.disposable.Add(innerDisposable = new SingleAssignmentDisposable());

                outerDisposable.Disposable = ObservableExtensions.SubscribeSafe(parent.outer, new OuterObserver(executor));
                innerDisposable.Disposable = ObservableExtensions.SubscribeSafe(parent.inner, new InnerObserver(executor));

                return executor;
            }

            public void Dispose()
            {
                this.disposable.Dispose();
                this.disposed = true;
            }

            private ResultObserver GetInnerSubjectByKey(TKey key)
            {
                if (key == null)
                {
                    return this.nullKeySubject.Value;
                }

                return this.map.GetOrAdd(key, k => new ResultObserver(this));
            }

            private void OnNext(TResult value)
            {
                lock (this.gate)
                {
                    if (!this.disposed)
                    {
                        this.observer.OnNext(value);
                    }
                }
            }

            private void OnError(Exception error)
            {
                lock (this.gate)
                {
                    if (!this.disposed)
                    {
                        this.observer.OnError(error);
                        this.Dispose();
                    }
                }
            }

            private void OnCompleted()
            {
                lock (this.gate)
                {
                    if (!this.disposed)
                    {
                        if (this.outerDone && this.innerDone)
                        {
                            this.observer.OnCompleted();
                            this.Dispose();
                        }
                    }
                }
            }

            private class OuterObserver : IObserver<TOuter>
            {
                private Executor parent;

                public OuterObserver(Executor parent)
                {
                    this.parent = parent;
                }

                public void OnNext(TOuter value)
                {
                    var key = default(TKey);
                    try
                    {
                        key = this.parent.parent.outerKeySelector(value);
                    }
                    catch (Exception error)
                    {
                        this.parent.OnError(error);
                        return;
                    }

                    this.parent.GetInnerSubjectByKey(key).AddOuterItem(value);
                }

                public void OnCompleted()
                {
                    this.parent.outerDone = true;
                    this.parent.OnCompleted();
                }

                public void OnError(Exception error)
                {
                    this.parent.OnError(error);
                }
            }

            private class InnerObserver : IObserver<TInner>
            {
                private Executor parent;

                public InnerObserver(Executor parent)
                {
                    this.parent = parent;
                }

                public void OnNext(TInner value)
                {
                    var key = default(TKey);
                    try
                    {
                        key = this.parent.parent.innerKeySelector(value);
                    }
                    catch (Exception error)
                    {
                        this.parent.OnError(error);
                        return;
                    }

                    this.parent.GetInnerSubjectByKey(key).AddInnerItem(value);
                }

                public void OnCompleted()
                {
                    this.parent.innerDone = true;
                    this.parent.OnCompleted();
                }

                public void OnError(Exception error)
                {
                    this.parent.OnError(error);
                }
            }

            private class ResultObserver
            {
                private readonly Executor executor;
                private readonly IList<TOuter> outerItems = new List<TOuter>();
                private readonly IList<TInner> innerItems = new List<TInner>();

                public ResultObserver(Executor executor)
                {
                    this.executor = executor;
                }

                public void AddOuterItem(TOuter value)
                {
                    // TODO: review
                    lock (this)
                    {
                        this.outerItems.Add(value);

                        foreach (var innerValue in this.innerItems)
                        {
                            var result = default(TResult);

                            try
                            {
                                result = this.executor.parent.resultSelector(value, innerValue);
                            }
                            catch (Exception ex)
                            {
                                this.executor.OnError(ex);
                                return;
                            }

                            this.executor.OnNext(result);
                        }
                    }
                }

                public void AddInnerItem(TInner value)
                {
                    // TODO: review
                    lock (this)
                    {
                        this.innerItems.Add(value);

                        foreach (var outerValue in this.outerItems)
                        {
                            var result = default(TResult);

                            try
                            {
                                result = this.executor.parent.resultSelector(outerValue, value);
                            }
                            catch (Exception ex)
                            {
                                this.executor.OnError(ex);
                                return;
                            }

                            this.executor.OnNext(result);
                        }
                    }
                }
            }
        }
    }
}
