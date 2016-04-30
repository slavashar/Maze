namespace Maze.Nodes
{
    public enum NodeKind
    {
        Empty,

        Text,

        Unary,

        Item,

        UnaryItem,

        Binary,

        Complex
    }

    public interface ITokenNode<TToken>
    {
        TToken Token { get; }
    }

    public abstract class Node
    {
        protected Node()
        {
        }

        public abstract NodeKind Kind { get; }

        public static implicit operator Node(string text)
        {
            return NodeFactory.Text(text);
        }
    }

    public abstract class TokenNode<TToken> : Node, ITokenNode<TToken>
        where TToken : Token
    {
        protected TokenNode()
        {
        }

        protected TokenNode(TToken token)
        {
            this.Token = token;
        }

        public TToken Token { get; }
    }
}
