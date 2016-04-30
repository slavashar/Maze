using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Maze
{
    public static class Graph
    {
        public static Graph<TNode> CreateNode<TNode>(TNode node)
        {
            return Graph<TNode>.Empty.CreateNode(node);
        }
    }

    public sealed class Graph<TNode>
    {
        private static readonly Graph<TNode> EmptyGraph = new Graph<TNode>(ImmutableHashSet<TNode>.Empty, ImmutableHashSet<Edge>.Empty);

        private readonly ImmutableHashSet<TNode> nodes;

        private readonly ImmutableHashSet<Edge> edges;

        private Graph(ImmutableHashSet<TNode> nodes, ImmutableHashSet<Edge> edges)
        {
            this.nodes = nodes;
            this.edges = edges;
        }

        public static Graph<TNode> Empty
        {
            get { return EmptyGraph; }
        }

        public IEnumerable<TNode> Nodes
        {
            get { return this.nodes; }
        }

        public IEnumerable<Edge> Edges
        {
            get { return this.edges; }
        }

        public Graph<TNode> CreateNode(TNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("Node can not be null");
            }

            if (this.nodes.Contains(node))
            {
                return this;
            }

            return new Graph<TNode>(this.nodes.Add(node), this.edges);
        }

        public Graph<TNode> CreateEdge(TNode source, TNode target)
        {
            if (!this.nodes.Contains(source))
            {
                throw new InvalidOperationException("Invalid origin of source node");
            }

            if (!this.nodes.Contains(target))
            {
                throw new InvalidOperationException("Invalid origin of target node");
            }

            var edge = new Edge(source, target);

            if (this.edges.Contains(edge))
            {
                return this;
            }

            return new Graph<TNode>(this.nodes, this.edges.Add(edge));
        }

        public struct Edge
        {
            private readonly TNode source, target;

            public Edge(TNode source, TNode target)
            {
                this.source = source;
                this.target = target;
            }

            public TNode Source
            {
                get { return this.source; }
            }

            public TNode Target
            {
                get { return this.target; }
            }
        }
    }
}
