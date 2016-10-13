using System.Collections.Immutable;
using System.Linq.Expressions;

namespace Maze.Mappings
{
    public sealed class ProxyMapping<TElement> : IMapping<TElement>
    {
        private readonly IMapping<TElement> original;
        private readonly ImmutableDictionary<ParameterExpression, IMapping> sources;

        public ProxyMapping(IMapping<TElement> original, ImmutableDictionary<ParameterExpression, IMapping> sources)
        {
            this.original = original;
            this.sources = sources;
        }

        public LambdaExpression Expression
        {
            get { return this.original.Expression; }
        }

        public string Name
        {
            get { return this.original.Name; }
        }

        public IMapping<TElement> Original
        {
            get { return this.original; }
        }

        public ImmutableDictionary<ParameterExpression, IMapping> SourceMappings
        {
            get { return this.sources; }
        }

        public override bool Equals(object obj)
        {
            if (obj is ProxyMapping<TElement>)
            {
                return this.original.Equals(((ProxyMapping<TElement>)obj).original);
            }

            return this.original.Equals(obj);
        }

        public override int GetHashCode()
        {
            return this.original.GetHashCode();
        }

        public override string ToString()
        {
            return this.original.Name;
        }
    }
}
