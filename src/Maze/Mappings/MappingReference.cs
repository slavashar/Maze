using System;

namespace Maze.Mappings
{
    public class MappingReference<TElement> : ContainerReference
    {
        private readonly IMapping<TElement> instance;

        internal MappingReference(IMapping<TElement> instance, MappingContainer container)
            : base(container)
        {
            if (ReferenceEquals(instance, null))
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (!container.Mappings.Contains(instance))
            {
                throw new ArgumentOutOfRangeException(nameof(instance));
            }

            this.instance = instance;
        }

        public IMapping<TElement> Instance
        {
            get { return this.instance; }
        }

        internal override MappingReference<TWrapElement> Wrap<TWrapElement>(IMapping<TWrapElement> instance)
        {
            return ReferenceEquals(this.instance, instance) ? this as MappingReference<TWrapElement> : base.Wrap(instance);
        }
    }
}
