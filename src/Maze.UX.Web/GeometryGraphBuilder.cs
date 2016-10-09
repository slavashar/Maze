using System;
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

namespace Maze.UX
{
    public static class GeometryGraphBuilderExtension
    {
        public static GeometryGraph BuildGeometryGraph(this MazeNode node)
        {
            var build = new GeometryGraphBuilder();

            return build.Build(node);
        }
    }

    public class GeometryGraphBuilder
    {
        public GeometryGraph Build(MazeNode node)
        {
            var geometryGraph = new GeometryGraph();

            this.AddNode(geometryGraph, node, geometryParentNode => { });

            LayoutHelpers.CalculateLayout(geometryGraph, new SugiyamaLayoutSettings(), null);

            return geometryGraph;
        }

        public void AddNode(GeometryGraph geometryGraph, MazeNode node, Action<GeometryNode> add)
        {
            if (node.Kind == NodeKind.MultiItem)
            {
                foreach (var parent in ((MultiItemNode)node))
                {
                    this.AddNode(geometryGraph, parent, add);
                }

                return;
            }

            var geometryNode = this.CreateNode(node);
            add(geometryNode);

            geometryGraph.Nodes.Add(geometryNode);

            var sources = new List<GeometryNode>();

            if (node is ITokenNode)
            {
                foreach (var parent in ((ITokenNode)node).GetParents())
                {
                    this.AddNode(geometryGraph, parent, sources.Add);
                }
            }

            foreach (var edge in this.CreateEdges(geometryNode, sources))
            {
                geometryGraph.Edges.Add(edge);
            }
        }

        public virtual GeometryNode CreateNode(MazeNode node)
        {
            return new GeometryNode(CurveFactory.CreateRectangle(new Rectangle(0, 0, 180, 50)), node);
        }

        public virtual IEnumerable<GeometryEdge> CreateEdges(GeometryNode geometryNode, IEnumerable<GeometryNode> sources)
        {
            foreach (var geometryParentNode in sources)
            {
                yield return new GeometryEdge(geometryParentNode, geometryNode);
            }
        }
    }
}