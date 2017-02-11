using System.Reactive.Linq;
using Maze;
using Xunit;

namespace DataFlow.Tests
{
    public class SimpleMapTests
    {
        [Fact]
        public async void execute_when_the_source_is_0()
        {
            var result = await Engine.Source(0).CreateSimpleMapping().Execute();

            Assert.Equal("Number: 0", result);
        }

        [Fact]
        public async void execute_when_the_source_is_1()
        {
            var result = await Engine.Source(1).CreateSimpleMapping().Execute().Any();

            Assert.False(result);
        }

        [Fact]
        public async void execute_when_the_source_is_1000()
        {
            var result = await Engine.Source(1000).CreateSimpleMapping().Execute();

            Assert.Equal("Number: 1000", result);
        }
    }
}
