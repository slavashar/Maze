using Maze.Nodes;
using Xunit;

namespace Maze.Facts
{
    public class SyntaxFacts
    {
        [Fact]
        public void create_from_text()
        {
            var syntax = SyntaxFactory.FromText("test");

            var result = syntax.Print();

            result.ShouldEqual("test");
        }

        [Fact]
        public void create_from_text_with_margin()
        {
            var syntax = SyntaxFactory.FromText("test", leftMargin: true, rightMargin: true);

            var result = syntax.Print();

            result.ShouldEqual(" test ");
        }

        [Fact]
        public void create_multiple_from_text_with_margin()
        {
            var syntax = SyntaxFactory.FromText("test", rightMargin: true).AddFromText("1");

            var result = syntax.Print();

            result.ShouldEqual("test 1");
        }

        [Fact]
        public void create_multiple_from_text_with_margin_from_both_sides()
        {
            var syntax = SyntaxFactory.FromText("test", rightMargin: true).AddFromText("1", leftMargin: true);

            var result = syntax.Print();

            result.ShouldEqual("test 1");
        }

    }
}
