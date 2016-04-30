using System.Collections.Immutable;
using System.Linq.Expressions;

namespace Maze.Mappings
{
    public interface IMapping
    {
        LambdaExpression Expression { get; }

        ImmutableList<IMapping> SourceMappings { get; }
    }

    public interface IMapping<TElement> : IMapping
    {
    }
}
