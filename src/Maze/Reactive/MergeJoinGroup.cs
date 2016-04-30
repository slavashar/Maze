using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;

namespace Maze.Reactive
{
    public sealed class MergeJoinGroup<TOuter, TInner, TKey, TResult> : IObservable<TResult>
    {
        private readonly IObservable<TOuter> outer;
        private readonly IObservable<TInner> inner;

        private readonly Func<TOuter, TKey> outerKeySelector;
        private readonly Func<TInner, TKey> innerKeySelector;

        private readonly Func<TOuter, IObservable<TInner>, TResult> resultSelector;

        public MergeJoinGroup(IObservable<TOuter> outer, IObservable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IObservable<TInner>, TResult> resultSelector)
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
            private readonly MergeJoinGroup<TOuter, TInner, TKey, TResult> parent;

            private readonly IObserver<TResult> observer;

            private readonly object gate = new object();

            private readonly Lazy<ResultObserver> nullKeySubject;
            private readonly ConcurrentDictionary<TKey, ResultObserver> map;
            private readonly CompositeDisposable disposable = new CompositeDisposable(2);

            private volatile bool outerDone = false, innerDone = false, disposed = false;

            private Executor(MergeJoinGroup<TOuter, TInner, TKey, TResult> parent, IObserver<TResult> observer)
            {
                this.parent = parent;
                this.observer = observer;

                this.nullKeySubject = new Lazy<ResultObserver>(this.CreateResultObserver);
                this.map = new ConcurrentDictionary<TKey, ResultObserver>();
            }

            public static IDisposable Run(MergeJoinGroup<TOuter, TInner, TKey, TResult> parent, IObserver<TResult> observer)
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

                return this.map.GetOrAdd(key, k => this.CreateResultObserver());
            }

            private ResultObserver CreateResultObserver()
            {
                return new ResultObserver(this);
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
                            if (this.nullKeySubject.IsValueCreated)
                            {
                                foreach (var subject in this.nullKeySubject.Value.Subjects)
                                {
                                    subject.OnCompleted();
                                }
                            }

                            foreach (var result in this.map.Values)
                            {
                                foreach (var subject in result.Subjects)
                                {
                                    subject.OnCompleted();
                                }
                            }

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
                private readonly Executor parent;

                private readonly IList<ISubject<TInner>> subjects = new List<ISubject<TInner>>();
                private readonly IList<TInner> innerItems = new List<TInner>();

                public ResultObserver(Executor parent)
                {
                    this.parent = parent;
                }

                public IEnumerable<ISubject<TInner>> Subjects
                {
                    get { return this.subjects; }
                }

                public void AddOuterItem(TOuter value)
                {
                    var subject = new Subject<TInner>();

                    this.subjects.Add(subject);

                    var result = default(TResult);

                    try
                    {
                        result = this.parent.parent.resultSelector(value, subject);
                    }
                    catch (Exception error)
                    {
                        this.parent.OnError(error);
                        return;
                    }

                    this.parent.observer.OnNext(result);

                    foreach (var innerValue in this.innerItems)
                    {
                        subject.OnNext(innerValue);
                    }
                }

                public void AddInnerItem(TInner value)
                {
                    this.innerItems.Add(value);

                    foreach (var subject in this.subjects)
                    {
                        subject.OnNext(value);
                    }
                }
            }
        }
    }
}