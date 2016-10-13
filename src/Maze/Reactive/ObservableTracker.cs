using System;
using System.Reactive.Subjects;

namespace Maze.Reactive
{
    public abstract class ObservableTracker
    {
        public IObservable<TElement> Get<TElement>()
        {
            return (IObservable<TElement>)this;
        }
    }

    public class ObservableTracker<TElement> : ObservableTracker, IObservable<TElement>
    {
        private Subject<TElement> subject = new Subject<TElement>();

        public IDisposable Subscribe(IObserver<TElement> observer)
        {
            return this.subject.Subscribe(observer);
        }

        internal IObservable<TElement> Attach(IObservable<TElement> source)
        {
            return new ProxyObservable(this, source);
        }

        private class ProxyObservable : IObservable<TElement>
        {
            private readonly ObservableTracker<TElement> parent;
            private readonly IObservable<TElement> original;

            public ProxyObservable(ObservableTracker<TElement> parent, IObservable<TElement> original)
            {
                this.parent = parent;
                this.original = original;
            }

            public IDisposable Subscribe(IObserver<TElement> observer)
            {
                return this.original.Subscribe(new ProxyObserve(this.parent, observer));
            }
        }

        private class ProxyObserve : IObserver<TElement>
        {
            private readonly ObservableTracker<TElement> parent;
            private readonly IObserver<TElement> original;

            public ProxyObserve(ObservableTracker<TElement> parent, IObserver<TElement> original)
            {
                this.parent = parent;
                this.original = original;
            }

            public void OnCompleted()
            {
                this.parent.subject.OnCompleted();
                this.original.OnCompleted();
            }

            public void OnError(Exception error)
            {
                this.parent.subject.OnError(error);
                this.original.OnError(error);
            }

            public void OnNext(TElement value)
            {
                this.parent.subject.OnNext(value);
                this.original.OnNext(value);
            }
        }
    }
}
