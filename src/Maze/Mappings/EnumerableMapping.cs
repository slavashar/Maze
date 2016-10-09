using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace Maze.Mappings
{
    public class EnumerableMapping<TElement> : IMapping<TElement>
    {
        private LambdaExpression expression;

        public EnumerableMapping(string name, IEnumerable<TElement> enumerable)
        {
            this.Name = name ?? ("Source: " + typeof(TElement).Name);
            this.Enumerable = enumerable;
            this.expression = System.Linq.Expressions.Expression.Lambda(System.Linq.Expressions.Expression.Constant(enumerable));
        }

        public Type ElementType
        {
            get { return typeof(TElement); }
        }

        public string Name { get; }

        public IEnumerable<TElement> Enumerable { get; }

        public LambdaExpression Expression
        {
            get { return this.expression; }
        }

        public ImmutableDictionary<ParameterExpression, IMapping> SourceMappings
        {
            get { return ImmutableDictionary<ParameterExpression, IMapping>.Empty; }
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
