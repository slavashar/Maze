using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading;

namespace Maze.Reactive
{
    public class Where<TSource> : IObservable<TSource>
    {
        private readonly IObservable<TSource> source;

        private readonly Func<TSource, IObservable<bool>> predicate;

        public Where(IObservable<TSource> source, Func<TSource, IObservable<bool>> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        public IDisposable Subscribe(IObserver<TSource> observer)
        {
            return Executor.Run(this, observer);
        }

        private class Executor : IDisposable, IObserver<TSource>
        {
            private readonly Where<TSource> parent;
            private readonly IObserver<TSource> observer;

            private readonly CompositeDisposable disposable = new CompositeDisposable();

            private int waiterCount;
            private bool completed;

            public Executor(Where<TSource> parent, IObserver<TSource> observer)
            {
                this.parent = parent;
                this.observer = observer;
            }

            public static IDisposable Run(Where<TSource> parent, IObserver<TSource> observer)
            {
                var exec = new Executor(parent, observer);

                exec.disposable.Add(ObservableExtensions.SubscribeSafe(parent.source, exec));

                return exec;
            }

            public void OnNext(TSource value)
            {
                var result = this.parent.predicate(value);

                var cancel = new SingleAssignmentDisposable();
                this.disposable.Add(cancel);

                cancel.Disposable = ObservableExtensions.SubscribeSafe(result, new Waiter(this, value, cancel));
            }

            public void OnError(Exception error)
            {
                this.observer.OnError(error);
                this.Dispose();
            }

            public void OnCompleted()
            {
                this.completed = true;

                if (this.waiterCount == 0)
                {
                    this.observer.OnCompleted();
                }
            }

            public void Dispose()
            {
                this.disposable.Dispose();
            }

            private class Waiter : IObserver<bool>
            {
                private readonly Executor executor;
                private readonly TSource item;
                private bool? result;

                private SingleAssignmentDisposable cancel;

                public Waiter(Executor executor, TSource item, SingleAssignmentDisposable cancel)
                {
                    this.executor = executor;
                    this.item = item;
                    this.cancel = cancel;

                    Interlocked.Increment(ref this.executor.waiterCount);
                }

                public void OnCompleted()
                {
                    this.executor.disposable.Remove(this.cancel);

                    if (!this.result.HasValue)
                    {
                        this.executor.OnError(new InvalidOperationException("A value was not provided."));
                        return;
                    }

                    if (this.result.Value)
                    {
                        this.executor.observer.OnNext(this.item);
                    }

                    if (Interlocked.Decrement(ref this.executor.waiterCount) == 0)
                    {
                        if (this.executor.completed)
                        {
                            this.executor.observer.OnCompleted();
                        }
                    }
                }

                public void OnError(Exception error)
                {
                    this.executor.disposable.Remove(this.cancel);

                    Interlocked.Decrement(ref this.executor.waiterCount);

                    this.executor.OnError(error);
                }

                public void OnNext(bool value)
                {
                    this.result = value;
                }
            }
        }
    }
}