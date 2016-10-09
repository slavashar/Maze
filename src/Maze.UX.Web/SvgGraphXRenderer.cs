using Microsoft.Msagl.Core.Layout;
using GeometryNode = Microsoft.Msagl.Core.Layout.Node;
using System.Xml.Linq;

namespace Maze.UX.Svg
{
    public class SvgGraphXRenderer
    {
        public static readonly XNamespace SvgNamespace = XNamespace.Get("http://www.w3.org/2000/svg");

        private readonly GeometryGraph graph;

        public SvgGraphXRenderer(GeometryGraph graph)
        {
            this.graph = graph;
        }

        public virtual XDocument CreateDocument()
        {
            var document = new XDocument(
                this.CreateRootElement(this.graph));

            document.Root.Add(this.CreateStyleElement());

            foreach (var edge in this.graph.Edges)
            {
                document.Root.Add(this.CreateEdgeElement(edge));
            }

            foreach (var node in this.graph.Nodes)
            {
                document.Root.Add(this.CreateNodeElement(node));
            }

            return document;
        }

        protected virtual XElement CreateRootElement(GeometryGraph graph)
        {
            return new XElement(
                SvgNamespace.GetName("svg"),
                new XAttribute("width", (graph.Width + 10).ToString()),
                new XAttribute("height", (graph.Height + 10).ToString()),
                new XAttribute("viewBox", string.Join(",", new object[] {
                    graph.Left - 5,
                    -5,
                    graph.Width + 10,
                    graph.Height + 10 })));
        }

        protected virtual XElement CreateStyleElement()
        {
            return new XElement(
                "style",
                new XAttribute("type", "text/css"),
                new XCData("rect {fill: #f7f7f9; stroke-width: 1; stroke: #e1e1e8; border-radius: 4;}"),
                new XCData("svg.graph-container.complete rect {fill: #dff0d8;}"),
                new XCData("line {stroke-width: 1; fill: none; stroke: #e1e1e8;}"),
                new XCData("text {font-family: \"Helvetica Neue\", Helvetica, Arial, sans-serif; font-size: 14px; color: #333;}"));
        }

        protected virtual XElement CreateNodeElement(GeometryNode geometryNode)
        {
            var element = new XElement(
                SvgNamespace.GetName("svg"),
                new XAttribute("x", geometryNode.BoundingBox.Left.ToString()),
                new XAttribute("y", (this.graph.Top - geometryNode.BoundingBox.Top).ToString()));

            element.Add(new XElement(
                SvgNamespace.GetName("rect"),
                new XAttribute("width", geometryNode.Width.ToString()),
                new XAttribute("height", geometryNode.Height.ToString()),
                new XAttribute("rx", "4"),
                new XAttribute("ry", "4")));

            return element;
        }

        protected virtual XElement CreateEdgeElement(Edge edge)
        {
            return new XElement(
                SvgNamespace.GetName("line"),
                new XAttribute("x1", edge.Curve.Start.X.ToString()),
                new XAttribute("y1", (this.graph.Top - edge.Curve.Start.Y).ToString()),
                new XAttribute("x2", edge.Curve.End.X.ToString()),
                new XAttribute("y2", (this.graph.Top - edge.Curve.End.Y).ToString()));
        }
    }
}