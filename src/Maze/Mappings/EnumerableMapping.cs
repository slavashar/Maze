using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace Maze.Mappings
{
    public sealed class EnumerableMapping<TElement> : IMapping<TElement>
    {
        public EnumerableMapping(string name, IEnumerable<TElement> enumerable)
        {
            this.Name = name ?? ("Source: " + typeof(TElement).Name);
            this.Enumerable = enumerable;
            this.Expression = System.Linq.Expressions.Expression.Lambda(System.Linq.Expressions.Expression.Constant(enumerable));
        }

        public Type ElementType
        {
            get { return typeof(TElement); }
        }

        public string Name { get; }

        public IEnumerable<TElement> Enumerable { get; }

        public LambdaExpression Expression { get; }

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
