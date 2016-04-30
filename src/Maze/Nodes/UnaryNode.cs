using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Maze.Nodes
{
    public interface ISingleParentDiagramNode
    {
        Node Parent { get; }
    }

    public interface IUnaryNode : ISingleParentDiagramNode
    {
        UnaryToken Token { get; }
    }

    public class UnaryNode : TokenNode<UnaryToken>, IUnaryNode, INodeContainer
    {
        protected UnaryNode(UnaryToken token, Node parent) : base (token)
        {
            this.Parent = parent;
        }

        public override NodeKind Kind
        {
            get { return NodeKind.Unary; }
        }

        public Node Parent { get; }

        public static UnaryNode Create(UnaryToken token, Node parent)
        {
            return new UnaryNode(token, parent);
        }

        IEnumerable<Node> INodeContainer.GetNodes()
        {
            yield return this.Parent;
        }

        Node INodeContainer.ReplaceNode(Node node, Node replacement)
        {
            if (this.Parent != node)
            {
                throw new InvalidOperationException();
            }

            return new UnaryNode(this.Token, replacement);
        }
    }

    public sealed class UnaryNode<TElement> : TypedNode<TElement>, IUnaryNode
    {
        private UnaryNode(TElement element, UnaryToken token, ImmutableDictionary<MemberInfo, ImmutableList<Node>> values) : base(element, values)
        {
            this.Token = token;
        }

        public override NodeKind Kind
        {
            get { return NodeKind.Unary; }
        }

        public UnaryToken Token { get; }

        public Node Parent
        {
            get { return this.values.Values.Single().Single(); }
        }

        public static UnaryNode<TElement> Create(TElement element, UnaryToken token, Expression<Func<TElement, object>> parentSelector, Node parent)
        {
            return new UnaryNode<TElement>(element, token, EmptyDictionary.Add(GetMember(parentSelector), ImmutableList.Create(parent)));
        }

        public override ComplexNode<TElement> AddItem(Expression<Func<TElement, object>> selector, Node node)
        {
            return ComplexNode<TElement>.CreateWith(this.Element, values.Add(GetMember(selector), ImmutableList.Create(node)));
        }

        public override ComplexNode<TElement> AddItems(Expression<Func<TElement, object>> selector, IEnumerable<Node> nodes)
        {
            return ComplexNode<TElement>.CreateWith(this.Element, values.Add(GetMember(selector), ImmutableList.CreateRange(nodes)));
        }
    }
}
