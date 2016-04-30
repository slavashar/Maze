using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Maze.Nodes;

namespace Maze
{
    public class MappingGraph
    {
        public static Node CreateNode(Expression expression)
        {
            var graph = new MappingGraph();
            return graph.AddNode(expression);
        }

        public Node AddNode(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Quote:
                    return this.AddNode(((UnaryExpression)expression).Operand);

                case ExpressionType.UnaryPlus:
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                    return this.CreateUnaryNode((UnaryExpression)expression, GetUnaryOperator(expression.NodeType), x => x.Operand);

                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.TypeAs:
                    return this.CreateUnaryNode((UnaryExpression)expression, GetUnaryItemOperator(expression.NodeType), x => x.Operand, x => x.Type);

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
                case ExpressionType.ArrayIndex:
                case ExpressionType.RightShift:
                case ExpressionType.LeftShift:
                case ExpressionType.ExclusiveOr:
                    return this.CreateBinaryNode((BinaryExpression)expression, GetBinaryOperator(expression.NodeType), x => x.Left, x => x.Right);

                case ExpressionType.TypeIs:
                    return this.CreateUnaryNode((TypeBinaryExpression)expression, Tokens.None, x => x.Expression);

                case ExpressionType.Conditional:
                    return this.CreateItemsNode((ConditionalExpression)expression, Tokens.None, x => x.Test, x => x.IfTrue, x => x.IfFalse);

                case ExpressionType.Switch:
                    return this.AddSwitchaNode((SwitchExpression)expression);

                case ExpressionType.Constant:
                    return this.AddConstantNode((ConstantExpression)expression);

                case ExpressionType.Parameter:
                    return this.CreateItemsNode((ParameterExpression)expression, Tokens.Parameter, x => x.Name);

                case ExpressionType.MemberAccess:
                    return this.AddMemberAccessNode((MemberExpression)expression);

                case ExpressionType.Call:
                    return this.AddMethodCallNode((MethodCallExpression)expression);

                case ExpressionType.Lambda:
                    return this.AddNode(((LambdaExpression)expression).Body);

                case ExpressionType.New:
                    return this.AddNewNode((NewExpression)expression);

                //case ExpressionType.NewArrayInit:
                //case ExpressionType.NewArrayBounds:
                //    return this.AddNewArrayNode((NewArrayExpression)expression);

                //case ExpressionType.Invoke:
                //    return this.AddInvocationNode((InvocationExpression)expression);

                case ExpressionType.MemberInit:
                    return this.AddMemberInitNode((MemberInitExpression)expression);

                //case ExpressionType.ListInit:
                //    return this.AddListInitNode((ListInitExpression)expression);

                default:
                    throw new InvalidOperationException("Unhandled Expression Type: " + expression.NodeType);
            }
        }

        private Node ExtractNode<T>(T element, Expression<Func<T, object>> selector)
        {
            var value = selector.Compile().Invoke(element);

            if (value == null)
            {
                return null;
            }

            if (value is Expression)
            {
                return this.AddNode((Expression)value);
            }

            if (value is MemberInfo)
            {
                return NodeFactory.Text(((MemberInfo)value).Name);
            }

            if (value is Type)
            {
                return NodeFactory.Text(((Type)value).Name);
            }

            if (value is ReadOnlyCollection<Expression>)
            {
                var collection = (ReadOnlyCollection<Expression>)value;

                if (collection.Count == 0)
                {
                    return NodeFactory.Empty;
                }

                if (collection.Count == 1)
                {
                    return this.AddNode(collection[0]);
                }

                throw new InvalidOperationException();
            }

            return NodeFactory.Text(value.ToString());
        }

        private Node CreateItemsNode<T>(
            T element,
            Token token,
            params Expression<Func<T, object>>[] items)
        {
            if (items.Length == 0)
            {
                throw new InvalidOperationException();
            }

            if (items.Length == 1)
            {
                return NodeFactory.ItemNode(element, token == Tokens.None ? Tokens.None : (ItemToken)token, items[0], this.ExtractNode(element, items[0]));
            }

            var result = NodeFactory.Node(element).AddItem(items[0], this.ExtractNode(element, items[0]));

            foreach (var item in items.Skip(1))
            {
                result = result.AddItem(item, this.ExtractNode(element, item));
            }

            return result;
        }

        private Node CreateUnaryNode<T>(
            T element,
            Token token,
            Expression<Func<T, object>> parent,
            params Expression<Func<T, object>>[] items)
        {
            var parentNode = this.ExtractNode(element, parent);

            if (items.Length == 0)
            {
                return NodeFactory.Node(element, token == Tokens.None ? Tokens.None : (UnaryToken)token, parent, parentNode);
            }

            if (items.Length == 1)
            {
                var itemNode = this.ExtractNode(element, items[0]);

                return NodeFactory.Node(element, token == Tokens.None ? Tokens.None : (UnaryItemToken)token, parent, parentNode, items[0], itemNode);
            }

            var result = NodeFactory.Node(element).AddParent(parent, parentNode);

            foreach (var item in items)
            {
                result = result.AddItem(item, this.ExtractNode(element, item));
            }

            return result;
        }

        private Node CreateBinaryNode<T>(
            T element,
            BinaryToken token,
            Expression<Func<T, object>> parentLeft,
            Expression<Func<T, object>> parentRight,
            params Expression<Func<T, object>>[] items)
        {
            var leftNode = this.ExtractNode(element, parentLeft);
            var rightNode = this.ExtractNode(element, parentRight);

            if (leftNode is IBinaryNode && leftNode is IElementNode<Expression> && TypeExt.IsNumericType(((IElementNode<Expression>)leftNode).Element.Type))
            {
                leftNode = leftNode.Then(Tokens.Brackets);
            }

            if (rightNode is IBinaryNode && rightNode is IElementNode<Expression> && TypeExt.IsNumericType(((IElementNode<Expression>)rightNode).Element.Type))
            {
                rightNode = rightNode.Then(Tokens.Brackets);
            }

            return NodeFactory.BinaryNode(element, token, parentLeft, leftNode, parentRight, rightNode);
        }

        private Node AddConstantNode(ConstantExpression expression)
        { 
            if (expression.Value == null)
            {
                return NodeFactory.ItemNode(expression, Tokens.Constant, x => x.Value, NodeFactory.Text("null"));
            }

            if (expression.Value is string)
            {
                return NodeFactory.ItemNode(expression, Tokens.Constant, x => x.Value, NodeFactory.Text((string)expression.Value).Then(Tokens.DoubleQuotes));
            }

            return this.CreateItemsNode(expression, Tokens.Constant, x => x.Value);
        }

        private Node AddMemberAccessNode(MemberExpression expression)
        {
            if (expression.Expression == null)
            {
                return NodeFactory.Node(expression, Tokens.Member, x => x.Expression, NodeFactory.Text(expression.Member.DeclaringType.Name), x => x.Member, NodeFactory.Text(expression.Member.Name));
            }

            return NodeFactory.Node(expression, Tokens.Member, x => x.Expression, this.AddNode(expression.Expression), x => x.Member, NodeFactory.Text(expression.Member.Name));
        }

        private Node AddSwitchaNode(SwitchExpression switchExpression)
        {
            return this.CreateItemsNode(switchExpression, Tokens.None, x => x.SwitchValue, x => x.Cases, x => x.DefaultBody);

            //@case =>
            //    Diagram.Node<SwitchCase>()
            //.AddItems(x => x.TestValues, @case.TestValues.Select(this.AddNode))
            //        .AddItem(x => x.Body, this.AddNode(@case.Body))))
        }

        private Node AddMethodCallNode(MethodCallExpression expression)
        {
            if (expression.Method.IsSpecialName && (expression.Method.Attributes & MethodAttributes.HideBySig) != 0)
            {
                return this.CreateBinaryNode(expression, Tokens.Index, x => x.Object, x => x.Arguments);
            }

            if (expression.Method.IsDefined(typeof(ExtensionAttribute), false))
            {
                return NodeFactory.Node(expression)
                    .AddParent(x => x.Object, this.AddNode(expression.Arguments.First()))
                    .AddItem(x => x.Method, expression.Method.Name)
                    .AddItems(x => x.Arguments, expression.Arguments.Skip(1).Select(this.AddNode));
            }

            if (expression.Object == null)
            {
                return NodeFactory.Node(expression)
                    .AddParent(x => x.Object, NodeFactory.Text(expression.Method.DeclaringType.Name))
                    .AddItem(x => x.Method, expression.Method.Name)
                    .AddItems(x => x.Arguments, expression.Arguments.Select(this.AddNode));
            }

            return NodeFactory.Node(expression)
                .AddParent(x => x.Object, this.AddNode(expression.Object))
                .AddItem(x => x.Method, expression.Method.Name)
                .AddItems(x => x.Arguments, expression.Arguments.Select(this.AddNode));
        }

        private TypedNode<NewExpression> AddNewNode(NewExpression expression)
        {
            var result = (ItemNode<NewExpression>)this.CreateItemsNode(expression, Tokens.None, x => x.Type);

            if (expression.Members != null)
            {
                return result.AddItems(x => x.Members, expression.Arguments.Select(this.AddNode));
            }

            if (expression.Arguments.Count > 0)
            {
                return result.AddItems(x => x.Arguments, expression.Arguments.Select(this.AddNode));
            }

            return result;
        }

        private TypedNode<NewExpression> AddMemberInitNode(MemberInitExpression memberInitExpression)
        {
            var newNode = this.AddNewNode(memberInitExpression.NewExpression);
            var bindings = memberInitExpression.Bindings.Select(this.AddBindingNode);

            return newNode.AddItems(x => x.Members, bindings);
        }

        private Node AddBindingNode(MemberBinding memberBinding)
        {
            switch (memberBinding.BindingType)
            {
                case MemberBindingType.Assignment:
                    return this.AddMemberAssignmentNode((MemberAssignment)memberBinding);
                case MemberBindingType.MemberBinding:
                    return this.AddMemberMemberBindingNode((MemberMemberBinding)memberBinding);
                case MemberBindingType.ListBinding:
                    return this.AddMemberListBindingNode((MemberListBinding)memberBinding);
                default:
                    throw new InvalidOperationException("Unhandled Binding Type: " + memberBinding.BindingType);
            }
        }

        protected virtual Node AddMemberAssignmentNode(MemberAssignment assignment)
        {
            return this.AddNode(assignment.Expression).Then(assignment, Tokens.Bind, x => x.Expression, x => x.Member, assignment.Member.Name);
        }

        protected virtual Node AddMemberMemberBindingNode(MemberMemberBinding memberMemberBinding)
        {
            //var bindings = memberMemberBinding.Bindings.Select(this.AddBindingNode);

            //return Diagram.Node(Token.Bind, memberMemberBinding.Member.Name).AddParents(bindings);

            throw new NotImplementedException();
        }

        protected virtual Node AddMemberListBindingNode(MemberListBinding memberListBinding)
        {
            throw new NotImplementedException();
        }

        private static UnaryToken GetUnaryOperator(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                    return Tokens.Negate;

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
                    return Tokens.As;

                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    return Tokens.Convert;

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
                    return Tokens.Add;
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return Tokens.Subtract;
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    return Tokens.Multiply;
                case ExpressionType.Divide:
                    return Tokens.Divide;
                case ExpressionType.Modulo:
                    return Tokens.Modulo;
                case ExpressionType.Power:
                    return Tokens.Power;
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    return Tokens.And;
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return Tokens.Or;
                case ExpressionType.LessThan:
                    return Tokens.LessThan;
                case ExpressionType.LessThanOrEqual:
                    return Tokens.LessThanOrEqual;
                case ExpressionType.GreaterThan:
                    return Tokens.GreaterThan;
                case ExpressionType.GreaterThanOrEqual:
                    return Tokens.GreaterThanOrEqual;
                case ExpressionType.Equal:
                    return Tokens.Equal;
                case ExpressionType.NotEqual:
                    return Tokens.NotEqual;
                case ExpressionType.Coalesce:
                    return Tokens.Coalesce;
                //case ExpressionType.RightShift:
                //    return ">>";
                //case ExpressionType.LeftShift:
                //    return "<<";
                //case ExpressionType.ExclusiveOr:
                //    return "^";
                case ExpressionType.ArrayIndex:
                    return Tokens.Index;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
