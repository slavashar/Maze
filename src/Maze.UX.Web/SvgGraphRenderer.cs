using System;
using System.IO;
using System.Xml;
using Maze.Mappings;
using Maze.Nodes;
using Microsoft.Msagl.Core.Layout;
using MazeNode = Maze.Nodes.Node;
using GeometryNode = Microsoft.Msagl.Core.Layout.Node;

namespace Maze.UX.Svg
{
    public class SvgGraphRenderer
    {
        private readonly double reverseHeight;

        public SvgGraphRenderer(double reverseHeight)
        {
            this.reverseHeight = reverseHeight;
        }

        public virtual void Write(TextWriter writer, GeometryGraph graph)
        {
            using (var xml = XmlWriter.Create(writer, new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = true,
                ConformanceLevel = ConformanceLevel.Fragment,
                CloseOutput = false
            }))
            {
                using (this.WriteBeginGraph(xml, graph))
                {
                    this.WriteStyle(xml);

                    foreach (var edge in graph.Edges)
                    {
                        this.WriteEdge(xml, edge);
                    }

                    foreach (var node in graph.Nodes)
                    {
                        using (this.WriteNode(xml, node))
                        {
                            this.WriteNodeBody(xml, node);
                            this.WriteNodeLabel(xml, node);
                        }
                    }
                }
            }
        }

        public virtual IDisposable WriteBeginGraph(XmlWriter writer, GeometryGraph graph)
        {
            writer.WriteStartElement("svg", "http://www.w3.org/2000/svg");
            writer.WriteAttributeString("xmlns", "xlink", null, "http://www.w3.org/1999/xlink");
            writer.WriteAttributeString("width", (graph.Width + 10).ToString());
            writer.WriteAttributeString("height", (graph.Height + 10).ToString());
            writer.WriteAttributeString("viewBox", string.Join(",", new object[] {
                graph.Left - 5,
                (this.reverseHeight - graph.Top) - 5,
                graph.Width + 10,
                graph.Height + 10 }));

            return new EndWriter(writer);
        }

        public virtual void WriteStyle(XmlWriter writer)
        {
            writer.WriteStartElement("style");
            writer.WriteAttributeString("type", "text/css");

            writer.WriteString("rect {fill: #f7f7f9; stroke-width: 1; stroke: #e1e1e8; border-radius: 4;}");
            writer.WriteString("svg.graph-container.complete rect {fill: #dff0d8;}");
            writer.WriteString("line {stroke-width: 1; fill: none; stroke: #e1e1e8;}");
            writer.WriteString("text {font-family: \"Helvetica Neue\", Helvetica, Arial, sans-serif; font-size: 14px; color: #333;}");

            writer.WriteEndElement();
        }

        public virtual IDisposable WriteNode(XmlWriter writer, GeometryNode geometryNode)
        {
            writer.WriteStartElement("svg");
            writer.WriteAttributeString("x", geometryNode.BoundingBox.Left.ToString());
            writer.WriteAttributeString("y", (this.reverseHeight - geometryNode.BoundingBox.Top).ToString());
            
            return new EndWriter(writer);
        }

        public virtual void WriteNodeBody(XmlWriter writer, GeometryNode geometryNode)
        {
            writer.WriteStartElement("rect");

            writer.WriteAttributeString("width", geometryNode.Width.ToString());
            writer.WriteAttributeString("height", geometryNode.Height.ToString());
            writer.WriteAttributeString("rx", "4");
            writer.WriteAttributeString("ry", "4");

            writer.WriteEndElement();
        }

        public virtual void WriteNodeLabel(XmlWriter writer, GeometryNode geometryNode)
        {
        }

        public virtual void WriteEdge(XmlWriter writer, Edge edge)
        {
            writer.WriteStartElement("line");

            writer.WriteAttributeString("x1", edge.Curve.Start.X.ToString());
            writer.WriteAttributeString("y1", (this.reverseHeight - edge.Curve.Start.Y).ToString());
            writer.WriteAttributeString("x2", edge.Curve.End.X.ToString());
            writer.WriteAttributeString("y2", (this.reverseHeight - edge.Curve.End.Y).ToString());

            writer.WriteEndElement();
        }

        private class EndWriter : IDisposable
        {
            private readonly XmlWriter self;

            public EndWriter(XmlWriter self)
            {
                this.self = self;
            }

            public void Dispose() => this.self.WriteEndElement();
        }
    }
}