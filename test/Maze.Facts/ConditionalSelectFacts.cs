using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Maze.Linq;
using Xunit;

namespace Maze.Facts
{
    public class ConditionalSelectFacts
    {
        [Fact]
        public void apply()
        {
            var items = new[] { 1, 10, 2, 8, 3, 7, 4, 6, 5 };

            var result = items
                .ConditionalSelect(x => x <= 3, x => "first: " + x)
                .ThenSelect(x => x <= 6, x => "second: " + x)
                .ThenSelectOthers(x => "third: " + x);

            result.ShouldEqual(
                "first: 1", "third: 10", "first: 2", "third: 8", "first: 3", "third: 7", "second: 4", "second: 6", "second: 5");
        }
    }
}
