using Maze.Nodes;
using Xunit;

namespace Maze.Facts
{
    public class NodeGraphFacts
    {
        [Fact]
        public void create_graph_from_single_node()
        {
            var node = NodeFactory.ItemNode(new ItemToken(), "single");

            var graph = node.ToGraph();

            graph.Nodes.ShouldEqual(node);
        }

        [Fact]
        public void create_graph_from_chain()
        {
            var parent = NodeFactory.ItemNode(new ItemToken(), "parent");
            var node = parent.Then(new UnaryToken());

            var graph = node.ToGraph();

            graph.Nodes.ShouldEqual(parent, node);
            graph.Edges.ShouldEqual(new Graph<Node>.Edge(parent, node));
        }

        [Fact]
        public void create_graph_from_chain_with_items()
        {
            var parent = NodeFactory.ItemNode(new ItemToken(), "parent");
            var node = parent.Then(new UnaryItemToken(), "center");

            var graph = node.ToGraph();

            graph.Nodes.ShouldEqual(parent, node);
            graph.Edges.ShouldEqual(new Graph<Node>.Edge(parent, node));
        }

        [Fact]
        public void create_graph_from_star()
        {
            var parent = NodeFactory.ItemNode(new ItemToken(), "parent");
            var child1 = parent.Then(new UnaryToken());
            var child2 = parent.Then(new UnaryToken());
            var node = NodeFactory.BinaryNode(new BinaryToken(), child1, child2);

            var graph = node.ToGraph();

            graph.Nodes.ShouldEqual(parent, child1, child2, node);
            graph.Edges.ShouldEqual(
                new Graph<Node>.Edge(parent, child1),
                new Graph<Node>.Edge(parent, child2),
                new Graph<Node>.Edge(child1, node),
                new Graph<Node>.Edge(child2, node));
        }
    }
}
