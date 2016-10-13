using Maze.Nodes;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Miscellaneous;
using GeometryNode = Microsoft.Msagl.Core.Layout.Node;
using GeometryEdge = Microsoft.Msagl.Core.Layout.Edge;
using MazeNode = Maze.Nodes.Node;
using System.Collections.Generic;
using Maze.UX.Web;

namespace Maze.UX
{
    public class GeometryGraphBuilder
    {
        private readonly GraphContext context;

        public GeometryGraphBuilder(GraphContext context)
        {
            this.context = context;
        }

        public GeometryGraph Build(Graph<MazeNode> graph)
        {
            var geometryGraph = new GeometryGraph();

            var dictionary = new Dictionary<MazeNode, GeometryNode>();

            foreach (var node in graph.Nodes)
            {
                var geometryNode = this.CreateNode(node);

                dictionary.Add(node, geometryNode);

                geometryGraph.Nodes.Add(geometryNode);
            }

            foreach (var edge in graph.Edges)
            {
                geometryGraph.Edges.Add(new GeometryEdge(dictionary[edge.Source], dictionary[edge.Target]));
            }

            LayoutHelpers.CalculateLayout(geometryGraph, new SugiyamaLayoutSettings(), null);

            return geometryGraph;
        }

        protected virtual GeometryNode CreateNode(MazeNode node)
        {
            var label = this.CreateLable(node);
            var geometryNode = new GeometryNode(this.CreateNodeGeometry(node, label), node);
            this.context.SetNodeLabel(geometryNode, label);
            return geometryNode;
        }

        protected virtual ICurve CreateNodeGeometry(MazeNode node, string label)
        {
            return CurveFactory.CreateRectangle(new Rectangle(0, 0, 180, 50));
        }

        protected virtual string CreateLable(MazeNode node)
        {
            var call = node as IElementNode<System.Linq.Expressions.MethodCallExpression>;

            if (call != null)
            {
                return call.Get(x => x.Method).Stringify();
            }

            return node.Stringify();
        }

        protected virtual IEnumerable<GeometryEdge> CreateEdges(GeometryNode geometryNode, IEnumerable<GeometryNode> sources)
        {
            foreach (var geometryParentNode in sources)
            {
                yield return new GeometryEdge(geometryParentNode, geometryNode);
            }
        }
    }
}