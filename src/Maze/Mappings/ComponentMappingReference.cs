using System;

namespace Maze.Mappings
{
    public class ComponentMappingReference<TComponent> : ContainerReference
    {
        private readonly IComponentMapping<TComponent> instance;

        internal ComponentMappingReference(IComponentMapping<TComponent> instance, MappingContainer container)
            : base(container)
        {
            if (ReferenceEquals(instance, null))
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (!container.Components.Contains(instance))
            {
                throw new ArgumentOutOfRangeException(nameof(instance));
            }

            this.instance = instance;
        }

        public IComponentMapping<TComponent> Instance
        {
            get { return this.instance; }
        }
    }
}
