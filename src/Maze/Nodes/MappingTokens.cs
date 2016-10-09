using Maze.Mappings;

namespace Maze.Nodes
{
    public static class MappingTokens
    {
        public static ItemToken<IMapping> Node { get; } = new ItemToken<IMapping>(x => x.Name, SyntaxFactory.FromText("["), SyntaxFactory.FromText("]"));

        public static UnaryItemToken<IMapping> Transformation { get; } = new UnaryItemToken<IMapping>(x => x.SourceMappings, x => x.Expression, null, SyntaxFactory.FromText("->"), null);
    }
}
