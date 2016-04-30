using System.Collections.Generic;

namespace Maze.Nodes
{
    public interface INodeContainer
    {
        IEnumerable<Node> GetNodes();

        Node ReplaceNode(Node node, Node replacement);
    }
}
