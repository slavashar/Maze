using System.Linq;
using Maze.Linq;
using Xunit;

namespace Maze.Facts
{
    public class JoinFirstOrDefaultFacts
    {
        [Fact]
        public void crete_quryable()
        {
            var outer = new[] { new { Key = 1, Value = "I" }, new { Key = 2, Value = "II" } }.AsQueryable();
            var inner = new[] { new { Key = 1, Value = "one" }, new { Key = 2, Value = "two" } }.AsQueryable();

            var result = outer.JoinFirstOrDefault(inner, x => x.Key, x => x.Key, (x1, x2) => x1.Value + " is " + ((x2 != null ? x2.Value : null) ?? "missing"));

            result.ShouldNotBeNull();
        }

        [Fact]
        public void join_first_or_dafault()
        {
            var outer = new[] { new { Key = 1, Value = "I" }, new { Key = 2, Value = "II" } };
            var inner = new[] { new { Key = 1, Value = "one" }, new { Key = 2, Value = "two" } };

            var result = outer.JoinFirstOrDefault(inner, x => x.Key, x => x.Key, (x1, x2) => x1.Value + " is " + (x2?.Value ?? "missing"));

            result.ShouldEqual(new[] { "I is one", "II is two" });
        }

        [Fact]
        public void join_first_or_dafault_with_a_missing_value()
        {
            var outer = new[] { new { Key = 1, Value = "I" }, new { Key = 2, Value = "II" }, new { Key = 3, Value = "III" } };
            var inner = new[] { new { Key = 1, Value = "one" }, new { Key = 3, Value = "three" } };

            var result = outer.JoinFirstOrDefault(inner, x => x.Key, x => x.Key, (x1, x2) => x1.Value + " is " + (x2?.Value ?? "missing"));

            result.ShouldEqual(new[] { "I is one", "II is missing", "III is three" });
        }

        [Fact]
        public void join_first_or_dafault_with_a_multiple_values()
        {
            var outer = new[] { new { Key = 1, Value = "I" }, new { Key = 2, Value = "II" } };
            var inner = new[] { new { Key = 1, Value = "one" }, new { Key = 2, Value = "two" }, new { Key = 2, Value = "second" } };

            var result = outer.JoinFirstOrDefault(inner, x => x.Key, x => x.Key, (x1, x2) => x1.Value + " is " + (x2?.Value ?? "missing"));

            result.ShouldEqual(new[] { "I is one", "II is two" });
        }
    }
}
