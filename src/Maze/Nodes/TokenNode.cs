using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Maze.Nodes
{
    public sealed class TokenNode<TToken> : Node, ITokenNode<TToken>, INodeContainer
        where TToken : Token
    {
        private readonly ImmutableDictionary<TokenConnection, Node> connections;

        private TokenNode(TToken token, ImmutableDictionary<TokenConnection, Node> connections)
        {
            this.Token = token;
            this.connections = connections;
        }

        public override NodeKind Kind => NodeKind.Token;

        public TToken Token { get; }

        Token ITokenNode.Token => this.Token;

        public Node this[TokenConnection connection]
        {
            get { return this.connections[connection]; }
        }

        public TokenNode<TToken> WithToken(TToken replacement)
        {
            return new TokenNode<TToken>(replacement, this.connections);
        }

        Node ITokenNode<TToken>.WithToken(TToken replacement)
        {
            return this.WithToken(replacement);
        }

        public TokenNode<TToken> WithNode(TokenConnection connection, Node node)
        {
            return new TokenNode<TToken>(this.Token, this.connections.SetItem(connection, node));
        }

        public IEnumerable<Node> GetNodes()
        {
            return this.connections.Values.Distinct();
        }

        public Node ReplaceNode(Node node, Node replacement)
        {
            var values = this.connections;

            foreach (var tuple in this.connections)
            {
                if (tuple.Value == node)
                {
                    values = values.SetItem(tuple.Key, replacement);
                }
            }

            return new TokenNode<TToken>(this.Token, values);
        }

        internal static TokenNode<TToken> Create(TToken token)
        {
            return new TokenNode<TToken>(token, ImmutableDictionary<TokenConnection, Node>.Empty);
        }

        internal static TokenNode<TToken> Create(TToken token, TokenConnection connection, Node node)
        {
            return new TokenNode<TToken>(token, ImmutableDictionary<TokenConnection, Node>.Empty.Add(connection, node));
        }

        internal static TokenNode<TToken> Create(TToken token, TokenConnection connection1, Node node1, TokenConnection connection2, Node node2)
        {
            return new TokenNode<TToken>(token, ImmutableDictionary<TokenConnection, Node>.Empty.Add(connection1, node1).Add(connection2, node2));
        }
    }
}
