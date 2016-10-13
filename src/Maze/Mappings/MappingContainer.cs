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

            var typeLookup = mappings.ToLookup(x => x.GetElementType());

            foreach (var missing in missingSources)
            {
                var proposedSources = typeLookup[missing.ElementType].ToList();

                switch (proposedSources.Count)
                {
                    case 0:
                        continue;

                    case 1:
                        anonymousSources = anonymousSources.Add(missing, proposedSources[0]);
                        missingSources = missingSources.Remove(missing);
                        break;

                    default:
                        throw new InvalidOperationException("Ambiguous source mapping provided for " + missing.ElementType.Name);
                }
            }

            ResolveDetachedMappings(ref executionQueue, ref detachedMappings, anonymousSources);

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

            var anonymouses = instance.SourceMappings.Values.OfType<IAnonymousMapping>();

            if (anonymouses.Any())
            {
                var typeLookup = this.mappings.ToLookup(x => x.GetElementType());

                var sourceAvailable = true;

                foreach (var anonymous in anonymouses)
                {
                    var proposedSources = typeLookup[anonymous.ElementType];

                    switch (proposedSources.Count())
                    {
                        case 0:
                            missingSources = missingSources.Add(anonymous);
                            sourceAvailable = false;
                            break;
                        case 1:
                            anonymousSources = anonymousSources.Add(anonymous, proposedSources.Single());
                            if (sourceAvailable)
                            {
                                sourceAvailable = executionQueue.Contains(proposedSources.Single());
                            }

                            break;
                        default:
                            throw new InvalidOperationException("Ambiguous source mapping provided for " + anonymous.ElementType.Name);
                    }
                }

                if (sourceAvailable && instance.SourceMappings.Values.Except(anonymouses).All(executionQueue.Contains))
                {
                    var sources = instance.SourceMappings.ToImmutableDictionary(x => x.Key, x => x.Value is IAnonymousMapping ? anonymousSources[(IAnonymousMapping)x.Value] : x.Value);

                    executionQueue = executionQueue.Enqueue(new ProxyMapping<TElement>(instance, sources));
                }
                else
                {
                    detachedMappings = detachedMappings.Add(instance);
                }
            }
            else
            {
                if (instance.SourceMappings.Values.All(executionQueue.Contains))
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

            ResolveDetachedMappings(ref executionQueue, ref detachedMappings, anonymousSources);

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

            var addeMethod = TypeExt.GetMethodDefinition(() => this.Add((IMapping<object>)null));

            foreach (var mapping in instance.Mappings.Values)
            {
                container = (MappingContainer)addeMethod
                    .MakeGenericMethod(mapping.GetElementType()).Invoke(container, new object[] { mapping });
            }

            return new MappingContainer(
                container.mappings,
                container.componentMappings.Add(instance),
                container.executionQueue,
                container.detachedMappings,
                container.anonymousSources,
                container.missingSources);
        }

        private static void ResolveDetachedMappings(
            ref ImmutableQueue<IMapping> executionQueue, ref ImmutableHashSet<IMapping> detachedMappings, ImmutableDictionary<IAnonymousMapping, IMapping> anonymousSources)
        {
            var queueChanged = true;

            while (queueChanged)
            {
                queueChanged = false;

                foreach (var mapping in detachedMappings)
                {
                    var allSourceQueued = true;
                    var containAnonymousSources = false;

                    var builder = ImmutableDictionary.CreateBuilder<ParameterExpression, IMapping>();

                    foreach (var sourceParamMapping in mapping.SourceMappings)
                    {
                        IMapping sourceMapping;

                        if (sourceParamMapping.Value is IAnonymousMapping)
                        {
                            containAnonymousSources = true;
                            if (!anonymousSources.TryGetValue((IAnonymousMapping)sourceParamMapping.Value, out sourceMapping))
                            {
                                allSourceQueued = false;
                                break;
                            }
                        }
                        else
                        {
                            sourceMapping = sourceParamMapping.Value;
                        }

                        if (!executionQueue.Contains(sourceMapping))
                        {
                            allSourceQueued = false;
                            break;
                        }

                        builder.Add(sourceParamMapping.Key, sourceMapping);
                    }

                    if (allSourceQueued)
                    {
                        if (containAnonymousSources)
                        {
                            var proxy = (IMapping)Activator.CreateInstance(
                                typeof(ProxyMapping<>).MakeGenericType(mapping.GetElementType()),
                                new object[] { mapping, builder.ToImmutable() });

                            executionQueue = executionQueue.Enqueue(proxy);
                        }
                        else
                        {
                            executionQueue = executionQueue.Enqueue(mapping);
                        }

                        detachedMappings = detachedMappings.Remove(mapping);

                        queueChanged = true;
                    }
                }
            }
        }
    }
}
