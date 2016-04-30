using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Maze.Linq
{
    internal class MazeExtVisitor : ExpressionVisitor
    {
        private static readonly string SwitchDefaultMethodName, SwitchCaseMethodName;

        private static readonly string IfElseMethodName, IfElseIfMethodName;

        static MazeExtVisitor()
        {
            SwitchDefaultMethodName = ((MethodCallExpression)((Expression<Func<MazeExt.IMazeSwitch<object, object>, dynamic>>)(x => x.Default(null))).Body).Method.Name;
            SwitchCaseMethodName = ((MethodCallExpression)((Expression<Func<MazeExt.IMazeSwitch<object>, dynamic>>)(x => x.Case<object>(null, null))).Body).Method.Name;

            IfElseMethodName = ((MethodCallExpression)((Expression<Func<MazeExt.IMazeIfThen<object>, dynamic>>)(x => x.Else(null))).Body).Method.Name;
            IfElseIfMethodName = ((MethodCallExpression)((Expression<Func<MazeExt.IMazeIfThen<object>, dynamic>>)(x => x.ElseIf(true))).Body).Method.Name;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType.IsGenericType &&
                node.Method.DeclaringType.GetGenericTypeDefinition() == typeof(MazeExt.IMazeSwitch<,>) &&
                node.Method.Name == SwitchDefaultMethodName)
            {
                var parent = node.Object as MethodCallExpression;

                var cases = new List<SwitchCase>();

                while (parent != null && parent.Method.Name == SwitchCaseMethodName)
                {
                    cases.Add(Expression.SwitchCase(this.Visit(parent.Arguments[1]), this.Visit(parent.Arguments[0])));

                    parent = parent.Object as MethodCallExpression;
                }

                cases.Reverse();

                return Expression.Switch(this.Visit(parent.Arguments[0]), this.Visit(node.Arguments[0]), cases.ToArray());
            }

            if (node.Method.DeclaringType.IsGenericType &&
                node.Method.DeclaringType.GetGenericTypeDefinition() == typeof(MazeExt.IMazeIfThen<>) &&
                node.Method.Name == IfElseMethodName)
            {
                var thenNode = node.Object as MethodCallExpression;
                var testNode = thenNode.Object as MethodCallExpression;
                var condition = Expression.Condition(this.Visit(testNode.Arguments[0]), this.Visit(thenNode.Arguments[0]), this.Visit(node.Arguments[0]));

                while (testNode.Method.Name == IfElseIfMethodName)
                {
                    thenNode = testNode.Object as MethodCallExpression;
                    testNode = thenNode.Object as MethodCallExpression;
                    condition = Expression.Condition(this.Visit(testNode.Arguments[0]), this.Visit(thenNode.Arguments[0]), condition);
                }

                return condition;
            }

            return base.VisitMethodCall(node);
        }
    }
}
