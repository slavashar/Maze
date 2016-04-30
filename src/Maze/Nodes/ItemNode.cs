using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Maze.Nodes
{
    public interface ISingleItemDiagramNode
    {
        Node Item { get; }
    }

    public interface IItemNode : ISingleItemDiagramNode
    {
        ItemToken Token { get; }
    }

    public sealed class ItemNode : TokenNode<ItemToken>, IItemNode, INodeContainer
    {
        public ItemNode(ItemToken token, Node item)
            : base (token)
        {
            this.Item = item;
        }

        public override NodeKind Kind
        {
            get { return NodeKind.Item; }
        }

        public Node Item { get; }

        public static ItemNode Create(ItemToken token, Node item)
        {
            return new ItemNode(token, item);
        }

        IEnumerable<Node> INodeContainer.GetNodes()
        {
            yield return this.Item;
        }

        Node INodeContainer.ReplaceNode(Node node, Node replacement)
        {
            if (this.Item != node)
            {
                throw new InvalidOperationException();
            }

            return new ItemNode(this.Token, replacement);
        }
    }

    public sealed class ItemNode<TElement> : TypedNode<TElement>, IItemNode
    {
        private ItemNode(TElement element, ItemToken token, ImmutableDictionary<MemberInfo, ImmutableList<Node>> values) : base(element, values)
        {
            this.Token = token;
        }

        public override NodeKind Kind
        {
            get { return NodeKind.Item; }
        }

        public ItemToken Token { get; }

        public Node Item
        {
            get { return this.values.Values.Single().Single(); }
        }

        public static ItemNode<TElement> Create(TElement element, ItemToken token, Expression<Func<TElement, object>> itemSelector, Node item)
        {
            return new ItemNode<TElement>(element, token, EmptyDictionary.Add(GetMember(itemSelector), ImmutableList.Create(item)));
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
