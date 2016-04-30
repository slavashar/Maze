using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;

namespace Maze.Mappings
{
    public sealed class MappingContainer
    {
        private readonly ImmutableHashSet<IMapping> mappings;
        private readonly ImmutableHashSet<IComponentMapping> componentMappings;
        private readonly ImmutableQueue<IMapping> executionQueue;
        private readonly ImmutableHashSet<IMapping> detachedMappings;
        private readonly ImmutableDictionary<IAnonymousMapping, IMapping> anonymousSources;
        private readonly ImmutableHashSet<IAnonymousMapping> missingSources;

        private MappingContainer(
            ImmutableHashSet<IMapping> mappings,
            ImmutableHashSet<IComponentMapping> componentMappings,
            ImmutableQueue<IMapping> executionQueue,
            ImmutableHashSet<IMapping> detachedMappings,
            ImmutableDictionary<IAnonymousMapping, IMapping> anonymousSources,
            ImmutableHashSet<IAnonymousMapping> missingSources)
        {
            this.mappings = mappings;
            this.componentMappings = componentMappings;
            this.executionQueue = executionQueue;
            this.detachedMappings = detachedMappings;
            this.anonymousSources = anonymousSources;
            this.missingSources = missingSources;
        }

        public ImmutableHashSet<IMapping> Mappings
        {
            get { return this.mappings; }
        }

        public ImmutableHashSet<IComponentMapping> Components
        {
            get { return this.componentMappings; }
        }

        public ImmutableQueue<IMapping> ExecutionQueue
        {
            get { return this.executionQueue; }
        }

        public static MappingContainer Create()
        {
            return new MappingContainer(
                ImmutableHashSet<IMapping>.Empty,
                ImmutableHashSet<IComponentMapping>.Empty,
                ImmutableQueue<IMapping>.Empty,
                ImmutableHashSet<IMapping>.Empty,
                ImmutableDictionary<IAnonymousMapping, IMapping>.Empty,
                ImmutableHashSet<IAnonymousMapping>.Empty);
        }

        public static MappingContainer Merge(MappingContainer firstContainer, MappingContainer secondContainer)
        {
            var mappings = firstContainer.mappings.Union(secondContainer.mappings);
            var componentMappings = firstContainer.componentMappings.Union(secondContainer.componentMappings);

            var executionQueue = firstContainer.executionQueue;

            foreach (var item in secondContainer.executionQueue)
            {
                if (!firstContainer.executionQueue.Contains(item))
                {
                    executionQueue = executionQueue.Enqueue(item);
                }
            }

            var detachedMappings = firstContainer.detachedMappings.Union(secondContainer.detachedMappings);

            var anonymousSources = firstContainer.anonymousSources;

            // TODO: review
            foreach (var item in secondContainer.anonymousSources)
            {
                if (firstContainer.anonymousSources.ContainsKey(item.Key))
                {
                    if (secondContainer.anonymousSources[item.Key] != firstContainer.anonymousSources[item.Key])
                    {
                        throw new InvalidOperationException("Ambiguous source mapping provided for " + item.Key.ElementType.Name);
                    }
                }
                else
                {
                    anonymousSources.Add(item.Key, item.Value);
                }
            }

            var missingSources = firstContainer.missingSources.Union(secondContainer.missingSources);

            var typeLookup = mappings.ToLookup(x => x.GetType().FindGenericType(typeof(IMapping<>)).GetGenericArguments().Single());

            foreach (var missing in missingSources)
            {
                var proposedSources = typeLookup[missing.ElementType].ToList();

                switch (proposedSources.Count)
                {
                    case 1:
                        anonymousSources = anonymousSources.Add(missing, proposedSources[0]);
                        missingSources = missingSources.Remove(missing);

                        break;
                    default:
                        throw new InvalidOperationException("Ambiguous source mapping provided for " + missing.ElementType.Name);
                }
            }

            while (true)
            {
                var detached = detachedMappings
                    .Where(m => m.SourceMappings.All(x => executionQueue.Contains(x is IAnonymousMapping ? anonymousSources.GetValueOrDefault((IAnonymousMapping)x) : x)))
                    .ToList();

                if (detached.Count == 0)
                {
                    break;
                }

                foreach (var mapping in detached)
                {
                    executionQueue = executionQueue.Enqueue(mapping);
                    detachedMappings = detachedMappings.Remove(mapping);
                }
            }

            return new MappingContainer(
                mappings,
                componentMappings,
                executionQueue,
                detachedMappings,
                anonymousSources,
                missingSources);
        }

        public MappingContainer Add<TElement>(IMapping<TElement> instance)
        {
            if (this.mappings.Contains(instance))
            {
                return this;
            }

            var executionQueue = this.executionQueue;
            var detachedMappings = this.detachedMappings;
            var anonymousSources = this.anonymousSources;
            var missingSources = this.missingSources;

            var anonymouses = instance.SourceMappings.OfType<IAnonymousMapping>();

            if (anonymouses.Any())
            {
                var typeLookup = this.mappings.ToLookup(x => x.GetType().FindGenericType(typeof(IMapping<>)).GetGenericArguments().Single());

                var sourceAvailable = true;

                foreach (var anonymous in anonymouses)
                {
                    var proposedSources = typeLookup[anonymous.ElementType].ToList();

                    switch (proposedSources.Count)
                    {
                        case 0:
                            missingSources = missingSources.Add(anonymous);
                            sourceAvailable = false;
                            break;
                        case 1:
                            anonymousSources = anonymousSources.Add(anonymous, proposedSources[0]);
                            if (sourceAvailable)
                            {
                                sourceAvailable = executionQueue.Contains(proposedSources[0]);
                            }

                            break;
                        default:
                            throw new InvalidOperationException("Ambiguous source mapping provided for " + anonymous.ElementType.Name);
                    }
                }

                if (sourceAvailable && instance.SourceMappings.Except(anonymouses).All(executionQueue.Contains))
                {
                    executionQueue = executionQueue.Enqueue(instance);
                }
                else
                {
                    detachedMappings = detachedMappings.Add(instance);
                }
            }
            else
            {
                if (instance.SourceMappings.All(executionQueue.Contains))
                {
                    executionQueue = executionQueue.Enqueue(instance);
                }
                else
                {
                    detachedMappings = detachedMappings.Add(instance);
                }
            }

            foreach (var missing in missingSources.OfType<AnonymousMapping<TElement>>())
            {
                anonymousSources = anonymousSources.Add(missing, instance);
                missingSources = missingSources.Remove(missing);
            }

            while (true)
            {
                var detached = detachedMappings
                    .Where(m => m.SourceMappings.All(x => executionQueue.Contains(x is IAnonymousMapping ? anonymousSources.GetValueOrDefault((IAnonymousMapping)x) : x)))
                    .ToList();

                if (detached.Count == 0)
                {
                    break;
                }

                foreach (var mapping in detached)
                {
                    executionQueue = executionQueue.Enqueue(mapping);
                    detachedMappings = detachedMappings.Remove(mapping);
                }
            }

            return new MappingContainer(
                this.mappings.Add(instance),
                this.componentMappings,
                executionQueue,
                detachedMappings,
                anonymousSources,
                missingSources);
        }

        public MappingContainer Add<TComponent>(IComponentMapping<TComponent> instance)
        {
            if (this.componentMappings.Contains(instance))
            {
                return this;
            }

            var container = this;

            foreach (var item in instance.Mappings.Values)
            {
                var type = item.GetType().FindGenericType(typeof(IMapping<>)).GetGenericArguments().Single();

                container = TypeExt.InvokeGenericMethod<MappingContainer>(new Func<IMapping<object>, MappingContainer>(container.Add), type, item);
            }

            return new MappingContainer(
                container.mappings,
                container.componentMappings.Add(instance),
                container.executionQueue,
                container.detachedMappings,
                container.anonymousSources,
                container.missingSources);
        }

        internal void BuildGraph<TNode>(IExecutionGraphBuilder<TNode> builder)
        {
            var nodes = new Dictionary<IMapping, TNode>();

            foreach (var mapping in this.executionQueue)
            {
                var expression = mapping.Expression;
                if (expression == null)
                {
                    throw new InvalidOperationException("Cannot create mapping node from " + mapping.GetType().Name);
                }

                var type = mapping.GetType().FindGenericType(typeof(IMapping<>)).GetGenericArguments().Single();

                var sources = mapping.SourceMappings.Select(x => nodes[x is IAnonymousMapping ? this.anonymousSources[(IAnonymousMapping)x] : x]);

                var node = TypeExt.InvokeGenericMethod<TNode>(
                    new Func<IMapping<object>, LambdaExpression, IEnumerable<TNode>, TNode>(builder.CreateExpressionNode), type, mapping, expression, sources);

                nodes.Add(mapping, node);
            }
        }
    }
}
