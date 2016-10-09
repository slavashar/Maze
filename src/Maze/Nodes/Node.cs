using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Maze.Nodes
{
    public enum NodeKind
    {
        Empty,

        Text,

        MultiItem,

        Token,

        ElementToken
    }

    public interface ITokenNode : INodeContainer
    {
        Token Token { get; }

        Node this[TokenConnection connection] { get; }
    }

    public interface ITokenNode<TToken> : ITokenNode
        where TToken : Token
    {
        new TToken Token { get; }

        Node WithToken(TToken replacement);
    }

    public interface IElementNode<out TElement> : INodeContainer, ITokenNode
    {
        TElement Element { get; }
    }

    public interface INodeContainer
    {
        IEnumerable<Node> GetNodes();

        Node ReplaceNode(Node item, Node replacement);
    }

    public abstract class Node
    {
        internal Node()
        {
        }

        public abstract NodeKind Kind { get; }

        public static implicit operator Node(string text)
        {
            return NodeFactory.Text(text);
        }
    }
}
