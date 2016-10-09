using System;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace Maze.Mappings
{
    public sealed class AnonymousMapping<TElement> : IMapping<TElement>, IAnonymousMapping
    {
        public Type ElementType
        {
            get { return typeof(TElement); }
        }

        public string Name
        {
            get { return this.ElementType.Name; }
        }

        public LambdaExpression Expression
        {
            get { return null; }
        }

        public ImmutableDictionary<ParameterExpression, IMapping> SourceMappings
        {
            get { return ImmutableDictionary<ParameterExpression, IMapping>.Empty; }
        }

        public override bool Equals(object obj)
        {
            return obj is AnonymousMapping<TElement>;
        }

        public override int GetHashCode()
        {
            return typeof(TElement).GetHashCode();
        }
    }
}
