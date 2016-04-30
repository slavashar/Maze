using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Maze.Nodes
{
    public interface IBinaryNode
    {
        BinaryToken Token { get; }

        Node Left { get; }

        Node Right { get; }
    }

    public class BinaryNode : TokenNode<BinaryToken>, IBinaryNode, INodeContainer
    {
        protected BinaryNode(BinaryToken token, Node left, Node right) : base(token)
        {
            this.Left = left;
            this.Right = right;
        }

        public override NodeKind Kind
        {
            get { return NodeKind.Binary; }
        }

        public Node Left { get; }

        public Node Right { get; }

        public static BinaryNode Create(BinaryToken token, Node left, Node right)
        {
            return new BinaryNode(token, left, right);
        }

        public BinaryNode Replace(BinaryToken replacementToken)
        {
            return new BinaryNode(replacementToken, this.Left, this.Right);
        }

        IEnumerable<Node> INodeContainer.GetNodes()
        {
            yield return this.Left;

            if (this.Left != this.Right)
            {
                yield return this.Right;
            }
        }

        Node INodeContainer.ReplaceNode(Node node, Node replacement)
        {
            var isleft = this.Left == node;
            var isright = this.Right == node;

            if (!isleft && !isright)
            {
                throw new InvalidOperationException();
            }

            return new BinaryNode(this.Token, isleft ? replacement : this.Left, isright ? replacement : this.Right);
        }
    }

    public sealed class BinaryNode<TElement> : TypedNode<TElement>, IBinaryNode
    {
        private readonly MemberInfo leftMember, rightMember;

        private BinaryNode(TElement element, BinaryToken token, MemberInfo left, MemberInfo right, ImmutableDictionary<MemberInfo, ImmutableList<Node>> values) : base(element, values)
        {
            this.Token = token;
            this.leftMember = left;
            this.rightMember = right;
        }

        public override NodeKind Kind
        {
            get { return NodeKind.Binary; }
        }

        public BinaryToken Token { get; }

        public Node Left
        {
            get { return this.values[this.leftMember].Single(); }
        }

        public Node Right
        {
            get { return this.values[this.rightMember].Single(); }
        }

        public static BinaryNode<TElement> Create(TElement element, BinaryToken token, Expression<Func<TElement, object>> leftSelector, Node left, Expression<Func<TElement, object>> rightSelector, Node right)
        {
            var leftMember = GetMember(leftSelector);
            var rightMember = GetMember(rightSelector);

            return new BinaryNode<TElement>(element, token, leftMember, rightMember, 
                EmptyDictionary.Add(leftMember, ImmutableList.Create(left)).Add(rightMember, ImmutableList.Create(right)));
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
