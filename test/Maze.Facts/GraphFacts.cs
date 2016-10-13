using Xunit;

namespace Maze
{
    public class GraphFacts
    {
        [Fact]
        public void create_a_graph()
        {
            var graph = Graph<object>.Empty;

            graph.ShouldNotBeNull();
        }

        [Fact]
        public void crate_a_node()
        {
            var node = new object();

            var graph = Graph
                .CreateNode(node);

            graph.Nodes.ShouldEqual(node);
        }

        [Fact]
        public void create_a_edge()
        {
            object sourceNode = new object(), targetNode = new object();

            var graph = Graph
                .CreateNode(sourceNode)
                .CreateNode(targetNode)
                .CreateEdge(sourceNode, targetNode);

            graph.Edges.ShouldEqual(new Graph<object>.Edge(sourceNode, targetNode));
        }

        [Fact]
        public void create_a_duplicated_edge()
        {
            object sourceNode = new object(), targetNode = new object();

            var graph = Graph
                .CreateNode(sourceNode)
                .CreateNode(targetNode)
                .CreateEdge(sourceNode, targetNode)
                .CreateEdge(sourceNode, targetNode);

            graph.Edges.ShouldEqual(new Graph<object>.Edge(sourceNode, targetNode));
        }
    }
}
