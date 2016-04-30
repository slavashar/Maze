using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace Maze.Mappings
{
    public class EnumerableMapping<TElement> : IMapping<TElement>
    {
        private readonly IEnumerable<TElement> enumerable;
        private LambdaExpression expression;

        public EnumerableMapping(IEnumerable<TElement> enumerable)
        {
            this.enumerable = enumerable;
            this.expression = System.Linq.Expressions.Expression.Lambda(System.Linq.Expressions.Expression.Constant(enumerable));
        }

        public Type ElementType
        {
            get { return typeof(TElement); }
        }

        public IEnumerable<TElement> Enumerable
        {
            get { return this.enumerable; }
        }

        public LambdaExpression Expression
        {
            get { return this.expression; }
        }

        public ImmutableList<IMapping> SourceMappings
        {
            get { return ImmutableList<IMapping>.Empty; }
        }

        public override string ToString()
        {
            return "Source: " + typeof(TElement).Name;
        }
    }
}
