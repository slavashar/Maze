using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Maze.Nodes
{
    public interface IUnaryItemNode : ISingleParentDiagramNode, ISingleItemDiagramNode
    {
        UnaryItemToken Token { get; }
    }

    public sealed class UnaryItemNode : TokenNode<UnaryItemToken>, IUnaryItemNode, INodeContainer
    {
        private UnaryItemNode(UnaryItemToken token, Node parent, Node item) : base (token)
        {
            this.Parent = parent;
            this.Item = item;
        }

        public override NodeKind Kind
        {
            get { return NodeKind.UnaryItem; }
        }

        public Node Parent { get; }

        public Node Item { get; }

        public UnaryNode Replace(UnaryToken brackets)
        {
            return UnaryNode.Create(brackets, this.Parent);
        }

        internal static UnaryItemNode Create(UnaryItemToken token, Node parent, Node item)
        {
            return new UnaryItemNode(token, parent, item);
        }

        IEnumerable<Node> INodeContainer.GetNodes()
        {
            yield return this.Item;

            if (this.Item != this.Parent)
            {
                yield return this.Parent;
            }
        }

        Node INodeContainer.ReplaceNode(Node node, Node replacement)
        {
            var isitem = this.Item == node;
            var isparent = this.Parent == node;

            if (!isitem && !isparent)
            {
                throw new InvalidOperationException();
            }

            return new UnaryItemNode(this.Token, isparent ? replacement : this.Parent, isitem ? replacement : this.Item);
        }
    }

    public sealed class UnaryItemNode<TElement> : TypedNode<TElement>, IUnaryItemNode
    {
        private readonly MemberInfo parentMember, itemMember;

        private UnaryItemNode(TElement element, UnaryItemToken token, MemberInfo parent, MemberInfo item, ImmutableDictionary<MemberInfo, ImmutableList<Node>> values) : base(element, values)
        {
            this.Token = token;
            this.parentMember = parent;
            this.itemMember = item;
        }

        public override NodeKind Kind
        {
            get { return NodeKind.UnaryItem; }
        }

        public UnaryItemToken Token { get; }

        public Node Parent
        {
            get { return this.values[this.parentMember].Single(); }
        }

        public Node Item
        {
            get { return this.values[this.itemMember].Single(); }
        }

        public static UnaryItemNode<TElement> Create(TElement element, UnaryItemToken token, Expression<Func<TElement, object>> parentSelector, Node parent, Expression<Func<TElement, object>> itemSelector, Node item)
        {
            var parentMember = GetMember(parentSelector);
            var itemMember = GetMember(itemSelector);
            
            return new UnaryItemNode<TElement>(element, token, parentMember, itemMember,
                EmptyDictionary.Add(parentMember, ImmutableList.Create(parent)).Add(itemMember, ImmutableList.Create(item)));
        }

        public override ComplexNode<TElement> AddItem(Expression<Func<TElement, object>> selector, Node node)
        {
            return ComplexNode<TElement>.CreateWith(this.Element, this.values.Add(GetMember(selector), ImmutableList.Create(node)));
        }

        public override ComplexNode<TElement> AddItems(Expression<Func<TElement, object>> selector, IEnumerable<Node> nodes)
        {
            return ComplexNode<TElement>.CreateWith(this.Element, this.values.Add(GetMember(selector), nodes.ToImmutableList()));
        }
    }
}
