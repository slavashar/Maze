using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Maze.Nodes
{
    public interface IElementNode
    {
        Type ElementType { get; }
    }

    public interface IElementNode<out TElement> : IElementNode
    {
        TElement Element { get; }
    }

    //public interface ITypedNode<out TElement> : IElementNode<TElement>
    //{
    //    Node Get(Expression<Func<TElement, object>> selector);
    //}

    public abstract class TypedNode<TElement> : Node, INodeContainer, IElementNode<TElement>
    {
        protected readonly ImmutableDictionary<MemberInfo, ImmutableList<Node>> values;

        protected TypedNode(TElement element, ImmutableDictionary<MemberInfo, ImmutableList<Node>> values)
        {
            this.Element = element;
            this.values = values;
        }

        public TElement Element { get; }

        Type IElementNode.ElementType
        {
            get { return typeof(TElement); }
        }

        protected static ImmutableDictionary<MemberInfo, ImmutableList<Node>> EmptyDictionary { get; } = ImmutableDictionary<MemberInfo, ImmutableList<Node>>.Empty;

        public abstract ComplexNode<TElement> AddItem(Expression<Func<TElement, object>> selector, Node node);

        public abstract ComplexNode<TElement> AddItems(Expression<Func<TElement, object>> selector, IEnumerable<Node> nodes);

        public Node Get(Expression<Func<TElement, object>> selector)
        {
            return this.values.GetValueOrDefault(GetMember(selector))?.Single();
        }

        public IEnumerable<Node> GetMany(Expression<Func<TElement, object>> selector)
        {
            return this.values.GetValueOrDefault(GetMember(selector));
        }

        protected static MemberInfo GetMember(Expression<Func<TElement, object>> selector)
        {
            return ((MemberExpression)selector.Body).Member;
        }

        IEnumerable<Node> INodeContainer.GetNodes()
        {
            return this.values.Values.SelectMany(x => x).Distinct();
        }

        Node INodeContainer.ReplaceNode(Node node, Node replacement)
        {
            var newvalues = this.values;

            foreach (var tuple in newvalues)
            {
                var list = Replace(tuple.Value, node, replacement);

                if (list != tuple.Value)
                {
                    newvalues = newvalues.SetItem(tuple.Key, list);
                }
            }

            if (newvalues == this.values)
            {
                return this;
            }

            var result = (Node)this.MemberwiseClone();

            var field = result.GetType().GetField(nameof(this.values), BindingFlags.Instance | BindingFlags.NonPublic);

            field.SetValue(result, newvalues);

            return result;
        }

        private static ImmutableList<TItem> Replace<TItem>(ImmutableList<TItem> list, TItem oldValue, TItem newValue)
        {
            var index = 0;

            while ((index = list.IndexOf(oldValue, 0, list.Count, EqualityComparer<TItem>.Default)) >= 0)
            {
                list = list.SetItem(index, newValue);
            }

            return list;
        }
    }
}
