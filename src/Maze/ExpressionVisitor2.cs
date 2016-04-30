using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Maze
{
    public class ExpressionVisitor2
    {
        [System.Diagnostics.DebuggerStepThrough]
        public Expression Visit(Expression exp)
        {
            if (exp == null)
            {
                return exp;
            }

            switch (exp.NodeType)
            {
                case ExpressionType.UnaryPlus:
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.ArrayLength:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                    return this.VisitUnary((UnaryExpression)exp);
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.Power:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.Coalesce:
                case ExpressionType.ArrayIndex:
                case ExpressionType.RightShift:
                case ExpressionType.LeftShift:
                case ExpressionType.ExclusiveOr:
                    return this.VisitBinary((BinaryExpression)exp);
                case ExpressionType.TypeIs:
                    return this.VisitTypeIs((TypeBinaryExpression)exp);
                case ExpressionType.Conditional:
                    return this.VisitConditional((ConditionalExpression)exp);
                case ExpressionType.Constant:
                    return this.VisitConstant((ConstantExpression)exp);
                case ExpressionType.Parameter:
                    return this.VisitParameter((ParameterExpression)exp);
                case ExpressionType.MemberAccess:
                    return this.VisitMemberAccess((MemberExpression)exp);
                case ExpressionType.Call:
                    return this.VisitMethodCall((MethodCallExpression)exp);
                case ExpressionType.Lambda:
                    return this.VisitLambda((LambdaExpression)exp);
                case ExpressionType.Switch:
                    return this.VisitSwitcha((SwitchExpression)exp);
                case ExpressionType.New:
                    return this.VisitNew((NewExpression)exp);
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    return this.VisitNewArray((NewArrayExpression)exp);
                case ExpressionType.Invoke:
                    return this.VisitInvocation((InvocationExpression)exp);
                case ExpressionType.MemberInit:
                    return this.VisitMemberInit((MemberInitExpression)exp);
                case ExpressionType.ListInit:
                    return this.VisitListInit((ListInitExpression)exp);
                default:
                    throw new InvalidOperationException("Unhandled Expression Type: " + exp.NodeType);
            }
        }

        public IEnumerable<Expression> Visit(IEnumerable<Expression> expressions)
        {
            return VisitCollection(
                System.Collections.Immutable.ImmutableList.ToImmutableList(expressions), this.Visit);
        }

        [System.Diagnostics.DebuggerStepThrough]
        protected static IReadOnlyList<T> VisitCollection<T>(IReadOnlyList<T> original, Func<T, T> visitor)
        {
            List<T> list = null;

            for (int i = 0, n = original.Count; i < n; i++)
            {
                T p = visitor(original[i]);

                if (list != null)
                {
                    list.Add(p);
                }
                else if (!object.ReferenceEquals(p, original[i]))
                {
                    list = new List<T>(n);
                    for (int j = 0; j < i; j++)
                    {
                        list.Add(original[j]);
                    }

                    list.Add(p);
                }
            }

            if (list != null)
            {
                return list;
            }

            return original;
        }

        protected virtual MemberBinding VisitBinding(MemberBinding binding)
        {
            switch (binding.BindingType)
            {
                case MemberBindingType.Assignment:
                    return this.VisitMemberAssignment((MemberAssignment)binding);
                case MemberBindingType.MemberBinding:
                    return this.VisitMemberMemberBinding((MemberMemberBinding)binding);
                case MemberBindingType.ListBinding:
                    return this.VisitMemberListBinding((MemberListBinding)binding);
                default:
                    throw new InvalidOperationException("Unhandled Binding Type: " + binding.BindingType);
            }
        }

        protected virtual MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
        {
            var expr = this.Visit(assignment.Expression);

            if (expr == assignment.Expression)
            {
                return assignment;
            }

            return Expression.Bind(assignment.Member, expr);
        }

        protected virtual MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
        {
            var bindings = VisitCollection(binding.Bindings, this.VisitBinding);

            if (bindings == binding.Bindings)
            {
                return binding;
            }

            return Expression.MemberBind(binding.Member, bindings);
        }

        protected virtual MemberListBinding VisitMemberListBinding(MemberListBinding binding)
        {
            var initializers = VisitCollection(binding.Initializers, this.VisitElementInitializer);

            if (initializers == binding.Initializers)
            {
                return binding;
            }

            return Expression.ListBind(binding.Member, initializers);
        }

        protected virtual ElementInit VisitElementInitializer(ElementInit initializer)
        {
            var arguments = VisitCollection(initializer.Arguments, this.Visit);

            if (arguments == initializer.Arguments)
            {
                return initializer;
            }

            return Expression.ElementInit(initializer.AddMethod, arguments);
        }

        protected virtual Expression VisitUnary(UnaryExpression unary)
        {
            var operand = this.Visit(unary.Operand);

            if (operand == unary.Operand)
            {
                return unary;
            }

            return Expression.MakeUnary(unary.NodeType, operand, unary.Type, unary.Method);
        }

        protected virtual Expression VisitBinary(BinaryExpression binary)
        {
            var left = this.Visit(binary.Left);
            var right = this.Visit(binary.Right);
            var conversion = this.Visit(binary.Conversion);

            if (left == binary.Left && right == binary.Right && conversion == binary.Conversion)
            {
                return binary;
            }

            if (binary.NodeType == ExpressionType.Coalesce && binary.Conversion != null)
            {
                return Expression.Coalesce(left, right, conversion as LambdaExpression);
            }

            return Expression.MakeBinary(binary.NodeType, left, right, binary.IsLiftedToNull, binary.Method);
        }

        protected virtual Expression VisitTypeIs(TypeBinaryExpression typeBinary)
        {
            var expr = this.Visit(typeBinary.Expression);

            if (expr == typeBinary.Expression)
            {
                return typeBinary;
            }

            return Expression.TypeIs(expr, typeBinary.TypeOperand);
        }

        protected virtual Expression VisitConstant(ConstantExpression constant)
        {
            return constant;
        }

        protected virtual Expression VisitConditional(ConditionalExpression conditional)
        {
            var test = this.Visit(conditional.Test);
            var ifTrue = this.Visit(conditional.IfTrue);
            var ifFalse = this.Visit(conditional.IfFalse);

            if (test == conditional.Test && ifTrue == conditional.IfTrue && ifFalse == conditional.IfFalse)
            {
                return conditional;
            }

            return Expression.Condition(test, ifTrue, ifFalse);
        }

        protected virtual Expression VisitParameter(ParameterExpression parameter)
        {
            return parameter;
        }

        protected virtual Expression VisitMemberAccess(MemberExpression member)
        {
            var exp = this.Visit(member.Expression);

            if (exp == member.Expression)
            {
                return member;
            }

            return Expression.MakeMemberAccess(exp, member.Member);
        }

        protected virtual Expression VisitMethodCall(MethodCallExpression methodCall)
        {
            var obj = this.Visit(methodCall.Object);
            var args = VisitCollection(methodCall.Arguments, this.Visit);

            if (obj == methodCall.Object && args == methodCall.Arguments)
            {
                return methodCall;
            }

            return Expression.Call(obj, methodCall.Method, args);
        }

        protected virtual Expression VisitLambda(LambdaExpression lambda)
        {
            var body = this.Visit(lambda.Body);

            if (body == lambda.Body)
            {
                return lambda;
            }

            return Expression.Lambda(lambda.Type, body, lambda.Parameters);
        }

        protected virtual Expression VisitSwitcha(SwitchExpression switchexpr)
        {
            var value = this.Visit(switchexpr.SwitchValue);

            var cases = VisitCollection(switchexpr.Cases, this.VisitSwitchCase);

            var defaultValue = this.Visit(switchexpr.DefaultBody);

            if (value == switchexpr.SwitchValue && cases == switchexpr.Cases && defaultValue == switchexpr.DefaultBody)
            {
                return switchexpr;
            }

            return Expression.Switch(value, defaultValue, null, cases);
        }

        protected virtual SwitchCase VisitSwitchCase(SwitchCase switchcase)
        {
            var body = this.Visit(switchcase.Body);

            var values = VisitCollection(switchcase.TestValues, this.Visit);

            if (body == switchcase.Body && values == switchcase.TestValues)
            {
                return switchcase;
            }

            return Expression.SwitchCase(body, values);
        }

        protected virtual NewExpression VisitNew(NewExpression newexpr)
        {
            var args = VisitCollection(newexpr.Arguments, this.Visit);

            if (args == newexpr.Arguments)
            {
                return newexpr;
            }

            if (newexpr.Members != null)
            {
                return Expression.New(newexpr.Constructor, args, newexpr.Members);
            }

            return Expression.New(newexpr.Constructor, args);
        }

        protected virtual Expression VisitMemberInit(MemberInitExpression init)
        {
            var expr = this.VisitNew(init.NewExpression);
            var bindings = VisitCollection(init.Bindings, this.VisitBinding);

            if (expr == init.NewExpression && bindings == init.Bindings)
            {
                return init;
            }

            return Expression.MemberInit(expr, bindings);
        }

        protected virtual Expression VisitListInit(ListInitExpression init)
        {
            var expr = this.VisitNew(init.NewExpression);
            var initializers = VisitCollection(init.Initializers, this.VisitElementInitializer);

            if (expr == init.NewExpression && initializers == init.Initializers)
            {
                return init;
            }

            return Expression.ListInit(expr, initializers);
        }

        protected virtual Expression VisitNewArray(NewArrayExpression newArray)
        {
            var exprs = VisitCollection(newArray.Expressions, this.Visit);

            if (exprs == newArray.Expressions)
            {
                return newArray;
            }

            if (newArray.NodeType == ExpressionType.NewArrayInit)
            {
                return Expression.NewArrayInit(newArray.Type.GetElementType(), exprs);
            }

            return Expression.NewArrayBounds(newArray.Type.GetElementType(), exprs);
        }

        protected virtual Expression VisitInvocation(InvocationExpression invocation)
        {
            var args = VisitCollection(invocation.Arguments, this.Visit);
            var expr = this.Visit(invocation.Expression);

            if (args == invocation.Arguments && expr == invocation.Expression)
            {
                return invocation;
            }

            return Expression.Invoke(expr, args);
        }
    }
}
