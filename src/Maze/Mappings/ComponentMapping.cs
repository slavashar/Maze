using System;
using System.Collections.Immutable;

namespace Maze.Mappings
{
    internal class ComponentMapping<TComponent> : IComponentMapping<TComponent>
    {
        private readonly ImmutableDictionary<string, IMapping> mappings;

        public ComponentMapping(ImmutableDictionary<string, IMapping> mappings)
        {
            this.mappings = mappings;
        }

        public ImmutableDictionary<string, IMapping> Mappings
        {
            get { return this.mappings; }
        }
    }
}
