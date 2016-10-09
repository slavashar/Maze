using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Maze.Facts
{
    public class EnumerableRewriterFacts
    {
        [Fact]
        public void tmp()
        {
            Expression<Func<IQueryable<int>, IQueryable<Dest>>> expr = numbers => from x in numbers select new Dest { Value = x };

            var rewriter = new EnumerableRewriter();

            var result = ((LambdaExpression)rewriter.Visit(expr));

            Expression<Func<IEnumerable<int>, IEnumerable<Dest>>> expected = numbers => from x in numbers select new Dest { Value = x };

            result.ShouldEqual(expected);
        }

        private class Dest
        {
            public int Value { get; set; }
        }
    }
}
