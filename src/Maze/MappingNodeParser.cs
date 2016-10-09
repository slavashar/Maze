using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Maze.Mappings;
using Maze.Nodes;

namespace Maze
{
    public class MappingNodeParser
    {
        public Node Visit(MappingContainer container)
        {
            var dictionary = ImmutableDictionary<IMapping, Node>.Empty;

            Node node = NodeFactory.Empty;

            foreach (var mapping in container.ExecutionQueue)
            {
                node = this.CreateNode(mapping, container, dictionary);

                dictionary = dictionary.Add(mapping, node);
            }

            return node;
        }

        private Node CreateNode(IMapping mapping, MappingContainer container, ImmutableDictionary<IMapping, Node> dictionary)
        {
            var parents = mapping.SourceMappings.Values.Select(x => dictionary[container.GetSourceMapping(x)]).ToList();

            var node = NodeFactory.ItemNode(mapping, MappingTokens.Node, NodeFactory.Text(mapping.Name));

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
