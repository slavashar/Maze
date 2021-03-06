﻿using Maze.Mappings;
using Maze.Nodes;
using Xunit;

namespace Maze.Facts
{
    public class MappingNodeBuilderFacts
    {
        [Fact]
        public void parse_a_source_node()
        {
            var parser = new MappingNodeBuilder();

            var mapping = Engine.Source("source", new[] { 1, 2, 3 });

            var node = parser.Build(mapping.Container).ShouldBeType<ElementNode<IMapping, MappingToken>>();

            node.Token.ShouldBe(MappingTokens.Node);
            node.Stringify().ShouldEqual("[source]");
        }

        [Fact]
        public void parse_a_child_node()
        {
            var parser = new MappingNodeBuilder();

            var mapping = Engine
                .Source("source", new[] { 1, 2, 3 })
                .Map("child", x => x);

            var node = parser.Build(mapping.Container).ShouldBeType<ElementNode<IMapping, UnaryItemToken>>();

            node.Token.ShouldBe(MappingTokens.Transformation);

            node.Stringify().ShouldEqual("[source]->[child]");
        }

        [Fact]
        public void parse_a_second_child_node()
        {
            var parser = new MappingNodeBuilder();

            var mapping = Engine
                .Source("source", new[] { 1, 2, 3 })
                .Map("child 1", x => x)
                .Map("child 2", x => x);

            var node = parser.Build(mapping.Container).ShouldBeType<ElementNode<IMapping, UnaryItemToken>>();

            node.Token.ShouldBe(MappingTokens.Transformation);

            node[UnaryItemToken.Item].Stringify().ShouldEqual("[child 2]");
            node.Stringify().ShouldEqual("[source]->[child 1]->[child 2]");
        }

        [Fact]
        public void parse_a_composit_node()
        {
            var parser = new MappingNodeBuilder();

            var mapping = Engine.Combine(
                Engine.Source("source 1", new[] { 1, 2, 3 }).Map("child 1", x => x),
                Engine.Source("source 2", new[] { 1, 2, 3 }).Map("child 2", x => x)
                )
                .Map("child 3", c => c.First ?? c.Second);

            var node = parser.Build(mapping.Container);
            
            node.Stringify().ShouldEqual("[source]->[child 1]->[child 2]");
        }
    }
}
