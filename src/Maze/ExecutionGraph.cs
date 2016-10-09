using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Maze.Mappings;

namespace Maze
{
    public class ExecutionGraph
    {
        private readonly ImmutableDictionary<IMapping, Func<object>> observers;
        private readonly List<Task> nodes;

        private ImmutableStack<Func<Task>> executionStack;

        public ExecutionGraph(
            MappingContainer container,
            ImmutableStack<Func<Task>> executionStack,
            ImmutableDictionary<IMapping, Func<object>> observers)
        {
            this.Container = container;
            this.executionStack = executionStack;
            this.observers = observers;
            this.nodes = new List<Task>();
        }

        protected ExecutionGraph(ExecutionGraph graph)
        {
            this.Container = graph.Container;
            this.executionStack = graph.executionStack;
            this.observers = graph.observers;
            this.nodes = graph.nodes;
        }

        public MappingContainer Container { get; }

        public IObservable<TElement> GetStream<TElement>(IMapping<TElement> mapping)
        {
            return (IObservable<TElement>)this.observers[mapping].Invoke();
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
}
