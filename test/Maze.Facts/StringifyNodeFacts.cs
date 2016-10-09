using System.Linq.Expressions;
using Maze.Nodes;
using Xunit;

namespace Maze.Facts
{
    public class StringifyNodeFacts
    {
        [Fact]
        public void stringify_text_node()
        {
            var node = NodeFactory.Text("item");

            var result = new TextSyntaxNodeVisitor().VisitNode(node).Print();

            result.ShouldEqual("item");
        }

        [Fact]
        public void stringify_item_with_text()
        {
            var node = NodeFactory.ItemNode(ExpressionTokens.Constant, "item");

            var result = new TextSyntaxNodeVisitor().VisitNode(node).Print();

            result.ShouldEqual("item");
        }

        [Fact]
        public void stringify_unary_node()
        {
            var node = NodeFactory.Text("item").Then(ExpressionTokens.Negate);

            var result = new TextSyntaxNodeVisitor().VisitNode(node).Print();

            result.ShouldEqual("-item");
        }

        [Fact]
        public void stringify_unary_node_with_text()
        {
            var node = NodeFactory.Text("item").Then(ExpressionTokens.Member, "value");

            var result = new TextSyntaxNodeVisitor().VisitNode(node).Print();

            result.ShouldEqual("item.value");
        }

        [Fact]
        public void stringify_binary_node()
        {
            var node = NodeFactory.BinaryNode(ExpressionTokens.Multiply, "left", "right");

            var result = new TextSyntaxNodeVisitor().VisitNode(node).Print();

            result.ShouldEqual("left * right");
        }

        [Fact]
        public void stringify_complex_binary_node()
        {
            var node = NodeFactory.BinaryNode(
                ExpressionTokens.Divide, NodeFactory.BinaryNode(ExpressionTokens.Multiply, "left", "central").Then(ExpressionTokens.Brackets), "right");

            var result = new TextSyntaxNodeVisitor().VisitNode(node).Print();

            result.ShouldEqual("(left * central) / right");
        }

        [Fact]
        public void stringify_conditional_node()
        {
            Node node = NodeFactory.Build<ConditionalExpression, ConditionalExpressionToken>(null, ExpressionTokens.Conditional)
                .Add(x => x.Test, NodeFactory.Text("test"))
                .Add(x => x.IfTrue, NodeFactory.Text("true"))
                .Add(x => x.IfFalse, NodeFactory.Text("false"));

            var result = new TextSyntaxNodeVisitor().VisitNode(node).Print();

            result.ShouldEqual("if test then true else false");
        }

        [Fact]
        public void stringify_call_node()
        {
            Node node = NodeFactory.Build<MethodCallExpression, MethodCallExpressionToken>(null, ExpressionTokens.MethodCall)
                .Add(x => x.Object, NodeFactory.ItemNode(ExpressionTokens.Parameter, "src"))
                .Add(x => x.Method, "Select")
                .Add(x => x.Arguments, NodeFactory.BinaryNode(ExpressionTokens.Add, NodeFactory.ItemNode(ExpressionTokens.Parameter, "x"), NodeFactory.ItemNode(ExpressionTokens.Constant, "10")));

            var result = new TextSyntaxNodeVisitor().VisitNode(node).Print();

            result.ShouldEqual("src.Select(x + 10)");
        }

        [Fact]
        public void stringify_call_node_with_multiple_parameters()
        {
            Node node = NodeFactory.Build<MethodCallExpression, MethodCallExpressionToken>(null, ExpressionTokens.MethodCall)
                .Add(x => x.Object, NodeFactory.Text("Math"))
                .Add(x => x.Method, "Max")
                .Add(x => x.Arguments, NodeFactory.MultipleItems(NodeFactory.Text("1"), NodeFactory.Text("2")));

            var result = new TextSyntaxNodeVisitor().VisitNode(node).Print();

            result.ShouldEqual("Math.Max(1, 2)");
        }

        [Fact]
        public void stringify_member_init()
        {
            Node node = NodeFactory.Build<NewExpression, NewExpressionToken>(null, ExpressionTokens.New)
                .Add(x => x.Type, "MemberInitSample")
                .Add(x => x.Arguments, NodeFactory.Empty)
                .Add(x => x.Members, NodeFactory.ItemNode(ExpressionTokens.Constant, "test").Then(ExpressionTokens.Bind, "Value"));
            
            var result = new TextSyntaxNodeVisitor().VisitNode(node).Print();

            result.ShouldEqual("New MemberInitSample: Value = test");
        }
    }
}
