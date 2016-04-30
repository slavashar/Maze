using System.Collections.Generic;

namespace Maze.Nodes
{
    internal interface IParentNodeProvider
    {
        IReadOnlyList<Node> Parents { get; }
    }
}
