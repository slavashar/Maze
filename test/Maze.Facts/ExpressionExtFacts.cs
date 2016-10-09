using System;
using System.Linq.Expressions;
using Maze.Linq;
using Xunit;

namespace Maze.Facts
{
    public class ExpressionExtFacts
    {
        [Fact]
        public void check_switch()
        {
            Expression<Func<string, int>> expr =
                source => Operator.Switch(source).Case("A", 1).Case("B", 2).Default(0);

            var result = new OperatorVisitor().Visit(expr);

            var param = Expression.Parameter(typeof(string), "source");
            var expected =
                Expression.Lambda<Func<string, int>>(
                Expression.Switch(
                    param,
                    Expression.Constant(0),
                    Expression.SwitchCase(Expression.Constant(1), Expression.Constant("A")),
                    Expression.SwitchCase(Expression.Constant(2), Expression.Constant("B"))),
                param);

            result.ShouldEqual(expected);
        }

        [Fact]
        public void check_switch_with_multiple_test_values()
        {
            Expression<Func<string, int>> expr =
                source => Operator.Switch(source).Case(new[] { "A", "B" }, 1).Default(0);

            var result = new OperatorVisitor().Visit(expr);

            var param = Expression.Parameter(typeof(string), "source");
            var expected =
                Expression.Lambda<Func<string, int>>(
                Expression.Switch(
                    param,
                    Expression.Constant(0),
                    Expression.SwitchCase(Expression.Constant(1), Expression.Constant("A"), Expression.Constant("B"))),
                param);

            result.ShouldEqual(expected);
        }

        [Fact]
        public void check_switch_with_variable()
        {
            string case1 = "A", case2 = "B";

            Expression<Func<string, int>> expr =
                source => Operator.Switch(source).Case(case1, 1).Case(case2, 2).Default(0);

            var result = new OperatorVisitor().Visit(expr);

            var param = Expression.Parameter(typeof(string), "source");
            var expected =
                Expression.Lambda<Func<string, int>>(
                Expression.Switch(
                    param,
                    Expression.Constant(0),
                    Expression.SwitchCase(Expression.Constant(1), Expression.Constant("A")),
                    Expression.SwitchCase(Expression.Constant(2), Expression.Constant("B"))),
                param);

            result.ShouldEqual(expected);
        }

        [Fact]
        public void check_if_then_else()
        {
            Expression<Func<string, int>> expr =
                source => Operator.If(source == "test", 1).Else(0);

            var result = new OperatorVisitor().Visit(expr);

            Expression<Func<string, int>> expected =
                source => source == "test" ? 1 : 0;

            result.ShouldEqual(expected);
        }

        [Fact]
        public void check_if_then_elseif()
        {
            Expression<Func<string, int>> expr =
                source => Operator
                    .If(source == "test", 1)
                    .ElseIf(source == "test 2", 2)
                    .Else(0);

            var result = new OperatorVisitor().Visit(expr);

            result.ShouldEqualExpression<Func<string, int>>(
                source => source == "test" ? 1 : source == "test 2" ? 2 : 0);
        }

        [Fact]
        public void check_extermal_expression()
        {
            Expression<Func<int, Result>> map = x => new Result
            {
                Value = x
            };

            Expression<Func<int, Result>> expr =
                source => Operator.Expression(map, source);

            var result = new OperatorVisitor().Visit(expr);

            result.ShouldEqualExpression<Func<int, Result>>(
                source => new Result
                {
                    Value = source
                });
        }

        [Fact]
        public void check_complex_extermal_expression()
        {
            Expression<Func<int, Result>> map = x => new Result
            {
                Value = x
            };

            Expression<Func<int, Result>> expr =
                source => source == 0 ? null : Operator.Expression(map, source);

            var result = new OperatorVisitor().Visit(expr);

            result.ShouldEqualExpression<Func<int, Result>>(
                source => source == 0 ? null : new Result
                {
                    Value = source
                });
        }

        private class Result
        {
            public int Value { get; set; }
        }
    }
}
