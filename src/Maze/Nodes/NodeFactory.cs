using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Maze.Nodes
{
    public static class NodeFactory
    {
        public static Node Empty { get; } = new EmptyNode();

        public static TextNode Text(string txt)
        {
            if (ReferenceEquals(txt, null))
            {
                throw new ArgumentNullException(nameof(txt));
            }

            return new TextNode(txt);
        }

        public static EmptyNode Node()
        {
            return EmptyNode.Create();
        }

        public static EmptyNode<TElement> Node<TElement>()
        {
            return EmptyNode<TElement>.Create(default(TElement));
        }

        public static EmptyNode<TElement> Node<TElement>(TElement element)
        {
            return EmptyNode<TElement>.Create(element);
        }

        public static EmptyNode Node(Token token)
        {
            return EmptyNode.Create(token);
        }

        public static UnaryNode Node(UnaryToken token, Node parent)
        {
            return UnaryNode.Create(token, parent);
        }

        public static UnaryNode<TElement> Node<TElement>(Expression<Func<TElement, object>> parentSelector, Node parent)
        {
            throw new NotImplementedException();
        }

        public static UnaryNode<TElement> Node<TElement>(TElement element, UnaryToken token, Expression<Func<TElement, object>> parentSelector, Node parent)
        {
            return UnaryNode<TElement>.Create(element, token, parentSelector, parent);
        }

        public static ItemNode ItemNode(ItemToken token, Node item)
        {
            return Nodes.ItemNode.Create(token, item);
        }

        public static ItemNode<TElement> ItemNode<TElement>(Expression<Func<TElement, object>> itemSelector, Node item)
        {
            throw new NotImplementedException();
        }

        public static ItemNode<TElement> ItemNode<TElement>(TElement element, ItemToken token, Expression<Func<TElement, object>> itemSelector, Node item)
        {
            return Nodes.ItemNode<TElement>.Create(element, token, itemSelector, item);
        }

        public static UnaryItemNode Node(UnaryItemToken token, Node parent, Node item)
        {
            return UnaryItemNode.Create(token, parent, item);
        }

        public static UnaryItemNode<TElement> Node<TElement>(TElement element, UnaryItemToken token, Expression<Func<TElement, object>> parentSelector, Node parent, Expression<Func<TElement, object>> itemSelector, Node item)
        {
            return UnaryItemNode<TElement>.Create(element, token, parentSelector, parent, itemSelector, item);
        }

        public static UnaryItemNode<TElement> Node<TElement>(Expression<Func<TElement, object>> parentSelector, Node parent, Expression<Func<TElement, object>> itemSelector, Node item)
        {
            throw new NotImplementedException();
        }

        public static BinaryNode BinaryNode(BinaryToken token, Node left, Node right)
        {
            return Nodes.BinaryNode.Create(token, left, right);
        }

        public static BinaryNode<TElement> BinaryNode<TElement>(TElement element, BinaryToken token, Expression<Func<TElement, object>> leftSelector, Node left, Expression<Func<TElement, object>> rightSelector, Node right)
        {
            return Nodes.BinaryNode<TElement>.Create(element, token, leftSelector, left, rightSelector, right);
        }

        public static UnaryNode Then(this Node parent, UnaryToken token)
        {
            return Node(token, parent);
        }

        public static UnaryItemNode Then(this Node parent, UnaryItemToken token, Node item)
        {
            return Node(token, parent, item);
        }

        public static UnaryNode<TElement> Then<TElement>(this Node parent, Expression<Func<TElement, object>> parentSelection)
        {
            return Node(parentSelection, parent);
        }

        public static UnaryItemNode<TElement> Then<TElement>(
            this Node parent, TElement element, UnaryItemToken token, 
            Expression<Func<TElement, object>> parentSelection, 
            Expression<Func<TElement, object>> itemSelection, Node item)
        {
            return Node(element, token, parentSelection, parent, itemSelection, item);
        }

        public static UnaryItemNode<T> Then<T>(this Node parent, Expression<Func<T, object>> parentSelection, Expression<Func<T, object>> itemSelection, Node item)
        {
            return Node(parentSelection, parent, itemSelection, item);
        }

        public static Node Replace(this Node node, Node replacement)
        {
            throw new NotImplementedException();

            //    if (node is IParentNodeProvider)
            //    {
            //        var parents = ((IParentNodeProvider)node).Parents;

            //        if (parents.Count > 0)
            //        {
            //            if (replacement.Kind == NodeKind.Token)
            //            {
            //                return ((TokenDiagramNode)replacement).AddParents(parents);
            //            }

            //            return Node(Token.Replacement).AddParents(parents).AddItem(replacement);
            //        }
            //    }

            //    return replacement;
        }

        public static TokenNodeSelection<UnaryNode, UnaryToken> Find(this Node node, params UnaryToken[] tokens)
        {
            if (tokens.Length == 0)
            {
                throw new InvalidOperationException();
            }

            if (tokens.Length == 1)
            {
                return TokenNodeSelection<UnaryNode, UnaryToken>.Create(node, tokens[0]);
            }

            return TokenNodeSelection<UnaryNode, UnaryToken>.Create(node, tokens);
        }

        public static TokenNodeSelection<UnaryNode, UnaryToken> Find(this Node node, UnaryToken token, Func<UnaryNode, bool> predicate)
        {
            return TokenNodeSelection<UnaryNode, UnaryToken>.Create(node, token, predicate);
        }

        public static TypedDiagramNodeSelection<T, TypedNode<T>> Find<T>(this Node node)
        {
            return TypedDiagramNodeSelection<T, TypedNode<T>>.Create(node);
        }

        public static TypedDiagramNodeSelection<T, UnaryItemNode<T>> FindUnaryItem<T>(this Node node)
        {
            return TypedDiagramNodeSelection<T, UnaryItemNode<T>>.Create(node);
        }

        public static TypedDiagramNodeSelection<T, TypedNode<T>> Find<T>(this Node node, Func<TypedNode<T>, bool> predicate)
        {
            return TypedDiagramNodeSelection<T, TypedNode<T>>.Create(node, predicate);
        }

        public static TypedDiagramNodeSelection<T, UnaryNode<T>> FindUnary<T>(this Node node, Func<UnaryNode<T>, bool> predicate)
        {
            return TypedDiagramNodeSelection<T, UnaryNode<T>>.Create(node, predicate);
        }

        public static TypedDiagramNodeSelection<T, UnaryItemNode<T>> FindUnaryItem<T>(this Node node, Func<UnaryItemNode<T>, bool> predicate)
        {
            return TypedDiagramNodeSelection<T, UnaryItemNode<T>>.Create(node, predicate);
        }

        public static TypedDiagramNodeSelection<T, BinaryNode<T>> FindBinary<T>(this Node node, Func<BinaryNode<T>, bool> predicate)
        {
            return TypedDiagramNodeSelection<T, BinaryNode<T>>.Create(node, predicate);
        }

        public static TypedDiagramNodeSelection<T, ComplexNode<T>> FindComplex<T>(this Node node, Func<ComplexNode<T>, bool> predicate)
        {
            return TypedDiagramNodeSelection<T, ComplexNode<T>>.Create(node, predicate);
        }

        public abstract class DiagramNodeSelection<TNode>
            where TNode : Node
        {
            protected readonly Node origin;

            public DiagramNodeSelection(Node node, ImmutableHashSet<TNode> nodes)
            {
                this.origin = node;
                this.Nodes = nodes;
            }

            public ImmutableHashSet<TNode> Nodes { get; }

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

            protected static Node Visit(HashSet<Node> set, ISet<TNode> afected, Node node, Func<TNode, Node> visit)
            {
                if (set.Add(node))
                {
                    if (afected.Contains(node))
                    {
                        node = visit((TNode)node);

                        if (node == null)
                        {
                            throw new InvalidOperationException();
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

        public class TokenNodeSelection<TNode, TToken> : DiagramNodeSelection<TNode>
            where TNode : Node, ITokenNode<TToken>
            where TToken : Token
        {
            public TokenNodeSelection(Node node, ImmutableHashSet<TNode> nodes) : base(node, nodes)
            {
            }

            public static TokenNodeSelection<TNode, TToken> Create(Node node, TToken token)
            {
                var set = new HashSet<Node>();
                Flat(set, node);
                return new TokenNodeSelection<TNode, TToken>(node, ImmutableHashSet.CreateRange(set.OfType<TNode>().Where(x => x.Token == token)));
            }

            public static TokenNodeSelection<TNode, TToken> Create(Node node, IEnumerable<TToken> tokens)
            {
                var set = new HashSet<Node>();
                Flat(set, node);
                return new TokenNodeSelection<TNode, TToken>(node, ImmutableHashSet.CreateRange(set.OfType<TNode>().Where(x => tokens.Contains(x.Token))));
            }

            public static TokenNodeSelection<TNode, TToken> Create(Node node, TToken token, Func<TNode, bool> predicate)
            {
                var set = new HashSet<Node>();
                Flat(set, node);
                return new TokenNodeSelection<TNode, TToken>(node, ImmutableHashSet.CreateRange(set.OfType<TNode>().Where(x => x.Token == token).Where(predicate)));
            }

            public Node Change(Func<TNode, Node> visit)
            {
                return Visit(new HashSet<Node>(), this.Nodes, this.origin, visit);
            }
        }

        public class TypedDiagramNodeSelection<TElement, TDiagramNode> : DiagramNodeSelection<TDiagramNode>
            where TDiagramNode : TypedNode<TElement>
        {
            public TypedDiagramNodeSelection(Node node, ImmutableHashSet<TDiagramNode> nodes) : base(node, nodes)
            {
            }

            public static TypedDiagramNodeSelection<TElement, TDiagramNode> Create(Node node)
            {
                var set = new HashSet<Node>();
                Flat(set, node);
                return new TypedDiagramNodeSelection<TElement, TDiagramNode>(node, ImmutableHashSet.CreateRange(set.OfType<TDiagramNode>()));
            }

            internal static TypedDiagramNodeSelection<TElement, TDiagramNode> Create(Node node, Func<TDiagramNode, bool> predicate)
            {
                var set = new HashSet<Node>();
                Flat(set, node);
                return new TypedDiagramNodeSelection<TElement, TDiagramNode>(node, ImmutableHashSet.CreateRange(set.OfType<TDiagramNode>().Where(predicate)));
            }

            public Node Change(Func<TDiagramNode, Node> visit)
            {
                return Visit(new HashSet<Node>(), this.Nodes, this.origin, visit);
            }
        }
    }
}
