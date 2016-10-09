using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;

namespace Maze.Nodes
{
    public static class NodeFactory
    {
        public static Node Empty { get; } = EmptyNode.Create();

        public static TextNode Text(string txt)
        {
            if (ReferenceEquals(txt, null))
            {
                throw new ArgumentNullException(nameof(txt));
            }

            return TextNode.Create(txt);
        }

        public static TokenNode<ItemToken> ItemNode(ItemToken token, Node item)
        {
            return TokenNode<ItemToken>.Create(token, ItemToken.Item, item);
        }

        public static ElementNode<TElement, ItemToken> ItemNode<TElement>(TElement element, ItemToken token, Node item)
        {
            return ElementNode<TElement, ItemToken>.Create(element, token, ItemToken.Item, item);
        }

        public static TokenNode<BinaryToken> BinaryNode(BinaryToken token, Node left, Node right)
        {
            return TokenNode<BinaryToken>.Create(token, BinaryToken.Left, left, BinaryToken.Right, right);
        }

        public static ElementNode<TElement, BinaryToken> BinaryNode<TElement>(TElement element, BinaryToken token, Node left, Node right)
        {
            return ElementNode<TElement, BinaryToken>.Create(element, token, BinaryToken.Left, left, BinaryToken.Right, right);
        }

        public static TokenNode<UnaryToken> Then(this Node parent, UnaryToken token)
        {
            return TokenNode<UnaryToken>.Create(token, UnaryToken.Parent, parent);
        }

        public static ElementNode<TElement, UnaryToken> Then<TElement>(this Node parent, TElement element, UnaryToken token)
        {
            return ElementNode<TElement, UnaryToken>.Create(element, token, UnaryToken.Parent, parent);
        }

        public static TokenNode<UnaryItemToken> Then(this Node parent, UnaryItemToken token, Node item)
        {
            return TokenNode<UnaryItemToken>.Create(token, UnaryItemToken.Parent, parent, UnaryItemToken.Item, item);
        }

        public static ElementNode<TElement, UnaryItemToken> Then<TElement>(this Node parent, TElement element, UnaryItemToken token, Node item)
        {
            return ElementNode<TElement, UnaryItemToken>.Create(element, token, UnaryItemToken.Parent, parent, UnaryItemToken.Item, item);
        }

        public static MultiItemNode MultipleItems(params Node[] nodes)
        {
            return MultiItemNode.Create(nodes);
        }

        public static MultiItemNode MultipleItems(IEnumerable<Node> nodes)
        {
            return MultiItemNode.Create(nodes);
        }

        public static Node Get<TElement>(this IElementNode<TElement> node, Expression<Func<TElement, object>> selector)
        {
            return Get((ITokenNode)node, selector);
        }

        public static Node Get<TElement>(this ITokenNode node, Expression<Func<TElement, object>> selector)
        {
            var member = ((MemberExpression)selector.Body).Member;

            var token = node.Token as IElementToken<TElement>;

            var connection = token?.Get(member);

            if (connection == null)
            {
                return null;
            }

            var result = node[connection];

            return result.Kind == NodeKind.MultiItem ? ((MultiItemNode)result).Single() : result;
        }

        public static IEnumerable<Node> GetMany<TElement>(this IElementNode<TElement> node, Expression<Func<TElement, object>> selector)
        {
            return GetMany((ITokenNode)node, selector);
        }

        public static IEnumerable<Node> GetMany<TElement>(this ITokenNode node, Expression<Func<TElement, object>> selector)
        {
            var member = ((MemberExpression)selector.Body).Member;

            var token = node.Token as IElementToken<TElement>;

            var connection = token?.Get(member);

            if (connection == null)
            {
                return null;
            }

            var result = node[connection];

            return result.Kind == NodeKind.MultiItem ? ((MultiItemNode)result) : Linq.Enumerable.Return(result);
        }

        public static Node GetParent(this ITokenNode node)
        {
            return node.Token.Connections
                .Where(x => x.Parent)
                .Select(connection => node[connection])
                .SelectMany(result => result.Kind == NodeKind.MultiItem ? ((MultiItemNode)result) : Linq.Enumerable.Return(result))
                .Single();
        }

        public static IEnumerable<Node> GetParents(this ITokenNode node)
        {
            return node.Token.Connections
                .Where(x => x.Parent)
                .Select(connection => node[connection])
                .SelectMany(result => result.Kind == NodeKind.MultiItem ? ((MultiItemNode)result) : Linq.Enumerable.Return(result));
        }

        public static Node GetItem(this ITokenNode node)
        {
            return node.Token.Connections
                .Where(x => !x.Parent)
                .Select(connection => node[connection])
                .SelectMany(result => result.Kind == NodeKind.MultiItem ? ((MultiItemNode)result) : Linq.Enumerable.Return(result))
                .Single();
        }

        public static IEnumerable<Node> GetItems(this ITokenNode node)
        {
            return node.Token.Connections
                .Where(x => !x.Parent)
                .Select(connection => node[connection])
                .SelectMany(result => result.Kind == NodeKind.MultiItem ? ((MultiItemNode)result) : Linq.Enumerable.Return(result));
        }

        public static ElementNodeBuilder<TElement, TToken> Build<TElement, TToken>(TElement element, TToken token)
            where TToken : Token, IElementToken<TElement>
        {
            return new ElementNodeBuilder<TElement, TToken>(element, token);
        }

        public static TokenNodeSelection<TToken> Find<TToken>(this Node node, TToken token)
            where TToken : Token
        {
            return TokenNodeSelection<TToken>.Create(node, token);
        }

        public static ElementNodeSelection<T> Find<T>(this Node node, Func<IElementNode<T>, bool> predicate)
        {
            return ElementNodeSelection<T>.Create(node, predicate);
        }

        public abstract class BaseNodeSelection<TNode>
        {
            public BaseNodeSelection(Node node, ImmutableHashSet<Node> nodes)
            {
                this.Root = node;
                this.Nodes = nodes;
            }

            public Node Root { get; }

            public ImmutableHashSet<Node> Nodes { get; }

            protected static void Flat(ISet<Node> set, Node node)
            {
                if (set.Add(node))
                {
                    if (node is INodeContainer)
                    {
                        foreach (var item in ((INodeContainer)node).GetNodes())
                        {
                            Flat(set, item);
                        }
                    }
                }
            }

            protected static Node Visit(HashSet<Node> set, ISet<Node> afected, Node node, Func<TNode, Node> visit)
            {
                if (set.Add(node))
                {
                    while (afected.Contains(node))
                    {
                        node = visit((TNode)(dynamic)node);

                        if (node == null)
                        {
                            throw new InvalidOperationException();
                        }

                        if (!set.Add(node))
                        {
                            break;
                        }
                    }

                    if (node is INodeContainer)
                    {
                        foreach (var item in ((INodeContainer)node).GetNodes())
                        {
                            var newitem = Visit(set, afected, item, visit);

                            if (newitem != item)
                            {
                                node = ((INodeContainer)node).ReplaceNode(item, newitem);
                            }
                        }
                    }
                }

                return node;
            }
        }

        public class TokenNodeSelection<TToken> : BaseNodeSelection<ITokenNode<TToken>>
            where TToken : Token
        {
            public TokenNodeSelection(Node node, ImmutableHashSet<Node> nodes)
                : base(node, nodes)
            {
            }

            public Node Change(Func<ITokenNode<TToken>, Node> visit)
            {
                return Visit(new HashSet<Node>(), this.Nodes, this.Root, visit);
            }

            internal static TokenNodeSelection<TToken> Create(Node node, TToken token)
            {
                var set = new HashSet<Node>();
                Flat(set, node);
                return new TokenNodeSelection<TToken>(node, ImmutableHashSet.CreateRange(set.OfType<ITokenNode<TToken>>().Where(x => x.Token == token).Cast<Node>()));
            }
        }

        public class ElementNodeSelection<TElement> : BaseNodeSelection<IElementNode<TElement>>
        {
            public ElementNodeSelection(Node node, ImmutableHashSet<Node> nodes)
                : base(node, nodes)
            {
            }

            public Node Change(Func<IElementNode<TElement>, Node> visit)
            {
                return Visit(new HashSet<Node>(), this.Nodes, this.Root, visit);
            }

            internal static ElementNodeSelection<TElement> Create(Node node, Func<IElementNode<TElement>, bool> predicate)
            {
                var set = new HashSet<Node>();
                Flat(set, node);
                return new ElementNodeSelection<TElement>(node, ImmutableHashSet.CreateRange(set.OfType<IElementNode<TElement>>().Where(predicate).Cast<Node>()));
            }
        }

        public class ElementNodeBuilder<TElement, TToken>
            where TToken : Token, IElementToken<TElement>
        {
            private readonly TElement element;
            private readonly TToken token;
            private readonly ImmutableDictionary<TokenConnection, Node> connections;

            public ElementNodeBuilder(TElement element, TToken token)
            {
                this.element = element;
                this.token = token;
                this.connections = ImmutableDictionary<TokenConnection, Node>.Empty;
            }

            private ElementNodeBuilder(TElement element, TToken token, ImmutableDictionary<TokenConnection, Node> connections)
            {
                this.element = element;
                this.token = token;
                this.connections = connections;
            }

            public static implicit operator ElementNode<TElement, TToken>(ElementNodeBuilder<TElement, TToken> builder)
            {
                return builder.ToNode();
            }

            public ElementNodeBuilder<TElement, TToken> Add(TokenConnection connection, Node node)
            {
                return new ElementNodeBuilder<TElement, TToken>(this.element, this.token, this.connections.Add(connection, node));
            }

            public ElementNodeBuilder<TElement, TToken> Add(Expression<Func<TElement, object>> selector, Node node)
            {
                var member = ((MemberExpression)selector.Body).Member;

                var connection = this.token.Get(member);

                return new ElementNodeBuilder<TElement, TToken>(this.element, this.token, this.connections.Add(connection, node));
            }

            public ElementNode<TElement, TToken> ToNode()
            {
                return ElementNode<TElement, TToken>.Create(this.element, this.token, this.connections);
            }
        }
    }
}
