using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Maze.Mappings
{
    public sealed class AnonymousMapping<TElement> : IMapping<TElement>, IAnonymousMapping
    {
        public System.Linq.Expressions.LambdaExpression Expression
        {
            get { return null; }
        }

        public ImmutableList<IMapping> SourceMappings
        {
            get { return ImmutableList<IMapping>.Empty; }
        }

        public Type ElementType
        {
            get { return typeof(TElement); }
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
