using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Xunit;

namespace Maze.Facts
{
    public class EngineFacts
    {
        [Fact]
        public async Task execute_a_simple_mapping()
        {
            var engine = Engine
                .Map((IQueryable<Source> src) => from x in src select new Dest { Value = x.Value * 10 });

            var result = await engine.Execute(new Source { Value = 2 });

            result.Value.ShouldEqual(20);
        }

        [Fact]
        public async Task execute_a_source_mapping()
        {
            var engine = Engine.Source(new[] { 1, 2, 3 });

            var result = await engine.Execute().FirstAsync();

            result.ShouldEqual(1);
        }

        [Fact]
        public void execute_a_source_mapping_async()
        {
            var subject = new Subject<int>();

            var engine = Engine.Source(subject.ToEnumerable());
            
            var result = engine.Execute().FirstAsync().ToTask();

            result.Wait(100).ShouldBeFalse();

            subject.OnNext(1);

            result.Wait(100).ShouldBeTrue();
        }

        [Fact]
        public async Task execute_a_chain_of_mappings()
        {
            var engine = Engine
                .Source(new[] { 1, 2, 3 })
                .Map(src => from x in src select new Dest { Value = x * 10 });

            var result = await engine.Execute().FirstAsync();

            result.Value.ShouldEqual(10);
        }

        [Fact]
        public async Task execute_combined_mapping()
        {
            var engine = Engine.Combine(
                Engine.Source(new[] { 1, 2, 3 }),
                Engine.Map((IQueryable<int> src) => from x in src select new Dest { Value = x * 10 }));

            var result = await engine.Get<Dest>().Execute().FirstAsync();

            result.Value.ShouldEqual(10);
        }

        [Fact]
        public async Task execute_a_chain_of_mappings_with_multiple_sources()
        {
            var engine = Engine.Combine(
                Engine.Source(new[] { 1, 2, 3 }),
                Engine.Source(new[] { 4, 5, 6 })
                    .Map(src => from x in src select new Dest { Value = x * 10 }));

            var result = await engine.Get<Dest>().Execute().FirstAsync();

            result.Value.ShouldEqual(40);
        }

        [Fact]
        public async Task execute_a_simple_component()
        {
            var engine = Engine.MapComponent((IQueryable<Source> src) => new
            {
                Dest = from x in src select new Dest { Value = x.Value * 10 }
            });

            var result = await engine.Get(x => x.Dest).Execute(new Source { Value = 1 }, new Source { Value = 2 }).FirstAsync();

            result.Value.ShouldEqual(10);
        }

        [Fact]
        public async Task execute_a_source_component()
        {
            var engine = Engine.CreateComponent(() => new
            {
                Items = new[] { 1, 2, 3 }.AsQueryable()
            });

            var result = await engine.Get(x => x.Items).Execute().FirstAsync();

            result.ShouldEqual(1);
        }

        [Fact]
        public async Task execute_a_chain_of_components()
        {
            var engine = Engine
                .CreateComponent(() => new
                {
                    Items = new[] { 1, 2, 3 }.AsQueryable()
                })
                .MapComponent(src => new
                {
                    Dest = from x in src.Items select new Dest { Value = x * 10 }
                });

            var result = await engine.Get(x => x.Dest).Execute().FirstAsync();

            result.Value.ShouldEqual(10);
        }

        [Fact]
        public async Task execute_a_concrete_mapping()
        {
            var engine = Engine
                .CreateComponent(() => new
                {
                    Items = new[] { new Source { Value = 1 } }.AsQueryable()
                })
                .MapComponent(src => new DestMapping(src.Items));

            var result = await engine.Get(x => x.Result).Execute().SingleAsync();

            result.Value.ShouldEqual(10);
        }

        [Fact]
        public async Task execute_a_chain_of_concrete_mappings()
        {
            var engine = Engine
                .CreateComponent(() => new
                {
                    Items = new[] { new Source { Value = 1 } }.AsQueryable()
                })
                .MapComponent(src => new DestMapping(src.Items))
                .MapComponent(src => new DestMapping2(src.Result));

            var result = await engine.Get(x => x.Result).Execute().SingleAsync();

            result.Value.ShouldEqual(1);
        }

        private class Source
        {
            public int Value { get; set; }
        }

        private class Dest
        {
            public int Value { get; set; }
        }

        private class DestMapping
        {
            private readonly IQueryable<Source> input;

            public DestMapping(IQueryable<Source> input)
            {
                this.input = input;
            }

            public IQueryable<Dest> Result
            {
                get { return from x in this.input select new Dest { Value = x.Value * 10 }; }
            }
        }

        private class DestMapping2
        {
            private readonly IQueryable<Dest> input;

            public DestMapping2(IQueryable<Dest> input)
            {
                this.input = input;
            }

            public IQueryable<Dest> Result
            {
                get { return from x in this.input select new Dest { Value = x.Value / 10 }; }
            }
        }
    }
}