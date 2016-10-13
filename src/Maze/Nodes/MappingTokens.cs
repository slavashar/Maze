using System;
using System.Collections.Generic;
using Maze.Mappings;

namespace Maze.Nodes
{
    public static class MappingTokens
    {
        public static MappingToken Node { get; } = new MappingToken();

        public static UnaryItemToken<IMapping> Transformation { get; } = new UnaryItemToken<IMapping>(x => x.SourceMappings, x => x.Expression, null, SyntaxFactory.FromText("->"), null);
    }

    public class MappingToken : TypedToken<IMapping>
    {
        public MappingToken()
            : base(Name, Expression)
        {
        }

        public static TokenConnection Name { get; } = TokenConnection.New<IMapping>(x => x.Name);

        public static TokenConnection Expression { get; } = TokenConnection.New<IMapping>(x => x.Expression);

        public override IEnumerable<Syntax> GetSyntax()
        {
            yield return FromText("[");
            yield return FromNode(Name);
            yield return FromText("]");
        }
    }
}
