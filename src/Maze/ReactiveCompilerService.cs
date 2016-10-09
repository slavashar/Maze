using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Maze.Mappings;

namespace Maze
{
    public class ReactiveCompilerService : IMappingCompilerService
    {
        public ExecutionGraph Build(MappingContainer container)
        {
            var nodes = new Dictionary<IMapping, object>();

            var createMethod = TypeExt.GetMethodDefinition(() => this.CreateExpressionNode<object>(null, null, null, null));

            var builder = new Builder();

            foreach (var mapping in container.ExecutionQueue)
            {
                var expression = mapping.Expression;
                if (expression == null)
                {
                    throw new InvalidOperationException("Cannot create mapping node from " + mapping.GetType().Name);
                }

                var sources = mapping.SourceMappings.ToImmutableDictionary(x => x.Key, x => nodes[container.GetSourceMapping(x.Value)]);

                var node = createMethod.MakeGenericMethod(mapping.GetElementType()).Invoke(this, new object[] { builder, mapping, expression, sources });

                nodes.Add(mapping, node);
            }

            return new ExecutionGraph(container, ImmutableStack.CreateRange(builder.ExecutionQueue.ToImmutable()), builder.Mappings.ToImmutable());
        }

        private GraphNode<TElement> CreateExpressionNode<TElement>(
            Builder builder, IMapping<TElement> mapping, LambdaExpression expression, IReadOnlyDictionary<ParameterExpression, object> sourceNodes)
        {
            var sources = expression.Parameters.Select(x => sourceNodes[x]).ToArray();

            expression = ObservableRewriter.ChangeParameters(expression);

            if (expression.Parameters.Count == 0 && expression.Body.NodeType == ExpressionType.Constant)
            {
                var @enum = ((ConstantExpression)expression.Body).Value;

                // TODO: allow to provide the scheduler externally
                return this.AddNode(builder, mapping, new GraphNode<TElement>(mapping, Observable.ToObservable((IEnumerable<TElement>)@enum, Scheduler.Default)));
            }

            var queryable = (IObservable<TElement>)expression.Compile().DynamicInvoke(sources);

            return this.AddNode(builder, mapping, new GraphNode<TElement>(mapping, queryable));
        }

        private GraphNode<TElement> AddNode<TElement>(Builder builder, IMapping<TElement> mapping, GraphNode<TElement> node)
        {
            builder.Mappings.Add(mapping, node.GetObserver);
            builder.ExecutionQueue.Add(node.Execute);

            return node;
        }

        private class Builder
        {
            public ImmutableDictionary<IMapping, Func<object>>.Builder Mappings { get; } = ImmutableDictionary.CreateBuilder<IMapping, Func<object>>();

            public ImmutableList<Func<Task>>.Builder ExecutionQueue { get; } = ImmutableList.CreateBuilder<Func<Task>>();
        }

        private class GraphNode<TElement> : IObservable<TElement>
        {
            private readonly IMapping mapping;
            private readonly IConnectableObservable<TElement> queryable;

            public GraphNode(IMapping mapping, IObservable<TElement> queryable)
            {
                this.mapping = mapping;
                this.queryable = queryable.Publish();
            }

            public IObservable<TElement> GetObserver()
            {
                return this;
            }

            public Task Execute()
            {
                var task = this.queryable.ToTask();
                this.queryable.Connect();
                return task;
            }

            public IDisposable Subscribe(IObserver<TElement> observer)
            {
                return this.queryable.Subscribe(observer);
            }
        }
    }
}
