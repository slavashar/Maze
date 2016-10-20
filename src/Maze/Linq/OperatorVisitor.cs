using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Maze.Linq
{
    public class OperatorVisitor : ExpressionVisitor
    {
        private static readonly string SwitchDefaultMethodName = ((MethodCallExpression)((Expression<Func<Operator.ISwitch<object, object>, dynamic>>)(x => x.Default(null))).Body).Method.Name;
        private static readonly string SwitchCaseMethodName = ((MethodCallExpression)((Expression<Func<Operator.ISwitchValue<object>, dynamic>>)(x => x.Case<object>(null, null))).Body).Method.Name;

        private static readonly string IfElseMethodName = ((MethodCallExpression)((Expression<Func<Operator.IIfThen<object>, dynamic>>)(x => x.Else(null))).Body).Method.Name;
        private static readonly string IfElseIfMethodName = ((MethodCallExpression)((Expression<Func<Operator.IIfThen<object>, dynamic>>)(x => x.ElseIf(true, null))).Body).Method.Name;

        private static readonly string ExpressionMethodName = new Func<Expression<Func<object, object>>, object, object>(Operator.Expression).GetGenericMethodDefinition().Name;
        
        public Expression<Func<TResult>> Visit<TResult>(Expression<Func<TResult>> expression)
        {
            return (Expression<Func<TResult>>)base.Visit(expression);
        }

        public Expression<Func<T, TResult>> Visit<T, TResult>(Expression<Func<T, TResult>> expression)
        {
            return (Expression<Func<T, TResult>>)base.Visit(expression);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Operator))
            {
                if (node.Method.Name == ExpressionMethodName)
                {
                    var expression = (LambdaExpression)Expression.Lambda(node.Arguments[0]).Compile().DynamicInvoke();

                    var @params = expression.Parameters
                        .Zip(node.Arguments.Skip(1), (param, exp) => new { param, exp })
                        .ToDictionary(x => x.param, x => x.exp);

                    var visitor = new ParamaterReplacement(@params);

                    return visitor.Visit(expression.Body);
                }
            }

            if (node.Method.DeclaringType.EqualsGenericDefinition(typeof(Operator.ISwitch<,>)) &&
                node.Method.Name == SwitchDefaultMethodName)
            {
                var parent = node.Object as MethodCallExpression;

                var cases = new List<SwitchCase>();

                while (parent != null && parent.Method.Name == SwitchCaseMethodName)
                {
                    IEnumerable<Expression> testValues;

                    var type = parent.Method.GetParameters()[0].ParameterType;

                    var values = Expression.Lambda(parent.Arguments[0]).Compile().DynamicInvoke();

                    if (type.EqualsGenericDefinition(typeof(IEnumerable<>)))
                    {
                        testValues = ((IEnumerable)values).Cast<object>().Select(x => Expression.Constant(x));
                    }
                    else
                    {
                        testValues = new[] { Expression.Constant(values) };
                    }

                    cases.Add(Expression.SwitchCase(this.Visit(parent.Arguments[1]), testValues));

                    parent = parent.Object as MethodCallExpression;
                }

                cases.Reverse();

                return Expression.Switch(this.Visit(parent.Arguments[0]), this.Visit(node.Arguments[0]), cases.ToArray());
            }

            if (node.Method.DeclaringType.EqualsGenericDefinition(typeof(Operator.IIfThen<>)) &&
                node.Method.Name == IfElseMethodName)
            {
                var testNode = node.Object as MethodCallExpression;
                var condition = Expression.Condition(this.Visit(testNode.Arguments[0]), this.Visit(testNode.Arguments[1]), this.Visit(node.Arguments[0]));

                while (testNode.Method.Name == IfElseIfMethodName)
                {
                    testNode = testNode.Object as MethodCallExpression;
                    condition = Expression.Condition(this.Visit(testNode.Arguments[0]), this.Visit(testNode.Arguments[1]), condition);
                }

                return condition;
            }

            return base.VisitMethodCall(node);
        }

        private class ParamaterReplacement : ExpressionVisitor
        {
            private readonly IReadOnlyDictionary<ParameterExpression, Expression> parameters;

            public ParamaterReplacement(IReadOnlyDictionary<ParameterExpression, Expression> parameters)
            {
                this.parameters = parameters;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                Expression result;
                return this.parameters.TryGetValue(node, out result) ? result : node;
            }
        }
    }
}
