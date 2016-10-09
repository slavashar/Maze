using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Text;
using Maze.Linq;

namespace Maze.Nodes
{
    public enum SyntaxKind
    {
        Text,
        Node,
        NodeContainer
    }

    public class SyntaxStyle
    {
        public SyntaxStyle(bool leftMargin, bool rightMargin)
        {
            this.LeftMargin = leftMargin;
            this.RightMargin = rightMargin;
        }

        public bool LeftMargin { get; }

        public bool RightMargin { get; }

        public static SyntaxStyle GetStyle(bool leftMargin = false, bool rightMargin = false)
        {
            return new SyntaxStyle(leftMargin, rightMargin);
        }
    }

    public abstract class Syntax
    {
        public Syntax(SyntaxStyle style)
        {
            this.Style = style;
        }

        public abstract SyntaxKind Kind { get; }

        public SyntaxStyle Style { get; }
    }

    public sealed class TextSyntax : Syntax
    {
        private TextSyntax(string value, SyntaxStyle style)
            : base(style)
        {
            this.Value = value;
        }

        public override SyntaxKind Kind => SyntaxKind.Text;

        public string Value { get; }

        public static implicit operator TextSyntax(string text)
        {
            return SyntaxFactory.FromText(text);
        }

        internal static TextSyntax Create(string txt, SyntaxStyle style)
        {
            return new TextSyntax(txt, style);
        }
    }

    public class NodeSyntax : Syntax
    {
        internal NodeSyntax(TokenConnection connection, SyntaxStyle style)
            : base(style)
        {
            this.Connection = connection;
        }

        public override SyntaxKind Kind => SyntaxKind.Node;

        public TokenConnection Connection { get; }
    }

    public sealed class NodeContainerSyntax : NodeSyntax
    {
        internal NodeContainerSyntax(TokenConnection connection, TextSyntax separetor, SyntaxStyle style)
            : base(connection, style)
        {
            this.Separetor = separetor;
        }

        public override SyntaxKind Kind => SyntaxKind.NodeContainer;

        public TextSyntax Separetor { get; }
    }

    public static class SyntaxFactory
    {
        public static TextSyntax FromText(string txt, bool margin)
        {
            return TextSyntax.Create(txt, SyntaxStyle.GetStyle(margin, margin));
        }

        public static TextSyntax FromText(string txt, bool leftMargin = false, bool rightMargin = false)
        {
            return TextSyntax.Create(txt, SyntaxStyle.GetStyle(leftMargin, rightMargin));
        }

        public static ImmutableList<TextSyntax> AddFromText(this TextSyntax syntax, string txt, bool leftMargin = false, bool rightMargin = false)
        {
            return ImmutableList.Create(syntax, FromText(txt));
        }

        public static ImmutableList<TextSyntax> AddFromText(this ImmutableList<TextSyntax> syntax, string txt, bool leftMargin = false, bool rightMargin = false)
        {
            return syntax.Add(FromText(txt));
        }

        public static string Print(this TextSyntax syntax)
        {
            return Enumerable.Return(syntax).Print();
        }

        public static string Print(this IEnumerable<TextSyntax> syntax)
        {
            var builder = new StringBuilder();

            foreach (var item in syntax)
            {
                if (item.Style.LeftMargin)
                {
                    builder.Append(" ");
                }

                builder.Append(item.Value);

                if (item.Style.RightMargin)
                {
                    builder.Append(" ");
                }
            }

            return builder.ToString();
        }
    }
}
