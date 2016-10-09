using System.Collections.Generic;

namespace Maze.Mappings
{
    public class ContainerReference
    {
        private readonly MappingContainer container;

        internal ContainerReference(MappingContainer container)
        {
            this.container = container;
        }

        public static ContainerReference Empty
        {
            get { return EmptyMappingContainer.Instance; }
        }

        public MappingContainer Container
        {
            get { return this.container; }
        }

        public ISet<IMapping> Mappings
        {
            get { return this.container.Mappings; }
        }

        public ISet<IComponentMapping> Components
        {
            get { return this.container.Components; }
        }

        internal static ComponentMappingReference<CombinedComponentMapping<TComponentFirst, TComponentSecond>.Component> Combine<TComponentFirst, TComponentSecond>(
            ComponentMappingReference<TComponentFirst> first, ComponentMappingReference<TComponentSecond> second)
        {
            var instance = new CombinedComponentMapping<TComponentFirst, TComponentSecond>.MergedComponentMappingProxy(first.Instance, second.Instance);

            return new ComponentMappingReference<CombinedComponentMapping<TComponentFirst, TComponentSecond>.Component>(
                instance, MappingContainer.Merge(first.container, second.container).Add(instance));
        }

        internal virtual MappingReference<TElement> Wrap<TElement>(IMapping<TElement> instance)
        {
            return new MappingReference<TElement>(instance, this.container);
        }

        internal MappingReference<TElement> Add<TElement>(IMapping<TElement> instance)
        {
            return new MappingReference<TElement>(instance, this.container.Add(instance));
        }

        internal ComponentMappingReference<TComponent> Add<TComponent>(IComponentMapping<TComponent> instance)
        {
            return new ComponentMappingReference<TComponent>(instance, this.container.Add(instance));
        }

        internal ContainerReference Add(ContainerReference mappingContainer)
        {
            return new ContainerReference(MappingContainer.Merge(this.container, mappingContainer.container));
        }

        private sealed class EmptyMappingContainer : ContainerReference
        {
            public EmptyMappingContainer()
                : base(MappingContainer.Create())
            {
            }

            public static EmptyMappingContainer Instance { get; private set; } = new EmptyMappingContainer();
        }
    }
}
