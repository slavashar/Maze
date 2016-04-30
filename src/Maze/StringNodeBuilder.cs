using System;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using Maze.Nodes;

namespace Maze
{
    public static class MappingNodeStringifyExtension
    {
        private static readonly Lazy<StringNodeBuilder> builder = new Lazy<StringNodeBuilder>();

        public static string Stringify(this Node node)
        {
            return builder.Value.CreateElement(node);
        }
    }

    public class StringNodeBuilder
    {
        private ImmutableDictionary<Token, string> custom;

        public StringNodeBuilder()
        {
            this.custom = ImmutableDictionary<Token, string>.Empty;
        }

        public StringNodeBuilder(ImmutableDictionary<Token, string> custom)
        {
            this.custom = custom;
        }

        public string CreateElement(Node node)
        {
            switch (node.Kind)
            {
                case NodeKind.Empty:
                    return string.Empty;

                case NodeKind.Text:
                    return CreateText((TextNode)node);

                case NodeKind.Unary:
                    return CreateUnary((IUnaryNode)node);

                case NodeKind.Item:
                    return CreateSingleItem((IItemNode)node);

                case NodeKind.UnaryItem:
                    return CreateUnarySingleItem((IUnaryItemNode)node);

                case NodeKind.Binary:
                    return CreateBinary((IBinaryNode)node);

                case NodeKind.Complex:
                    return CreateComplex((IComplexNode)node);

                default:
                    throw new NotImplementedException();
            }
        }

        private string CreateText(TextNode node)
        {
            return node.Value;
        }

        private string CreateUnary(IUnaryNode node)
        {
            return string.Format(this.custom.GetValueOrDefault(node.Token, node.Token.Format), this.CreateElement(node.Parent));
        }

        private string CreateSingleItem(IItemNode node)
        {
            return string.Format(this.custom.GetValueOrDefault(node.Token, node.Token.Format), this.CreateElement(node.Item));
        }

        private string CreateUnarySingleItem(IUnaryItemNode node)
        {
            return string.Format(this.custom.GetValueOrDefault(node.Token, node.Token.Format), this.CreateElement(node.Parent), this.CreateElement(node.Item));
        }

        private string CreateBinary(IBinaryNode node)
        {
            return string.Format(this.custom.GetValueOrDefault(node.Token, node.Token.Format), this.CreateElement(node.Left), this.CreateElement(node.Right));
        }

        private string CreateComplex(IComplexNode node)
        {
            if (node is ComplexNode<ConditionalExpression>)
            {
                return this.CreateConditional((ComplexNode<ConditionalExpression>)node);
            }

            if (node is ComplexNode<MethodCallExpression>)
            {
                return this.CreateCall((ComplexNode<MethodCallExpression>)node);
            }

            if (node is ComplexNode<NewExpression>)
            {
                return this.CreateNew((ComplexNode<NewExpression>)node);
            }

            return string.Join(", ", node);
        }

        private string CreateConditional(ComplexNode<ConditionalExpression> node)
        {
            return string.Format(
                "If {0} Then {1} Else {2}",
                this.CreateElement(node.Get(x => x.Test)),
                this.CreateElement(node.Get(x => x.IfTrue)),
                this.CreateElement(node.Get(x => x.IfFalse)));
        }

        private string CreateCall(ComplexNode<MethodCallExpression> node)
        {
            return string.Format(
                "{0}.{1}({2})",
                this.CreateElement(node.Get(x => x.Object)),
                this.CreateElement(node.Get(x => x.Method)),
                string.Join(", ", node.GetMany(x => x.Arguments).Select(this.CreateElement)));
        }

        private string CreateNew(ComplexNode<NewExpression> node)
        {
            return string.Format(
                "New {0}: {1}",
                this.CreateElement(node.Get(x => x.Type)),
                string.Join(", ", node.GetMany(x => x.Members).Select(this.CreateElement)));
        }
    }
}
