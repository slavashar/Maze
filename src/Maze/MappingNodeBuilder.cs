using System.Collections.Immutable;
using System.Linq;
using Maze.Mappings;
using Maze.Nodes;

namespace Maze
{
    public class MappingNodeBuilder
    {
        public Node Build(MappingContainer container)
        {
            var dictionary = ImmutableDictionary<IMapping, Node>.Empty;

            Node node = NodeFactory.Empty;

            foreach (var mapping in container.ExecutionQueue)
            {
                node = this.CreateMappingNode(mapping, dictionary);

                dictionary = dictionary.Add(mapping, node);
            }

            return node;
        }

        private Node CreateMappingNode(IMapping mapping, ImmutableDictionary<IMapping, Node> dictionary)
        {
            var parents = mapping.SourceMappings.Values.Select(x => dictionary[x]).ToList();

            Node node = NodeFactory.Build(mapping, MappingTokens.Node)
                .Add(x => x.Name, NodeFactory.Text(mapping.Name))
                .Add(x => x.Expression, ExpressionNodeBuilder.Parse(mapping.Expression));

            if (parents.Count == 0)
            {
                return node;
            }

            if (parents.Count == 1)
            {
                return parents[0].Then(mapping, MappingTokens.Transformation, node);
            }

            return NodeFactory.MultipleItems(parents).Then(mapping, MappingTokens.Transformation, node);
        }
    }
}
