using System.Collections.Immutable;
using System.Reflection;

namespace Maze.Nodes
{
    public sealed class TextNode : Node
    {
        internal TextNode(string txt)
        {
            this.Value = txt;
        }

        public override NodeKind Kind
        {
            get { return NodeKind.Text; }
        }

        public string Value { get; }
    }
}
