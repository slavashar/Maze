using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Subjects;

using Maze.Mappings;

namespace Maze
{
    public class SimpleCompiler : IMappingCompilerService
    {
        public IExecutionGraph Build(ContainerReference mapping)
        {
            var builder = new Builder();

            mapping.BuildGraph(builder);

            return builder.ToGraph();
        }

        private class GraphNode<TElement> : IEnumerable<TElement>
        {
            private readonly IMapping mapping;
            private readonly IEnumerable<TElement> enumerable;
            private readonly ISubject<TElement> subject = new Subject<TElement>();
            private IList<TElement> data;

            public GraphNode(IMapping mapping, IEnumerable<TElement> enumerable)
            {
                this.mapping = mapping;
                this.enumerable = enumerable;
            }

            public IObservable<TElement> Observable
            {
                get { return this.subject; }
            }

            public void Execute()
            {
                this.data = new List<TElement>();

                foreach (var item in this.enumerable)
                {
                    this.subject.OnNext(item);

                    this.data.Add(item);
                }

                this.subject.OnCompleted();
            }

            public IEnumerator<TElement> GetEnumerator()
            {
                return this.data.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        private class Builder : IExecutionGraphBuilder<object>
        {
            private readonly EnumerableRewriter rewriter = new EnumerableRewriter();

            private ImmutableDictionary<IMapping, object>.Builder mappings = ImmutableDictionary.CreateBuilder<IMapping, object>();
            private ImmutableList<Action>.Builder executionQueue = ImmutableList.CreateBuilder<Action>();

            public IExecutionGraph ToGraph()
            {
                return new Graph(new Queue<Action>(this.executionQueue.ToImmutable()), this.mappings.ToImmutable());
            }

            public object CreateExpressionNode<TElement>(IMapping<TElement> mapping, LambdaExpression expression, IEnumerable<object> sourceNodes)
            {
                var sources = sourceNodes.ToArray();

                expression = (LambdaExpression)this.rewriter.Visit(expression);

                var enumerable = (IEnumerable<TElement>)expression.Compile().DynamicInvoke(sources);

                return this.AddNode(mapping, new GraphNode<TElement>(mapping, enumerable));
            }

            private GraphNode<TElement> AddNode<TElement>(IMapping<TElement> mapping, GraphNode<TElement> node)
            {
                this.mappings.Add(mapping, node);
                this.executionQueue.Add(node.Execute);

                return node;
            }

            private class Graph : IExecutionGraph
            {
                private readonly Queue<Action> executionQueue;
                private readonly IReadOnlyDictionary<IMapping, object> mappings;

                public Graph(Queue<Action> executionQueue, IReadOnlyDictionary<IMapping, object> mappings)
                {
                    this.executionQueue = executionQueue;
                    this.mappings = mappings;
                }

                public IObservable<TElement> GetStream<TElement>(MappingReference<TElement> mapping)
                {
                    return ((GraphNode<TElement>)this.mappings[mapping.Instance]).Observable;
                }

                public void Release()
                {
                    while (this.executionQueue.Count > 0)
                    {
                        this.executionQueue.Dequeue().Invoke();
                    }
                }
            }
        }
    }
}
