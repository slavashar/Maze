using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Maze.Mappings;
using Maze.Nodes;
using Xunit;

namespace Maze.Facts
{
    public class MappingNodeParserFacts
    {
        [Fact]
        public void parse_a_source_node()
        {
            var parser = new MappingNodeParser();

            var mapping = Engine.Source("source", new[] { 1, 2, 3 });

            var node = parser.Visit(mapping.Container).ShouldBeType<ElementNode<IMapping, ItemToken>>();

            node.Token.ShouldBe(MappingTokens.Node);
            node.Stringify().ShouldEqual("[source]");
        }

        [Fact]
        public void parse_a_child_node()
        {
            var parser = new MappingNodeParser();

            var mapping = Engine
                .Source("source", new[] { 1, 2, 3 })
                .Map("child", x => x);

            var node = parser.Visit(mapping.Container).ShouldBeType<ElementNode<IMapping, UnaryItemToken>>();

            node.Token.ShouldBe(MappingTokens.Transformation);

            node.Stringify().ShouldEqual("[source]->[child]");
        }

        [Fact]
        public void parse_a_second_child_node()
        {
            var parser = new MappingNodeParser();

            var mapping = Engine
                .Source("source", new[] { 1, 2, 3 })
                .Map("child 1", x => x)
                .Map("child 2", x => x);

            var node = parser.Visit(mapping.Container).ShouldBeType<ElementNode<IMapping, UnaryItemToken>>();

            node.Token.ShouldBe(MappingTokens.Transformation);

            node[UnaryItemToken.Item].Stringify().ShouldEqual("[child 2]");
            node.Stringify().ShouldEqual("[source]->[child 1]->[child 2]");
        }
    }
}
