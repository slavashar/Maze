using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Maze.Nodes
{
    public abstract class ElementNode<TElement> : Node, ITokenNode, IElementNode<TElement>
    {
        protected ElementNode(TElement element)
        {
            this.Element = element;
        }

        public override NodeKind Kind => NodeKind.ElementToken;

        public TElement Element { get; }

        Token ITokenNode.Token => this.GetToken();

        public abstract Node this[TokenConnection connection] { get; }

        public abstract Node ReplaceNode(Node node, Node replacement);

        public abstract IEnumerable<Node> GetNodes();

        protected abstract Token GetToken();
    }

    public sealed class ElementNode<TElement, TToken> : ElementNode<TElement>, ITokenNode<TToken>
        where TToken : Token
    {
        private readonly ImmutableDictionary<TokenConnection, Node> connections;

        private ElementNode(TElement element, TToken token, ImmutableDictionary<TokenConnection, Node> connections)
            : base(element)
        {
            this.Token = token;
            this.connections = connections;
        }

        public TToken Token { get; }

        public override Node this[TokenConnection connection] => this.connections[connection];

        public ElementNode<TElement, TToken> WithToken(TToken replacement)
        {
            return new ElementNode<TElement, TToken>(this.Element, replacement, this.connections);
        }

        Node ITokenNode<TToken>.WithToken(TToken replacement)
        {
            return this.WithToken(replacement);
        }

        public ElementNode<TElement, TToken> WithNode(TokenConnection connection, Node node)
        {
            return new ElementNode<TElement, TToken>(this.Element, this.Token, this.connections.SetItem(connection, node));
        }

        public override IEnumerable<Node> GetNodes()
        {
            return this.connections.Values.Distinct();
        }

        public override Node ReplaceNode(Node node, Node replacement)
        {
            var values = this.connections;

            foreach (var tuple in this.connections)
            {
                if (tuple.Value == node)
                {
                    values = values.SetItem(tuple.Key, replacement);
                }
            }

            return new ElementNode<TElement, TToken>(this.Element, this.Token, values);
        }

        internal static ElementNode<TElement, TToken> Create(TElement element, TToken token)
        {
            return new ElementNode<TElement, TToken>(element, token, ImmutableDictionary<TokenConnection, Node>.Empty);
        }

        internal static ElementNode<TElement, TToken> Create(TElement element, TToken token, TokenConnection connection, Node node)
        {
            return new ElementNode<TElement, TToken>(element, token, ImmutableDictionary<TokenConnection, Node>.Empty.Add(connection, node));
        }

        internal static ElementNode<TElement, TToken> Create(TElement element, TToken token, TokenConnection connection1, Node node1, TokenConnection connection2, Node node2)
        {
            return new ElementNode<TElement, TToken>(element, token, ImmutableDictionary<TokenConnection, Node>.Empty.Add(connection1, node1).Add(connection2, node2));
        }

        internal static ElementNode<TElement, TToken> Create(TElement element, TToken token, ImmutableDictionary<TokenConnection, Node> connections)
        {
            return new ElementNode<TElement, TToken>(element, token, connections);
        }

        protected override Token GetToken() => this.Token;
    }
}
