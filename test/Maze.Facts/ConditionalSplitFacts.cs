using System.Linq;
using Maze.Linq;
using Xunit;

namespace Maze.Facts
{
    public class ConditionalSplitFacts
    {
        [Fact(Skip = "not finalized design")]
        public void create_qeryable()
        {
            var items = new[] { 1, 10, 2, 8, 3, 7, 4, 6, 5 }.AsQueryable();

            var q = items.ConditionalSplit(x => x <= 3);

            var result1 = q.Select(x => "first: " + x);
            var result2 = (q = q.ThenSplit(x => x <= 6)).Select(x => "second: " + x);            
            var result3 = q.ThenOther().Select(x => "third: " + x);

            var result = q.MergeSplitResults(result1, result2, result3);
        }

        [Fact]
        public void split()
        {
            var items = new[] { 1, 10, 2, 8, 3, 7, 4, 6, 5 };

            var q = items.ConditionalSplit(x => x <= 3);

            var result1 = q;
            var result2 = (q = q.ThenSplit(x => x <= 6));
            var result3 = q.ThenOther();

            result1.ShouldEqual(1, 2, 3);
            result2.ShouldEqual(4, 6, 5);
            result3.ShouldEqual(10, 8, 7);
        }

        [Fact(Skip = "the implementation is delayed")]
        public void split_with_select()
        {
            var items = new[] { 1, 10, 2, 8, 3, 7, 4, 6, 5 };

            var q = items.ConditionalSplit(x => x <= 3);

            var result1 = q.Select(x => "first: " + x).ToList();
            var result2 = (q = q.ThenSplit(x => x <= 6)).Select(x => "second: " + x).ToList();
            var result3 = q.ThenOther().Select(x => "third: " + x).ToList();

            result1.ShouldEqual("first: 1", "first: 2", "first: 3");
            result2.ShouldEqual("second: 4", "second: 6", "second: 5");
            result3.ShouldEqual("third: 10", "third: 8", "third: 7");
        }

        [Fact(Skip = "the implementation is not feasible")]
        public void merge_back()
        {
            var items = new[] { 1, 10, 2, 8, 3, 7, 4, 6, 5 };

            var q = items.ConditionalSplit(x => x <= 3);

            var result1 = q.Select(x => "first: " + x);
            var result2 = (q = q.ThenSplit(x => x <= 6)).Select(x => "second: " + x);
            var result3 = (q = q.ThenOther()).Select(x => "third: " + x);

            var list = q.MergeSplitResults(result1, result2, result3).ToList();

            //list.ShouldEqual("first: 1", "third: 10", "first: 2", "third: 8", "first: 3", "third: 7", "second: 4", "second: 6", "second: 5");
        }

    }
}
