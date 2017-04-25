using System.Reactive.Linq;
using Maze;
using Xunit;

namespace DataFlow.Tests
{
    public class SimpleTransformationTests
    {
        [Fact]
        public async void execute()
        {
            var result = await Engine.Source(0, 1, 1000).CreateSimpleTransformation().Execute().ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal("Number: 0", result[0]);
            Assert.Equal("Number: 1000", result[1]);
        }
    }
}
