using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Maze.Nodes
{
    public interface IComplexNode
    {

    }

    public sealed class ComplexNode<TElement> : TypedNode<TElement>, IComplexNode
    {
        private ComplexNode(TElement element, ImmutableDictionary<MemberInfo, ImmutableList<Node>> values) : base(element, values)
        {
        }

        public override NodeKind Kind
        {
            get { return NodeKind.Complex; }
        }

        public static ComplexNode<TElement> CreateWithParents(TElement element, Expression<Func<TElement, object>> selector, IEnumerable<Node> node)
        {
            return new ComplexNode<TElement>(element, ImmutableDictionary<MemberInfo, ImmutableList<Node>>.Empty.Add(GetMember(selector), node.ToImmutableList()));
        }

        public static ComplexNode<TElement> CreateWithItems(TElement element, Expression<Func<TElement, object>> selector, IEnumerable<Node> node)
        {
            return new ComplexNode<TElement>(element, ImmutableDictionary<MemberInfo, ImmutableList<Node>>.Empty.Add(GetMember(selector), node.ToImmutableList()));
        }

        internal static ComplexNode<TElement> CreateWith(TElement element, ImmutableDictionary<MemberInfo, ImmutableList<Node>> values)
        {
            return new ComplexNode<TElement>(element, values);
        }

        public ComplexNode<TElement> AddParent(Expression<Func<TElement, object>> selector, Node node)
        {
            return new ComplexNode<TElement>(this.Element, values.Add(GetMember(selector), ImmutableList.Create(node)));
        }

        public ComplexNode<TElement> AddParents(Expression<Func<TElement, object>> selector, IEnumerable<Node> nodes)
        {
            return new ComplexNode<TElement>(this.Element, values.Add(GetMember(selector), ImmutableList.CreateRange(nodes)));
        }

        public override ComplexNode<TElement> AddItem(Expression<Func<TElement, object>> selector, Node node)
        {
            return new ComplexNode<TElement>(this.Element, values.Add(GetMember(selector), ImmutableList.Create(node)));
        }

        public override ComplexNode<TElement> AddItems(Expression<Func<TElement, object>> selector, IEnumerable<Node> nodes)
        {
            return new ComplexNode<TElement>(this.Element, values.Add(GetMember(selector), ImmutableList.CreateRange(nodes)));
        }
    }
}
