using System;
using System.Collections.Generic;
using System.Linq;
using Maze.Nodes;

namespace Maze
{
    public abstract class NodeVisitor<TResult>
    {
        public virtual TResult VisitNode(Node node)
        {
            switch (node.Kind)
            {
                case NodeKind.Empty:
                    return this.VisitEmptyNode();

                case NodeKind.Text:
                    return this.VisitTextNode((TextNode)node);

                case NodeKind.MultiItem:
                    return this.VisitMultiItemNode((MultiItemNode)node, null);

                case NodeKind.Token:
                case NodeKind.ElementToken:
                    return this.VisitTokenNode(((ITokenNode)node).Token, node);

                default:
                    throw new NotImplementedException();
            }
        }

        public virtual TResult VisitSyntax(Syntax syntax, Node node)
        {
            switch (syntax.Kind)
            {
                case SyntaxKind.Text:
                    return this.VisitTextSyntax((TextSyntax)syntax);

                case SyntaxKind.Node:
                case SyntaxKind.NodeContainer:
                    var item = ((ITokenNode)node)[((NodeSyntax)syntax).Connection];

                    if (item == null)
                    {
                        throw new InvalidOperationException();
                    }

                    if (syntax.Kind == SyntaxKind.NodeContainer && item.Kind == NodeKind.MultiItem)
                    {
                        var separetor = ((NodeContainerSyntax)syntax).Separetor;

                        return this.VisitMultiItemNode((MultiItemNode)item, separetor);
                    }

                    return this.VisitNode(item);

                default:
                    throw new InvalidOperationException();
            }
        }

        protected abstract TResult VisitEmptyNode();

        protected abstract TResult VisitTextNode(TextNode node);

        protected abstract TResult VisitMultiItemNode(MultiItemNode node, TextSyntax separetor);

        protected abstract TResult VisitTokenNode(Token token, Node node);

        protected abstract TResult VisitEmptySyntax();

        protected abstract TResult VisitTextSyntax(TextSyntax syntax);
    }

    public class TextSyntaxNodeVisitor : NodeVisitor<IEnumerable<TextSyntax>>
    {
        protected override IEnumerable<TextSyntax> VisitEmptyNode()
        {
            return Enumerable.Empty<TextSyntax>();
        }

        protected override IEnumerable<TextSyntax> VisitTextNode(TextNode node)
        {
            yield return SyntaxFactory.FromText(node.Value);
        }

        protected override IEnumerable<TextSyntax> VisitMultiItemNode(MultiItemNode node, TextSyntax separetor)
        {
            if (separetor == null)
            {
                return node.SelectMany(this.VisitNode);
            }

            return node.SelectMany((x, i) => i == 0 ? this.VisitNode(x) : Linq.Enumerable.Return(separetor).Concat(this.VisitNode(x)));
        }

        protected override IEnumerable<TextSyntax> VisitTokenNode(Token token, Node node)
        {
            return token.GetSyntax().SelectMany(x => this.VisitSyntax(x, node));
        }

        protected override IEnumerable<TextSyntax> VisitEmptySyntax()
        {
            return Enumerable.Empty<TextSyntax>();
        }

        protected override IEnumerable<TextSyntax> VisitTextSyntax(TextSyntax syntax)
        {
            return Linq.Enumerable.Return(syntax);
        }
    }
}
