using System;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace Maze.Mappings
{
    public sealed class ExpressionMapping<TElement> : IMapping<TElement>
    {
        public ExpressionMapping(string name, LambdaExpression expression, ImmutableDictionary<ParameterExpression, IMapping> sourceMappings)
        {
            this.Name = name ?? ("Transformation: " + typeof(TElement).Name);
            this.Expression = expression;
            this.SourceMappings = sourceMappings;
        }

        public Type ElementType
        {
            get { return typeof(TElement); }
        }

        public string Name { get; }

        public LambdaExpression Expression { get; }

        public ImmutableDictionary<ParameterExpression, IMapping> SourceMappings { get; }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
