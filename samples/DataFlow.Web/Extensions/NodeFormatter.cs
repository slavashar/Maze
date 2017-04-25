using System.Linq.Expressions;
using Maze;
using Maze.Nodes;

namespace DataFlow.Web.Extensions
{
    public static class NodeFormatter
    {
        public static Graph<Node> ExtractGraph<TElement>(this Maze.Mappings.MappingReference<TElement> refrence)
        {
            var node = ExpressionNodeBuilder.Parse(refrence.Instance.Expression);

            node = node.Find<MethodCallExpression>(n => n.Element.Method.Name == "Select")
                .Change(n => n.ReplaceNode(n.Get(x => x.Method), n.Get(x => x.Arguments)));

            node = node.Find<MethodCallExpression>(n => n.Element.Method.Name == "Where")
                .Change(n => n.ReplaceNode(n.Get(x => x.Method), n.Get(x => x.Arguments)));

            node = node.Find(ExpressionTokens.As, ExpressionTokens.Convert)
                .Change(n => n.GetParent());

            node = node.Find<ParameterExpression>(n => refrence.Instance.SourceMappings.ContainsKey(n.Element))
                .Change(n => NodeFactory.Text(refrence.Instance.SourceMappings[n.Element].Name));

            return node.ToGraph();
        }

        public static Graph<Node> ExtractGraph(this Maze.Mappings.ContainerReference refrence)
        {
            var parser = new MappingNodeBuilder();

            var node = parser.Build(refrence.Container);

            return node.ToGraph();
        }

        private static Node Simplify(Node node)
        {
            return node;
        }
    }
}
