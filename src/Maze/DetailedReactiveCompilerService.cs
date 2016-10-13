using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Maze.Mappings;

namespace Maze
{
    public class DetailedReactiveCompilerService
    {
        public ExecutionGraph Build(MappingContainer container, Expression[] tracking)
        {
            var nodes = new Dictionary<IMapping, ExecutionGraphNode>();

            var createMethod = TypeExt.GetMethodDefinition(() => this.CreateExpressionNode<object>(null, null, null));

            var builder = new ExecutionGraphBulder(container);

            foreach (var mapping in container.ExecutionQueue)
            {
                var sources = mapping.SourceMappings.ToImmutableDictionary(x => x.Key, x => nodes[x.Value]);

                var node = (ExecutionGraphNode)createMethod.MakeGenericMethod(mapping.GetElementType()).Invoke(this, new object[] { builder, mapping, sources });

                nodes.Add(mapping, node);
            }

            return builder.ToGraph();
        }

        private ExecutionGraphNode<TElement> CreateExpressionNode<TElement>(
            ExecutionGraphBulder builder, IMapping<TElement> mapping, IReadOnlyDictionary<ParameterExpression, ExecutionGraphNode> sourceNodes)
        {
            if (mapping.Expression.Parameters.Count == 0 && mapping.Expression.Body.NodeType == ExpressionType.Constant)
            {
                var @enum = ((ConstantExpression)mapping.Expression.Body).Value;

                // TODO: allow to provide the scheduler externally
                return builder.AddNode(new ExecutionGraphNode<TElement>(mapping, Observable.ToObservable((IEnumerable<TElement>)@enum, Scheduler.Default)));
            }

            var sources = mapping.Expression.Parameters.Select(x => sourceNodes[x]).ToArray();

            var mapParams = mapping.Expression.Parameters.ToImmutableDictionary(x => x, x => sourceNodes[x].Paramater);

            var rewriter = new MetricObservableRewriter(mapParams);

            var expression = (LambdaExpression)rewriter.Visit(mapping.Expression);

            var queryable = (IObservable<TElement>)expression.Compile().DynamicInvoke(sources);

            return builder.AddNode(new ExecutionGraphNode<TElement>(mapping, queryable));
        }
    }
}
