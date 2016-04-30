using System;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace Maze.Mappings
{
    public class ExpressionMapping<TElement> : IMapping<TElement>
    {
        private readonly LambdaExpression expression;
        private readonly ImmutableList<IMapping> sourceMappings;

        public ExpressionMapping(LambdaExpression expression, ImmutableList<IMapping> sourceMappings)
        {
            this.expression = expression;
            this.sourceMappings = sourceMappings;
        }

        public Type ElementType
        {
            get { return typeof(TElement); }
        }

        public LambdaExpression Expression
        {
            get { return this.expression; }
        }

        public ImmutableList<IMapping> SourceMappings
        {
            get { return this.sourceMappings; }
        }

        public override string ToString()
        {
            return "Transformation: " + typeof(TElement).Name;
        }
    }
}
