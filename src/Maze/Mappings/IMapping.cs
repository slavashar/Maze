using System.Collections.Immutable;
using System.Linq.Expressions;

namespace Maze.Mappings
{
    public interface IMapping
    {
        string Name { get; }

        LambdaExpression Expression { get; }

        ImmutableDictionary<ParameterExpression, IMapping> SourceMappings { get; }
    }

    public interface IMapping<TElement> : IMapping
    {
    }
}
