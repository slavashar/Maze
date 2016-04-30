using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;

namespace Maze.Nodes
{
    public class EmptyNode : TokenNode<Token>
    {
        public override NodeKind Kind
        {
            get { return NodeKind.Empty; }
        }

        public static EmptyNode Create()
        {
            throw new NotImplementedException();
        }

        public static EmptyNode Create(Token token)
        {
            throw new NotImplementedException();
        }
    }

    public class EmptyNode<TElement> : TypedNode<TElement>
    {
        public EmptyNode(TElement element) : base(element, ImmutableDictionary<MemberInfo, ImmutableList<Node>>.Empty)
        {
        }

        public override NodeKind Kind
        {
            get { return NodeKind.Empty; }
        }

        public static EmptyNode<TElement> Create(TElement element)
        {
            return new EmptyNode<TElement>(element);
        }

        public ComplexNode<TElement> AddParent(Expression<Func<TElement, object>> selector, Node node)
        {
            return ComplexNode<TElement>.CreateWithParents(this.Element, selector, ImmutableList.Create(node));
        }

        public ComplexNode<TElement> AddParents(Expression<Func<TElement, object>> selector, IEnumerable<Node> nodes)
        {
            return ComplexNode<TElement>.CreateWithParents(this.Element, selector, nodes);
        }

        public override ComplexNode<TElement> AddItem(Expression<Func<TElement, object>> selector, Node node)
        {
            return ComplexNode<TElement>.CreateWithParents(this.Element, selector, ImmutableList.Create(node));
        }

        public override ComplexNode<TElement> AddItems(Expression<Func<TElement, object>> selector, IEnumerable<Node> nodes)
        {
            return ComplexNode<TElement>.CreateWithParents(this.Element, selector, nodes);
        }
    }
}
