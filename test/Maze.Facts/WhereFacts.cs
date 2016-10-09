using Maze.Reactive;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Reactive.Testing;
using System.Reactive.Subjects;
using Observable = System.Reactive.Linq.Observable;
using ObservableMaze = Maze.Reactive.Observable;

namespace Maze.Facts
{
    public class WhereFacts
    {
        [Fact]
        public async Task execute_empty()
        {
            var result = await ObservableMaze
                .Where(Observable.Empty<int?>(), v => Observable.Return(true))
                .SingleOrDefaultAsync();

            result.ShouldBeNull();
        }

        [Fact]
        public async Task execute_a_constant()
        {
            var result = await ObservableMaze.Where(Observable.Return(1), v => Observable.Return(true)).SingleAsync();

            result.ShouldEqual(1);
        }

        [Fact]
        public async Task execute()
        {
            var result = await ObservableMaze.Where(Observable.Range(0, 4), v => Observable.Return(v % 2 == 0)).ToArray();

            result.ShouldEqual(new[] { 0, 2 });
        }

        [Fact]
        public void wait_for_result()
        {
            var scheduler = new TestScheduler();

            var result = new BehaviorSubject<int>(0);

            ObservableMaze.Where(Observable.Return(1), v => Observable.Return(true, scheduler)).Subscribe(result);

            result.Value.ShouldEqual(0);

            scheduler.Start();            
            result.Value.ShouldEqual(1);
        }
    }
}
