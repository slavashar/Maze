using System;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using Xunit;
using Maze.Reactive;
using System.Threading.Tasks;
using Observable = System.Reactive.Linq.Observable;
using ObservableMaze = Maze.Reactive.Observable;

namespace Maze.Facts
{
    public class MergeJoinFacts
    {
        [Fact]
        public async Task execute_join()
        {
            var items = new ReplaySubject<Item>();
            items.OnNext(new Item { CategoryId = 1, Name = "First item" });
            items.OnNext(new Item { CategoryId = 1, Name = "Second item" });
            items.OnNext(new Item { CategoryId = 2, Name = "Third  item" });
            items.OnCompleted();

            var categories = new ReplaySubject<Category>();
            categories.OnNext(new Category { Id = 1, Name = "First category" });
            categories.OnCompleted();

            var names = ObservableMaze.Join(items, categories, i => i.CategoryId, c => c.Id, (i, c) => i.Name + " from " + c.Name);
            
            var list = await names.ToArray();

            list.ShouldEqual(new[] { "First item from First category", "Second item from First category" });
        }

        [Fact(Skip = "not ready yet")]
        public async Task execute_join_with_obs_key()
        {
            var items = new ReplaySubject<Item>();
            items.OnNext(new Item { CategoryId = 1, Name = "First item" });
            items.OnNext(new Item { CategoryId = 1, Name = "Second item" });
            items.OnNext(new Item { CategoryId = 2, Name = "Third  item" });
            items.OnCompleted();

            var categories = new ReplaySubject<Category>();
            categories.OnNext(new Category { Id = 1, Name = "First category" });
            categories.OnCompleted();

            var names = ObservableMaze.Join(items, categories, i => Observable.Return(i.CategoryId), c => Observable.Return(c.Id), (i, c) => i.Name + " from " + c.Name);

            var list = await names.ToArray();

            list.ShouldEqual(new[] { "First item from First category", "Second item from First category" });
        }

        [Fact]
        public async Task execute_join_group()
        {
            var items = new ReplaySubject<Item>();
            items.OnNext(new Item { CategoryId = 1, Name = "First item" });
            items.OnNext(new Item { CategoryId = 1, Name = "Second item" });
            items.OnNext(new Item { CategoryId = 2, Name = "Third  item" });
            items.OnCompleted();

            var categories = new ReplaySubject<Category>();
            categories.OnNext(new Category { Id = 1, Name = "First category" });
            categories.OnCompleted();

            var names = ObservableMaze.GroupJoin(categories, items, c => c.Id, i => i.CategoryId, (c, itms) => itms.Count().Select(x => c.Name + " has " + x + " items"));

            var name = await names.Merge().SingleAsync();

            name.ShouldEqual("First category has 2 items");
        }

        private class Item
        {
            public int CategoryId { get; set; }

            public string Name { get; set; }
        }

        private class Category
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }
    }
}
