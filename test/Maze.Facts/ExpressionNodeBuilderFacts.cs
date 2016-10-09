using System;
using System.Linq;
using System.Linq.Expressions;
using Maze.Nodes;
using Xunit;

namespace Maze.Facts
{
    public class ExpressionNodeBuilderFacts
    {
        [Fact]
        public void parse_paramater_expression()
        {
            Expression<Func<int, int>> map = x => x;

            var result = ExpressionNodeBuilder.Parse(map.Body).ShouldBeType<ElementNode<ParameterExpression, ItemToken>>();
            
            result.Token.ShouldBe(ExpressionTokens.Parameter);
            result[ItemToken.Item].ShouldBeType<TextNode>().Value.ShouldEqual("x");
        }

        [Fact]
        public void parse_constant_expression()
        {
            Expression<Func<int>> map = () => 100;

            var result = ExpressionNodeBuilder.Parse(map.Body).ShouldBeType<ElementNode<ConstantExpression, ItemToken>>();

            result.Token.ShouldBe(ExpressionTokens.Constant);
            result[ItemToken.Item].ShouldBeType<TextNode>().Value.ShouldEqual("100");
        }

        [Fact]
        public void parse_string_constant_expression()
        {
            Expression<Func<string>> map = () => "test";

            var result = ExpressionNodeBuilder.Parse(map.Body).ShouldBeType<ElementNode<ConstantExpression, ItemToken>>();
            
            result.Token.ShouldBe(ExpressionTokens.Constant);
            var constant = result[ItemToken.Item].ShouldBeType<TokenNode<UnaryToken>>();

            constant.Token.ShouldBe(ExpressionTokens.DoubleQuotes);
            constant[UnaryToken.Parent].ShouldBeType<TextNode>().Value.ShouldEqual("test");
        }

        [Fact]
        public void parse_null_constant_expression()
        {
            Expression<Func<string>> map = () => null;

            var result = ExpressionNodeBuilder.Parse(map.Body).ShouldBeType<ElementNode<ConstantExpression, ItemToken>>();

            result.Token.ShouldBe(ExpressionTokens.Constant);
            result[ItemToken.Item].ShouldBeType<TextNode>().Value.ShouldEqual("null");
        }

        [Fact]
        public void parse_property_expression()
        {
            Expression<Func<DateTime, int>> map = x => x.Year;

            var result = ExpressionNodeBuilder.Parse(map.Body).ShouldBeType<ElementNode<MemberExpression, UnaryItemToken>>();

            result.Token.ShouldBe(ExpressionTokens.Member);
            result[UnaryItemToken.Parent].ShouldBeType<ElementNode<ParameterExpression, ItemToken>>();
            result[UnaryItemToken.Item].ShouldBeType<TextNode>().Value.ShouldEqual("Year");
        }

        [Fact]
        public void parse_static_field_expression()
        {
            Expression<Func<string>> map = () => string.Empty;

            var result = ExpressionNodeBuilder.Parse(map.Body).ShouldBeType<ElementNode<MemberExpression, UnaryItemToken>>();

            result.Token.ShouldBe(ExpressionTokens.Member);
            result[UnaryItemToken.Parent].ShouldBeType<TextNode>().Value.ShouldEqual("String");
            result[UnaryItemToken.Item].ShouldBeType<TextNode>().Value.ShouldEqual("Empty");
        }

        [Fact]
        public void parse_static_property_expression()
        {
            Expression<Func<DateTime>> map = () => DateTime.Today;

            var result = ExpressionNodeBuilder.Parse(map.Body).ShouldBeType<ElementNode<MemberExpression, UnaryItemToken>>();

            result.Token.ShouldBe(ExpressionTokens.Member);
            result[UnaryItemToken.Parent].ShouldBeType<TextNode>().Value.ShouldEqual("DateTime");
            result[UnaryItemToken.Item].ShouldBeType<TextNode>().Value.ShouldEqual("Today");
        }

        [Fact]
        public void parse_unary_expression()
        {
            Expression<Func<int, int>> map = x => -x;

            var result = ExpressionNodeBuilder.Parse(map.Body).ShouldBeType<ElementNode<UnaryExpression, UnaryToken>>();

            result.Token.ShouldBe(ExpressionTokens.Negate);
            result[UnaryToken.Parent].ShouldBeType<ElementNode<ParameterExpression, ItemToken>>();
        }

        [Fact]
        public void parse_binary_expression()
        {
            Expression<Func<int, int>> map = x => x + 10;

            var result = ExpressionNodeBuilder.Parse(map.Body).ShouldBeType<ElementNode<BinaryExpression, BinaryToken>>();

            result.Token.ShouldBe(ExpressionTokens.Add);
            result[BinaryToken.Left].ShouldBeType<ElementNode<ParameterExpression, ItemToken>>();
            result[BinaryToken.Right].ShouldBeType<ElementNode<ConstantExpression, ItemToken>>();
        }

        [Fact]
        public void parse_multiple_number_binary_expression()
        {
            Expression<Func<int, int>> map = x => x + 10 + 100;

            var result = ExpressionNodeBuilder.Parse(map.Body).ShouldBeType<ElementNode<BinaryExpression, BinaryToken>>();

            result.Stringify().ShouldEqual("(x + 10) + 100");
        }

        [Fact]
        public void parse_concatenation_expression()
        {
            Expression<Func<string, string>> map = x => x + "one" + "two";

            var result = ExpressionNodeBuilder.Parse(map.Body).ShouldBeType<ElementNode<BinaryExpression, BinaryToken>>();

            result.Stringify().ShouldEqual("x + \"one\" + \"two\"");
        }

        [Fact]
        public void parse_complex_binary_expression()
        {
            Expression<Func<int, int>> map = x => (x + 10) / 100;

            var result = ExpressionNodeBuilder.Parse(map.Body).ShouldBeType<ElementNode<BinaryExpression, BinaryToken>>();

            var brackets = result[BinaryToken.Left].ShouldBeType<TokenNode<UnaryToken>>();

            brackets.Token.ShouldBe(ExpressionTokens.Brackets);
        }

        [Fact]
        public void parse_coalesce_expression()
        {
            Expression<Func<int?, int>> map = x => x ?? 10;

            var result = ExpressionNodeBuilder.Parse(map.Body).ShouldBeType<ElementNode<BinaryExpression, BinaryToken>>();

            result.Token.ShouldBe(ExpressionTokens.Coalesce);
            result[BinaryToken.Left].ShouldBeType<ElementNode<ParameterExpression, ItemToken>>();
            result[BinaryToken.Right].ShouldBeType<ElementNode<ConstantExpression, ItemToken>>();
        }

        [Fact]
        public void parse_array_index_expression()
        {
            Expression<Func<int[], int>> map = x => x[0];

            var result = ExpressionNodeBuilder.Parse(map.Body).ShouldBeType<ElementNode<BinaryExpression, UnaryItemToken>>();

            result.Token.ShouldBe(ExpressionTokens.Index);
            result[UnaryItemToken.Parent].ShouldBeType<ElementNode<ParameterExpression, ItemToken>>();
            result[UnaryItemToken.Item].ShouldBeType<ElementNode<ConstantExpression, ItemToken>>();
        }

        [Fact]
        public void parse_index_expression()
        {
            Expression<Func<string, char>> map = x => x[0];

            var result = ExpressionNodeBuilder.Parse(map.Body).ShouldBeType<TokenNode<UnaryItemToken>>();

            result.Token.ShouldBe(ExpressionTokens.Index);
            result[UnaryItemToken.Parent].ShouldBeType<ElementNode<ParameterExpression, ItemToken>>();
            result[UnaryItemToken.Item].ShouldBeType<ElementNode<ConstantExpression, ItemToken>>();
        }

        [Fact]
        public void parse_type_as_expression()
        {
            Expression<Func<string, object>> map = x => x as object;

            var result = ExpressionNodeBuilder.Parse(map.Body).ShouldBeType<ElementNode<UnaryExpression, UnaryItemToken>>();

            result.Token.ShouldBe(ExpressionTokens.As);
            result[UnaryItemToken.Parent].ShouldBeType<ElementNode<ParameterExpression, ItemToken>>();
            result[UnaryItemToken.Item].ShouldBeType<TextNode>().Value.ShouldEqual("Object");
        }

        [Fact]
        public void parse_type_is_expression()
        {
            Expression<Func<string, bool>> map = x => x is object;

            var result = ExpressionNodeBuilder.Parse(map.Body).ShouldBeType<ElementNode<TypeBinaryExpression, UnaryItemToken>>();

            result.Token.ShouldBe(ExpressionTokens.Is);
        }

        [Fact]
        public void parse_convert_expression()
        {
            Expression<Func<string, object>> map = x => (object)x;

            var result = ExpressionNodeBuilder.Parse(map.Body).ShouldBeType<ElementNode<UnaryExpression, UnaryItemToken>>();

            result.Token.ShouldBe(ExpressionTokens.Convert);
            result[UnaryItemToken.Parent].ShouldBeType<ElementNode<ParameterExpression, ItemToken>>();
            result[UnaryItemToken.Item].ShouldBeType<TextNode>().Value.ShouldEqual("Object");
        }

        [Fact]
        public void parse_conditional_expression()
        {
            Expression<Func<int, string>> map = x => x == 0 ? string.Empty : null;

            var result = ExpressionNodeBuilder.Parse(map.Body).ShouldBeType<ElementNode<ConditionalExpression, ConditionalExpressionToken>>();

            result.Token.ShouldBe(ExpressionTokens.Conditional);
            result[ConditionalExpressionToken.Test].Stringify().ShouldEqual("x == 0");
            result[ConditionalExpressionToken.IfTrue].Stringify().ShouldEqual("String.Empty");
            result[ConditionalExpressionToken.IfFalse].Stringify().ShouldEqual("null");
        }

        [Fact]
        public void parse_a_object_call_expression()
        {
            Expression<Func<int, string>> map = value => value.ToString();

            var result = ExpressionNodeBuilder.Parse(map.Body).ShouldBeType<ElementNode<MethodCallExpression, MethodCallExpressionToken>>();

            result.Token.ShouldBe(ExpressionTokens.MethodCall);
            result[MethodCallExpressionToken.Name].ShouldBeType<TextNode>().Value.ShouldEqual("ToString");
            result[MethodCallExpressionToken.Object].ShouldBeType<ElementNode<ParameterExpression, ItemToken>>();
            result[MethodCallExpressionToken.Arguments].Kind.ShouldEqual(NodeKind.Empty);
        }

        [Fact]
        public void parse_a_static_call_expression()
        {
            Expression<Func<string>> map = () => string.Concat("a", "b");

            var result = ExpressionNodeBuilder.Parse(map.Body).ShouldBeType<ElementNode<MethodCallExpression, MethodCallExpressionToken>>();

            result.Token.ShouldBe(ExpressionTokens.MethodCall);
            result[MethodCallExpressionToken.Name].ShouldBeType<TextNode>().Value.ShouldEqual("Concat");
            result[MethodCallExpressionToken.Object].ShouldBeType<TextNode>().Value.ShouldEqual("String");
            result[MethodCallExpressionToken.Arguments].ShouldBeType<MultiItemNode>().Count().ShouldEqual(2);
        }

        [Fact]
        public void parse_select_call_expression()
        {
            Expression<Func<IQueryable<int>, IQueryable<int>>> map = src => src.Select(x => x + 10);

            var result = ExpressionNodeBuilder.Parse(map.Body).ShouldBeType<ElementNode<MethodCallExpression, MethodCallExpressionToken>>();

            result.Token.ShouldBe(ExpressionTokens.MethodCall);
            result[MethodCallExpressionToken.Name].ShouldBeType<TextNode>().Value.ShouldEqual("Select");
            result[MethodCallExpressionToken.Object].ShouldBeType<ElementNode<ParameterExpression, ItemToken>>();
            (result[MethodCallExpressionToken.Arguments] as ITokenNode).Token.ShouldBe(ExpressionTokens.Add);
        }

        [Fact]
        public void parse_join_call_expression()
        {
            Expression<Func<IQueryable<JoinSample>, IQueryable<int>>> map =
                src => src.Join(new[] { new { item = 1 } }, x1 => x1.key, x2 => x2.item, (x1, x2) => x1.value + 10);

            var result = ExpressionNodeBuilder.Parse(map.Body).ShouldBeType<ElementNode<MethodCallExpression, MethodCallExpressionToken>>();


            result[MethodCallExpressionToken.Name].ShouldBeType<TextNode>().Value.ShouldEqual("Select");
            var join = result[MethodCallExpressionToken.Object].ShouldBeType<ElementNode<MethodCallExpression, JoinExpressionToken>>();
        }

        [Fact]
        public void parse_new_expression()
        {
            Expression<Func<MemberInitSample>> map = () => new MemberInitSample();

            var result = ExpressionNodeBuilder.Parse(map.Body).ShouldBeType<ElementNode<NewExpression, NewExpressionToken>>();

            result.Token.ShouldBe(ExpressionTokens.New);
            result[NewExpressionToken.Type].ShouldBeType<TextNode>().Value.ShouldEqual("MemberInitSample");
            result[NewExpressionToken.Arguments].ShouldBe(NodeFactory.Empty);
            result[NewExpressionToken.Members].ShouldBe(NodeFactory.Empty);
        }

        [Fact]
        public void parse_constractor_expression()
        {
            Expression<Func<string, MemberInitSample>> map = x => new MemberInitSample(x);

            var result = ExpressionNodeBuilder.Parse(map.Body).ShouldBeType<ElementNode<NewExpression, NewExpressionToken>>();

            result.Token.ShouldBe(ExpressionTokens.New);
            result[NewExpressionToken.Type].ShouldBeType<TextNode>().Value.ShouldEqual("MemberInitSample");
            result[NewExpressionToken.Arguments].ShouldBeType<ElementNode<ParameterExpression, ItemToken>>();
            result[NewExpressionToken.Members].ShouldBe(NodeFactory.Empty);
        }

        [Fact]
        public void parse_member_init_expression()
        {
            Expression<Func<MemberInitSample>> map = () => new MemberInitSample
            {
                Value = "test"
            };

            var result = ExpressionNodeBuilder.Parse(map.Body).ShouldBeType<ElementNode<NewExpression, NewExpressionToken>>();

            result.Token.ShouldBe(ExpressionTokens.New);
            result[NewExpressionToken.Type].ShouldBeType<TextNode>().Value.ShouldEqual("MemberInitSample");
            result[NewExpressionToken.Arguments].ShouldBe(NodeFactory.Empty);
            result[NewExpressionToken.Members].ShouldBeType<ElementNode<MemberAssignment, UnaryItemToken>>();
        }

        [Fact]
        public void parse_member_constractor_expression()
        {
            Expression<Func<string, MemberInitSample>> map = x => new MemberInitSample(x)
            {
                Value = "test"
            };

            var result = ExpressionNodeBuilder.Parse(map.Body).ShouldBeType<ElementNode<NewExpression, NewExpressionToken>>();

            result.Token.ShouldBe(ExpressionTokens.New);
            result[NewExpressionToken.Type].ShouldBeType<TextNode>().Value.ShouldEqual("MemberInitSample");
            result[NewExpressionToken.Arguments].ShouldBeType<ElementNode<ParameterExpression, ItemToken>>();
            result[NewExpressionToken.Members].ShouldBeType<ElementNode<MemberAssignment, UnaryItemToken>>();
        }

        [Fact]
        public void parse_new_array_expression()
        {
            Expression<Func<int[]>> map = () => new [] { 1 };

            var result = ExpressionNodeBuilder.Parse(map.Body).ShouldBeType<ElementNode<NewArrayExpression, NewArrayExpressionToken>>();

            result.Token.ShouldBe(ExpressionTokens.NewArray);
            result[NewArrayExpressionToken.Expressions].ShouldBeType<ElementNode<ConstantExpression, ItemToken>>();
        }

        private class MemberInitSample
        {
            public MemberInitSample()
            {
            }

            public MemberInitSample(string value)
            {
                this.Value = value;
            }

            public string Value { get; set; }
        }

        private class JoinSample
        {
            public object key { get; internal set; }
            public int value { get; internal set; }
        }
    }
}