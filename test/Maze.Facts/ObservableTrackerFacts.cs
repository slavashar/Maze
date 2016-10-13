using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using Maze.Reactive;
using Microsoft.Reactive.Testing;
using Xunit;
using static Microsoft.Reactive.Testing.ReactiveTest;

namespace Maze.Facts
{
    public class ObservableTrackerFacts
    {
        [Fact]
        public void track_single_observable()
        {
            var scheduler = new TestScheduler();

            var traker = new ObservableTracker<int>();

            var observable = scheduler
                .CreateHotObservable(
                    OnNext(10, 1),
                    OnNext(10, 2),
                    OnNext(10, 3),
                    OnCompleted<int>(10))
                .Track(traker);

            var tracked = traker.ToList().ToTask();

            observable.Subscribe(new Subject<int>());

            scheduler.AdvanceBy(100);

            tracked.IsCompleted.ShouldBeTrue();

            tracked.Result.Count.ShouldEqual(3);
        }

        [Fact]
        public void track_multiple_observable()
        {
            var scheduler = new TestScheduler();

            var traker = new ObservableTracker<int>();

            var observable = scheduler
                .CreateHotObservable(
                    OnNext(10, 1),
                    OnNext(10, 2),
                    OnNext(10, 3),
                    OnCompleted<int>(10))
                .Track(traker);
            
            var tracked = traker.ToList().ToTask();

            observable.Subscribe(new Subject<int>());
            observable.Subscribe(new Subject<int>());

            scheduler.AdvanceBy(100);

            tracked.IsCompleted.ShouldBeTrue();

            tracked.Result.Count.ShouldEqual(6);
        }

        [Fact]
        public void track_multiple_published_observable()
        {
            var scheduler = new TestScheduler();

            var traker = new ObservableTracker<int>();

            var observable = scheduler
                .CreateHotObservable(
                    OnNext(10, 1),
                    OnNext(10, 2),
                    OnNext(10, 3),
                    OnCompleted<int>(10))
                .Track(traker)
                .Publish()
                .RefCount();

            var tracked = traker.ToList().ToTask();

            observable.Subscribe(new Subject<int>());
            observable.Subscribe(new Subject<int>());

            scheduler.AdvanceBy(100);

            tracked.IsCompleted.ShouldBeTrue();

            tracked.Result.Count.ShouldEqual(3);
        }
    }
}
