using Maze.Nodes;

namespace Maze
{
    public static class NodeStringifyExtension
    {
        private static readonly TextSyntaxNodeVisitor Builder = new TextSyntaxNodeVisitor();

        public static string Stringify(this Node node)
        {
            return Builder.VisitNode(node).Print();
        }
    }
}
