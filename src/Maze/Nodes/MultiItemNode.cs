using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Maze.Nodes
{
    public class MultiItemNode : Node, IEnumerable<Node>, INodeContainer
    {
        private ImmutableList<Node> nodes;

        public MultiItemNode(ImmutableList<Node> nodes)
        {
            this.nodes = nodes;
        }

        public override NodeKind Kind
        {
            get { return NodeKind.MultiItem; }
        }

        public IEnumerator<Node> GetEnumerator()
        {
            return this.nodes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IEnumerable<Node> GetNodes()
        {
            return this.nodes;
        }

        public Node ReplaceNode(Node node, Node replacement)
        {
            var builder = this.nodes.ToBuilder();

            var index = builder.IndexOf(node);

            if (index == -1)
            {
                throw new InvalidOperationException("Node is not found in the collection");
            }

            // todo: can the collection have more than one 

            builder.RemoveAt(index);
            builder.Insert(index, replacement);

            return new MultiItemNode(builder.ToImmutable());
        }

        internal static MultiItemNode Create(IEnumerable<Node> nodes)
        {
            return new MultiItemNode(nodes.ToImmutableList());
        }
    }
}
