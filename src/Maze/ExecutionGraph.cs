using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Maze.Mappings;

namespace Maze
{
    public class ExecutionGraph
    {
        private readonly ImmutableDictionary<IMapping, ExecutionGraphNode> mappings;
        private readonly List<Task> nodes;

        private ImmutableStack<Func<Task>> executionStack;

        public ExecutionGraph(
            MappingContainer container,
            ImmutableStack<Func<Task>> executionStack,
            ImmutableDictionary<IMapping, ExecutionGraphNode> mappings)
        {
            this.Container = container;
            this.executionStack = executionStack;
            this.mappings = mappings;
            this.nodes = new List<Task>();
        }

        protected ExecutionGraph(ExecutionGraph graph)
        {
            this.Container = graph.Container;
            this.executionStack = graph.executionStack;
            this.mappings = graph.mappings;
            this.nodes = graph.nodes;
        }

        public MappingContainer Container { get; }

        public IObservable<TElement> GetStream<TElement>(IMapping<TElement> mapping)
        {
            return (IObservable<TElement>)this.mappings[mapping];
        }

        public Task Release()
        {
            lock (this.nodes)
            {
                while (!this.executionStack.IsEmpty)
                {
                    Func<Task> node;
                    this.executionStack = this.executionStack.Pop(out node);
                    this.nodes.Add(node.Invoke());
                }

                return Task.WhenAll(this.nodes);
            }
        }
    }

    public class MappingExecution<TElement> : ExecutionGraph, IObservable<TElement>
    {
        public MappingExecution(ExecutionGraph execution, IMapping<TElement> mapping)
            : base(execution)
        {
            this.Mapping = mapping;
        }

        public IMapping<TElement> Mapping { get; }

        public IObservable<TElement> GetStream()
        {
            return this.GetStream(this.Mapping);
        }

        public IDisposable Subscribe(IObserver<TElement> observer)
        {
            return this.GetStream().Subscribe(observer);
        }
    }

    public class ExecutionGraphBulder
    {
        private readonly MappingContainer container;

        private readonly ImmutableDictionary<IMapping, ExecutionGraphNode>.Builder mappings = ImmutableDictionary.CreateBuilder<IMapping, ExecutionGraphNode>();

        private readonly List<Func<Task>> queue = new List<Func<Task>>();

        public ExecutionGraphBulder(MappingContainer container)
        {
            this.container = container;
        }

        public ExecutionGraph ToGraph()
        {
            return new ExecutionGraph(this.container, ImmutableStack.CreateRange(this.queue), this.mappings.ToImmutable());
        }

        public ExecutionGraphNode<TElement> AddNode<TElement>(ExecutionGraphNode<TElement> node)
        {
            this.mappings.Add(node.Mapping, node);
            this.queue.Add(node.Execute);

            return node;
        }
    }

    public abstract class ExecutionGraphNode
    {
        public abstract ParameterExpression Paramater { get; }
    }

    public class ExecutionGraphNode<TElement> : ExecutionGraphNode, IObservable<TElement>
    {
        private readonly IConnectableObservable<TElement> queryable;

        public ExecutionGraphNode(IMapping mapping, IObservable<TElement> queryable)
        {
            this.Mapping = mapping;
            this.queryable = queryable.Publish();
        }

        public IMapping Mapping { get; }

        public override ParameterExpression Paramater { get; } = Expression.Parameter(typeof(IObservable<TElement>));

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
