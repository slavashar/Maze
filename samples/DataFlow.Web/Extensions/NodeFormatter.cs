using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Maze;
using Maze.Nodes;
using Maze.UX;

namespace DataFlow.Web.Extensions
{
    public static class NodeFormatter
    {
        public static Node ExtractGraph<TElement>(this Maze.Mappings.MappingReference<TElement> refrence)
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

            return node;
        }
    }
}
