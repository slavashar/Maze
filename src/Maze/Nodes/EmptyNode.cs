namespace Maze.Nodes
{
    public sealed class EmptyNode : Node
    {
        private static EmptyNode instance = new EmptyNode();

        private EmptyNode()
        {
        }

        public override NodeKind Kind => NodeKind.Empty;

        public static EmptyNode Create()
        {
            return instance;
        }
    }
}
