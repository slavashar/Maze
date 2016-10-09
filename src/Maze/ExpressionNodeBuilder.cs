using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Maze.Nodes;

namespace Maze
{
    public class ExpressionNodeBuilder
    {
        public static Node Parse(Expression expression)
        {
            var graph = new ExpressionNodeBuilder();
            return graph.CreateNode(expression);
        }

        public Node CreateNode(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Quote:
                    return this.CreateNode(((UnaryExpression)expression).Operand);

                case ExpressionType.UnaryPlus:
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                    return this.CreateUnaryNode((UnaryExpression)expression, GetUnaryOperator(expression.NodeType), x => x.Operand);

                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.TypeAs:
                    return this.CreateUnaryNode((UnaryExpression)expression, GetUnaryItemOperator(expression.NodeType), x => x.Operand, x => x.Type);

                case ExpressionType.TypeIs:
                    return this.CreateUnaryNode((TypeBinaryExpression)expression, ExpressionTokens.Is, x => x.Expression, x => x.Type);

                case ExpressionType.ArrayLength:
                    throw new NotImplementedException();

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
                case ExpressionType.RightShift:
                case ExpressionType.LeftShift:
                case ExpressionType.ExclusiveOr:
                    return this.CreateBinaryNode((BinaryExpression)expression, GetBinaryOperator(expression.NodeType));

                case ExpressionType.ArrayIndex:
                    return this.CreateUnaryNode((BinaryExpression)expression, ExpressionTokens.Index, x => x.Left, x => x.Right);

                case ExpressionType.Conditional:
                    return this.CreateConditionalNode((ConditionalExpression)expression);

                case ExpressionType.Switch:
                    return this.CreateSwitchNode((SwitchExpression)expression);

                case ExpressionType.Constant:
                    return this.CreateConstantNode((ConstantExpression)expression);

                case ExpressionType.Parameter:
                    return NodeFactory.ItemNode((ParameterExpression)expression, ExpressionTokens.Parameter, ((ParameterExpression)expression).Name);

                case ExpressionType.MemberAccess:
                    return this.CreateMemberAccessNode((MemberExpression)expression);

                case ExpressionType.Call:
                    return this.CreateMethodCallNode((MethodCallExpression)expression);

                case ExpressionType.Lambda:
                    return this.CreateNode(((LambdaExpression)expression).Body);

                case ExpressionType.New:
                    return this.CreateNewNode((NewExpression)expression);

                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    return this.CreateNewArrayNode((NewArrayExpression)expression);

                //case ExpressionType.Invoke:
                //    return this.AddInvocationNode((InvocationExpression)expression);

                case ExpressionType.MemberInit:
                    return this.CreateMemberInitNode((MemberInitExpression)expression);

                //case ExpressionType.ListInit:
                //    return this.AddListInitNode((ListInitExpression)expression);

                default:
                    throw new InvalidOperationException("Unhandled Expression Type: " + expression.NodeType);
            }
        }

        protected Node CreateNode(IEnumerable<Expression> expressions)
        {
            var nodes = expressions.Select(this.CreateNode).ToList();

            if (nodes.Count == 0)
            {
                return NodeFactory.Empty;
            }

            if (nodes.Count == 1)
            {
                return nodes[0];
            }

            return NodeFactory.MultipleItems(nodes);
        }

        protected virtual Node CreateObjectNode(object value)
        {
            if (value == null)
            {
                return NodeFactory.Text("null");
            }

            if (value is string)
            {
                return NodeFactory.Text(value.ToString()).Then(ExpressionTokens.DoubleQuotes);
            }

            if (value is char)
            {
                return NodeFactory.Text(value.ToString()).Then(ExpressionTokens.SingleQuotes);
            }

            if (value.GetType().IsArray)
            {
                var array = (Array)value;

                if (array.Length == 0)
                {
                    return TokenNode<NewArrayExpressionToken>.Create(ExpressionTokens.NewArray, NewArrayExpressionToken.Expressions, NodeFactory.Empty);
                }

                if (array.Length == 1)
                {
                    return TokenNode<NewArrayExpressionToken>.Create(ExpressionTokens.NewArray, NewArrayExpressionToken.Expressions, this.CreateObjectNode(array.GetValue(0)));
                }

                var values = NodeFactory.MultipleItems(array.Cast<object>().Select(this.CreateObjectNode));

                return TokenNode<NewArrayExpressionToken>.Create(ExpressionTokens.NewArray, NewArrayExpressionToken.Expressions, values);
            }

            return NodeFactory.Text(value.ToString());
        }

        protected virtual Node CreateTypeNode(Type type)
        {
            return NodeFactory.Text(type.Name);
        }

        protected virtual Node CreateMethodNode(MethodInfo method)
        {
            return NodeFactory.Text(method.Name);
        }

        protected virtual Node CreateMemberNode(MemberInfo member)
        {
            return NodeFactory.Text(member.Name);
        }

        protected virtual Node CreateConstantNode(ConstantExpression expression)
        {
            return NodeFactory.ItemNode(expression, ExpressionTokens.Constant, this.CreateObjectNode(expression.Value));
        }

        protected virtual Node CreateMemberAccessNode(MemberExpression expression)
        {
            if (expression.Expression == null)
            {
                return this.CreateTypeNode(expression.Member.DeclaringType).Then(expression, ExpressionTokens.Member, this.CreateMemberNode(expression.Member));
            }

            return this.CreateNode(expression.Expression).Then(expression, ExpressionTokens.Member, this.CreateMemberNode(expression.Member));
        }

        protected Node CreateConditionalNode(ConditionalExpression expression)
        {
            return NodeFactory.Build(expression, ExpressionTokens.Conditional)
                .Add(x => x.Test, this.CreateNode(expression.Test))
                .Add(x => x.IfTrue, this.CreateNode(expression.IfTrue))
                .Add(x => x.IfFalse, this.CreateNode(expression.IfFalse));
        }

        protected virtual Node CreateSwitchNode(SwitchExpression switchExpression)
        {
            var cases = switchExpression.Cases
                .Select(@case => (Node)NodeFactory
                        .Build(@case, ExpressionTokens.SwitchCase)
                        .Add(x => x.TestValues, this.CreateNode(@case.TestValues))
                        .Add(x => x.Body, this.CreateNode(@case.Body)))
                .ToList();

            return NodeFactory.Build(switchExpression, ExpressionTokens.Switch)
                .Add(x => x.SwitchValue, this.CreateNode(switchExpression.SwitchValue))
                .Add(x => x.Cases, cases.Count == 0 ? NodeFactory.Empty : cases.Count == 1 ? cases[0] : NodeFactory.MultipleItems(cases))
                .Add(x => x.DefaultBody, this.CreateNode(switchExpression.DefaultBody))
                .ToNode();
        }

        protected virtual Node CreateMethodCallNode(MethodCallExpression expression)
        {
            if (expression.Method.IsSpecialName && (expression.Method.Attributes & MethodAttributes.HideBySig) != 0)
            {
                return this.CreateNode(expression.Object).Then(ExpressionTokens.Index, this.CreateNode(expression.Arguments));
            }

            if (expression.Method.Name == "Join" || expression.Method.Name == "JoinGroup")
            {
                Node join = NodeFactory.Build(expression, ExpressionTokens.Join)
                    .Add(x => x.Method, this.CreateMethodNode(expression.Method))
                    .Add(JoinExpressionToken.Inner, this.CreateNode(expression.Arguments.First()))
                    .Add(JoinExpressionToken.Outer, this.CreateNode(expression.Arguments.ElementAt(1)))
                    .Add(JoinExpressionToken.Key, NodeFactory.BinaryNode(
                            ExpressionTokens.Equal,
                            this.CreateNode(expression.Arguments.ElementAt(2)),
                            this.CreateNode(expression.Arguments.ElementAt(3))));

                return NodeFactory.Build(expression, ExpressionTokens.MethodCall)
                    .Add(x => x.Object, join)
                    .Add(x => x.Method, NodeFactory.Text("Select"))
                    .Add(x => x.Arguments, this.CreateNode(expression.Arguments.ElementAt(4)));
            }

            if (expression.Method.IsDefined(typeof(ExtensionAttribute), false))
            {
                return NodeFactory.Build(expression, ExpressionTokens.MethodCall)
                    .Add(x => x.Object, this.CreateNode(expression.Arguments.First()))
                    .Add(x => x.Method, this.CreateMethodNode(expression.Method))
                    .Add(x => x.Arguments, this.CreateNode(expression.Arguments.Skip(1)));
            }

            if (expression.Object == null)
            {
                return NodeFactory.Build(expression, ExpressionTokens.MethodCall)
                    .Add(x => x.Object, this.CreateTypeNode(expression.Method.DeclaringType))
                    .Add(x => x.Method, this.CreateMethodNode(expression.Method))
                    .Add(x => x.Arguments, this.CreateNode(expression.Arguments));
            }

            return NodeFactory.Build(expression, ExpressionTokens.MethodCall)
                .Add(x => x.Object, this.CreateNode(expression.Object))
                .Add(x => x.Method, this.CreateMethodNode(expression.Method))
                .Add(x => x.Arguments, this.CreateNode(expression.Arguments));
        }

        protected virtual ElementNode<NewExpression, NewExpressionToken> CreateNewNode(NewExpression expression)
        {
            return NodeFactory.Build(expression, ExpressionTokens.New)
                    .Add(x => x.Type, this.CreateTypeNode(expression.Type))
                    .Add(x => x.Arguments, this.CreateNode(expression.Arguments))
                    .Add(x => x.Members, NodeFactory.Empty);
        }

        protected virtual ElementNode<NewExpression, NewExpressionToken> CreateMemberInitNode(MemberInitExpression memberInitExpression)
        {
            var newNode = this.CreateNewNode(memberInitExpression.NewExpression);
            var bindings = memberInitExpression.Bindings.Select(this.CrateBindingNode).ToList();

            if (bindings.Count == 0)
            {
                return newNode;
            }

            if (bindings.Count == 1)
            {
                return newNode.WithNode(NewExpressionToken.Members, bindings[0]);
            }

            return newNode.WithNode(NewExpressionToken.Members, NodeFactory.MultipleItems(bindings));
        }

        protected virtual Node CrateBindingNode(MemberBinding memberBinding)
        {
            switch (memberBinding.BindingType)
            {
                case MemberBindingType.Assignment:
                    return this.CreateMemberAssignmentNode((MemberAssignment)memberBinding);
                case MemberBindingType.MemberBinding:
                    return this.CreateMemberMemberBindingNode((MemberMemberBinding)memberBinding);
                case MemberBindingType.ListBinding:
                    return this.CreateMemberListBindingNode((MemberListBinding)memberBinding);
                default:
                    throw new InvalidOperationException("Unhanded Binding Type: " + memberBinding.BindingType);
            }
        }

        protected virtual Node CreateMemberAssignmentNode(MemberAssignment assignment)
        {
            return this.CreateNode(assignment.Expression).Then(assignment, ExpressionTokens.Bind, assignment.Member.Name);
        }

        protected virtual Node CreateMemberMemberBindingNode(MemberMemberBinding memberMemberBinding)
        {
            var bindings = memberMemberBinding.Bindings.Select(this.CrateBindingNode).ToList();

            if (bindings.Count == 0)
            {
                return NodeFactory.Empty;
            }

            if (bindings.Count == 1)
            {
                return bindings[0].Then(memberMemberBinding, ExpressionTokens.Bind, memberMemberBinding.Member.Name);
            }

            return NodeFactory.MultipleItems(bindings).Then(memberMemberBinding, ExpressionTokens.Bind, memberMemberBinding.Member.Name);
        }

        protected virtual Node CreateMemberListBindingNode(MemberListBinding memberListBinding)
        {
            var initializers = memberListBinding.Initializers.Select(init => this.CreateNode(init.Arguments)).ToList();

            if (initializers.Count == 0)
            {
                return NodeFactory.Empty;
            }

            if (initializers.Count == 1)
            {
                return initializers[0].Then(memberListBinding, ExpressionTokens.Bind, memberListBinding.Member.Name);
            }

            return NodeFactory.MultipleItems(initializers).Then(memberListBinding, ExpressionTokens.Bind, memberListBinding.Member.Name);
        }

        protected virtual Node CreateNewArrayNode(NewArrayExpression expression)
        {
            return NodeFactory.Build(expression, ExpressionTokens.NewArray)
                    .Add(x => x.Expressions, this.CreateNode(expression.Expressions));
        }

        private static UnaryToken GetUnaryOperator(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                    return ExpressionTokens.Negate;

                case ExpressionType.UnaryPlus:
                case ExpressionType.Not:
                case ExpressionType.ArrayLength:
                case ExpressionType.Quote:
                    throw new NotImplementedException();

                default:
                    throw new InvalidOperationException();
            }
        }

        private static UnaryItemToken GetUnaryItemOperator(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.TypeAs:
                    return ExpressionTokens.As;

                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    return ExpressionTokens.Convert;

                default:
                    throw new InvalidOperationException();
            }
        }

        private static BinaryToken GetBinaryOperator(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    return ExpressionTokens.Add;
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return ExpressionTokens.Subtract;
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    return ExpressionTokens.Multiply;
                case ExpressionType.Divide:
                    return ExpressionTokens.Divide;
                case ExpressionType.Modulo:
                    return ExpressionTokens.Modulo;
                case ExpressionType.Power:
                    return ExpressionTokens.Power;
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    return ExpressionTokens.And;
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return ExpressionTokens.Or;
                case ExpressionType.LessThan:
                    return ExpressionTokens.LessThan;
                case ExpressionType.LessThanOrEqual:
                    return ExpressionTokens.LessThanOrEqual;
                case ExpressionType.GreaterThan:
                    return ExpressionTokens.GreaterThan;
                case ExpressionType.GreaterThanOrEqual:
                    return ExpressionTokens.GreaterThanOrEqual;
                case ExpressionType.Equal:
                    return ExpressionTokens.Equal;
                case ExpressionType.NotEqual:
                    return ExpressionTokens.NotEqual;
                case ExpressionType.Coalesce:
                    return ExpressionTokens.Coalesce;
                //case ExpressionType.RightShift:
                //    return ">>";
                //case ExpressionType.LeftShift:
                //    return "<<";
                //case ExpressionType.ExclusiveOr:
                //    return "^";
                default:
                    throw new NotImplementedException();
            }
        }

        private ElementNode<T, UnaryToken> CreateUnaryNode<T>(T element, UnaryToken token, Func<T, Expression> parent)
        {
            return this.CreateNode(parent(element)).Then(element, token);
        }

        private ElementNode<T, UnaryItemToken> CreateUnaryNode<T>(T element, UnaryItemToken token, Func<T, Expression> parent, Func<T, Expression> item)
        {
            return this.CreateNode(parent(element)).Then(element, token, this.CreateNode(item(element)));
        }

        private ElementNode<T, UnaryItemToken> CreateUnaryNode<T>(T element, UnaryItemToken token, Func<T, Expression> parent, Func<T, Type> item)
        {
            var parentNode = this.CreateNode(parent.Invoke(element));
            var itemNode = this.CreateTypeNode(item.Invoke(element));

            return parentNode.Then(element, token, itemNode);
        }

        private ElementNode<BinaryExpression, BinaryToken> CreateBinaryNode(BinaryExpression element, BinaryToken token)
        {
            var leftNode = this.CreateNode(element.Left);
            var rightNode = this.CreateNode(element.Right);

            if (leftNode is ElementNode<BinaryExpression> && TypeExt.IsNumericType(((ElementNode<BinaryExpression>)leftNode).Element.Type))
            {
                leftNode = leftNode.Then(ExpressionTokens.Brackets);
            }

            if (rightNode is ElementNode<BinaryExpression> && TypeExt.IsNumericType(((ElementNode<BinaryExpression>)rightNode).Element.Type))
            {
                rightNode = rightNode.Then(ExpressionTokens.Brackets);
            }

            return NodeFactory.BinaryNode(element, token, leftNode, rightNode);
        }
    }
}
