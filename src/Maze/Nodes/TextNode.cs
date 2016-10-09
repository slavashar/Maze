using System;
using System.Collections.Generic;

namespace Maze.Nodes
{
    public sealed class TextNode : Node
    {
        private TextNode(string txt)
        {
            this.Value = txt;
        }

        public override NodeKind Kind => NodeKind.Text;

        public string Value { get; }

        internal static TextNode Create(string txt)
        {
            return new TextNode(txt);
        }
    }
}
