using System;
using System.Collections.Immutable;

namespace Maze.Mappings
{
    public static class CombinedComponentMapping<TComponentFirst, TComponentSecond>
    {
        public abstract class Component
        {
            public TComponentFirst First { get; }

            public TComponentSecond Second { get; }
        }

        internal class MergedComponentMappingProxy : IComponentMapping<Component>
        {
            private readonly ImmutableDictionary<string, IMapping> mappings;

            public MergedComponentMappingProxy(IComponentMapping<TComponentFirst> first, IComponentMapping<TComponentSecond> second)
            {
                var builder = ImmutableDictionary.CreateBuilder<string, IMapping>();

                foreach (var item in first.Mappings)
                {
                    builder.Add("First." + item.Key, item.Value);
                }

                foreach (var item in second.Mappings)
                {
                    builder.Add("Second." + item.Key, item.Value);
                }

                this.mappings = builder.ToImmutable();
            }

            public MergedComponentMappingProxy(IMapping first, IMapping second)
            {
                var builder = ImmutableDictionary.CreateBuilder<string, IMapping>();

                builder.Add("First", first);
                builder.Add("Second", second);

                this.mappings = builder.ToImmutable();
            }

            public ImmutableDictionary<string, IMapping> Mappings
            {
                get { return this.mappings; }
            }
        }
    }
}
