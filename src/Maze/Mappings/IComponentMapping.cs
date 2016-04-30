using System.Collections.Immutable;

namespace Maze.Mappings
{
    public interface IComponentMapping
    {
        ImmutableDictionary<string, IMapping> Mappings { get; }
    }

    public interface IComponentMapping<TComponent> : IComponentMapping
    {
    }
}
