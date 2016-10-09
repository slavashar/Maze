using System.Collections.Generic;
using System.Linq.Expressions;

namespace Maze.Nodes
{
    public static class ExpressionTokens
    {
        public static BinaryToken Add { get; } = CreateBinaryTokenFromDelimiter("+", true);

        public static BinaryToken Subtract { get; } = CreateBinaryTokenFromDelimiter("-", true);

        public static BinaryToken Multiply { get; } = CreateBinaryTokenFromDelimiter("*", true);

        public static BinaryToken Divide { get; } = CreateBinaryTokenFromDelimiter("/", true);

        public static BinaryToken Modulo { get; } = CreateBinaryTokenFromDelimiter("%", true);

        public static BinaryToken Power { get; } = CreateBinaryTokenFromDelimiter("*", true);

        public static BinaryToken NotEqual { get; } = CreateBinaryTokenFromDelimiter("!=", true);

        public static BinaryToken Coalesce { get; } = CreateBinaryTokenFromDelimiter("??", true);

        public static BinaryToken Equal { get; } = CreateBinaryTokenFromDelimiter("==", true);

        public static BinaryToken And { get; } = CreateBinaryTokenFromDelimiter("and", true);

        public static BinaryToken Or { get; } = CreateBinaryTokenFromDelimiter("or", true);

        public static BinaryToken LessThan { get; } = CreateBinaryTokenFromDelimiter("<", true);

        public static BinaryToken LessThanOrEqual { get; } = CreateBinaryTokenFromDelimiter("<=", true);

        public static BinaryToken GreaterThan { get; } = CreateBinaryTokenFromDelimiter(">", true);

        public static BinaryToken GreaterThanOrEqual { get; } = CreateBinaryTokenFromDelimiter(">=", true);

        public static UnaryItemToken Index { get; } = new UnaryItemToken<BinaryExpression>(x => x.Left, x => x.Right, null, SyntaxFactory.FromText("["), SyntaxFactory.FromText("]"));

        public static UnaryToken Negate { get; } = new UnaryToken<UnaryExpression>(x => x.Operand, SyntaxFactory.FromText("-"), null);

        public static UnaryItemToken Member { get; } = new UnaryItemToken<MemberExpression>(x => x.Expression, x => x.Member, null, SyntaxFactory.FromText("."), null);

        public static UnaryToken Brackets { get; } = CreateUnaryToken("(", ")");

        public static UnaryToken SquareBrackets { get; } = CreateUnaryToken("[", "]");

        public static UnaryToken SingleQuotes { get; } = CreateUnaryToken("\'", "\'");

        public static UnaryToken DoubleQuotes { get; } = CreateUnaryToken("\"", "\"");

        public static ItemToken Parameter { get; } = new ItemToken<ParameterExpression>(x => x.Name);

        public static ItemToken Constant { get; } = new ItemToken<ConstantExpression>(x => x.Value);

        public static UnaryItemToken Bind { get; } = new BindExpressionToken();

        public static UnaryItemToken As { get; } = new UnaryItemToken<UnaryExpression>(x => x.Operand, x => x.Type, SyntaxFactory.FromText("("), SyntaxFactory.FromText("as", true), SyntaxFactory.FromText(")"));

        public static UnaryItemToken Is { get; } = new UnaryItemToken<TypeBinaryExpression>(x => x.Expression, x => x.Type, SyntaxFactory.FromText("("), SyntaxFactory.FromText("is", true), SyntaxFactory.FromText(")"));

        public static UnaryItemToken Convert { get; } = new UnaryItemToken<UnaryExpression>(x => x.Operand, x => x.Type, SyntaxFactory.FromText("("), SyntaxFactory.FromText("as", true), SyntaxFactory.FromText(")"));

        public static ConditionalExpressionToken Conditional { get; } = new ConditionalExpressionToken();

        public static SwitchExpressionToken Switch { get; } = new SwitchExpressionToken();

        public static SwitchCaseExpressionToken SwitchCase { get; } = new SwitchCaseExpressionToken();

        public static MethodCallExpressionToken MethodCall { get; } = new MethodCallExpressionToken();

        public static JoinExpressionToken Join { get; } = new JoinExpressionToken();

        public static NewExpressionToken New { get; } = new NewExpressionToken();

        public static NewArrayExpressionToken NewArray { get; } = new NewArrayExpressionToken();

        public static UnaryItemToken CreateUnaryItemTokenFromDelimiter(string delimiter)
        {
            return CreateUnaryItemTokenFromDelimiter(delimiter, false);
        }

        public static UnaryItemToken CreateUnaryItemTokenFromDelimiter(string delimiter, bool margin)
        {
            return new UnaryItemToken(null, SyntaxFactory.FromText(delimiter, margin), null);
        }

        public static ItemToken CreateItemToken(string leader = null, string trailer = null)
        {
            return new ItemToken(leader != null ? SyntaxFactory.FromText(leader) : null, trailer != null ? SyntaxFactory.FromText(trailer) : null);
        }

        public static UnaryToken CreateUnaryToken(string leader = null, string trailer = null)
        {
            return new UnaryToken(leader != null ? SyntaxFactory.FromText(leader) : null, trailer != null ? SyntaxFactory.FromText(trailer) : null);
        }

        public static BinaryToken CreateBinaryToken(string delimiter)
        {
            return new BinaryToken<BinaryExpression>(x => x.Left, x => x.Right, null, SyntaxFactory.FromText(delimiter, true, true), null);
        }

        public static BinaryToken CreateBinaryTokenFromDelimiter(string delimiter, bool margin)
        {
            if (margin)
            {
                return new BinaryToken<BinaryExpression>(x => x.Left, x => x.Right, null, SyntaxFactory.FromText(delimiter, true, true), null);
            }

            return new BinaryToken<BinaryExpression>(x => x.Left, x => x.Right, null, SyntaxFactory.FromText(delimiter), null);
        }
    }

    public sealed class ConditionalExpressionToken : TypedToken<ConditionalExpression>
    {
        public ConditionalExpressionToken()
            : base(Test, IfTrue, IfFalse)
        {
        }

        public static TokenConnection Test { get; } = TokenConnection.New<ConditionalExpression>(x => x.Test);

        public static TokenConnection IfTrue { get; } = TokenConnection.New<ConditionalExpression>(x => x.IfTrue);

        public static TokenConnection IfFalse { get; } = TokenConnection.New<ConditionalExpression>(x => x.IfFalse);

        public override IEnumerable<Syntax> GetSyntax()
        {
            yield return FromText("if", rightMargin: true);
            yield return FromNode(Test);
            yield return FromText("then", leftMargin: true, rightMargin: true);
            yield return FromNode(IfTrue);
            yield return FromText("else", leftMargin: true, rightMargin: true);
            yield return FromNode(IfFalse);
        }
    }

    public sealed class SwitchExpressionToken : TypedToken<SwitchExpression>
    {
        public SwitchExpressionToken()
            : base(SwitchValue, Cases, DefaultBody)
        {
        }

        public static TokenConnection SwitchValue { get; } = TokenConnection.New<SwitchExpression>(x => x.SwitchValue);

        public static TokenConnection Cases { get; } = TokenConnection.New<SwitchExpression>(x => x.Cases);

        public static TokenConnection DefaultBody { get; } = TokenConnection.New<SwitchExpression>(x => x.DefaultBody);

        public override IEnumerable<Syntax> GetSyntax()
        {
            yield return FromText("case", rightMargin: true);
            yield return FromNode(SwitchValue, rightMargin: true);
            yield return FromNodeCollection(Cases, FromText(string.Empty, rightMargin: true), rightMargin: true);
            yield return FromText("else", rightMargin: true);
            yield return FromNode(DefaultBody, rightMargin: true);
        }
    }

    public sealed class SwitchCaseExpressionToken : TypedToken<SwitchCase>
    {
        public SwitchCaseExpressionToken()
            : base(TestValues, Body)
        {
        }

        public static TokenConnection TestValues { get; } = TokenConnection.New<SwitchCase>(x => x.TestValues);

        public static TokenConnection Body { get; } = TokenConnection.New<SwitchCase>(x => x.Body);

        public override IEnumerable<Syntax> GetSyntax()
        {
            yield return FromText("when", rightMargin: true);
            yield return FromNodeCollection(TestValues, FromText(",", rightMargin: true), rightMargin: true);
            yield return FromText("then", rightMargin: true);
            yield return FromNode(Body);
        }
    }

    public class MethodCallExpressionToken : TypedToken<MethodCallExpression>
    {
        public MethodCallExpressionToken()
            : base(Object, Name, Arguments)
        {
        }

        public static TokenConnection Object { get; } = TokenConnection.New<MethodCallExpression>(x => x.Object, true);

        public static TokenConnection Name { get; } = TokenConnection.New<MethodCallExpression>(x => x.Method);

        public static TokenConnection Arguments { get; } = TokenConnection.New<MethodCallExpression>(x => x.Arguments);

        public override IEnumerable<Syntax> GetSyntax()
        {
            yield return FromNode(Object);
            yield return FromText(".");
            yield return FromNode(Name);
            yield return FromText("(");
            yield return FromNodeCollection(Arguments, FromText(",", rightMargin: true));
            yield return FromText(")");
        }
    }

    public class JoinExpressionToken : TypedToken<MethodCallExpression>
    {
        public JoinExpressionToken()
            : base(Name, Outer, Inner, Key)
        {
        }

        public static TokenConnection Name { get; } = TokenConnection.New<MethodCallExpression>(x => x.Method);

        public static TokenConnection Outer { get; } = TokenConnection.New<MethodCallExpression>(x => x.Object, true);

        public static TokenConnection Inner { get; } = TokenConnection.New(true);

        public static TokenConnection Key { get; } = TokenConnection.New<MethodCallExpression>(x => x.Arguments);

        public override IEnumerable<Syntax> GetSyntax()
        {
            yield return FromNode(Outer);
            yield return FromText("join", true, true);
            yield return FromNode(Inner);
            yield return FromText("on", true, true);
            yield return FromNode(Key);
        }
    }

    public sealed class NewExpressionToken : TypedToken<NewExpression>
    {
        public NewExpressionToken()
            : base(Type, Arguments, Members)
        {
        }

        public static TokenConnection Type { get; } = TokenConnection.New<NewExpression>(x => x.Type);

        public static TokenConnection Arguments { get; } = TokenConnection.New<NewExpression>(x => x.Arguments);

        public static TokenConnection Members { get; } = TokenConnection.New<NewExpression>(x => x.Members);

        public override IEnumerable<Syntax> GetSyntax()
        {
            yield return FromText("New", rightMargin: true);
            yield return FromNode(Type);
            yield return FromNodeCollection(Arguments, FromText(",", rightMargin: true), true, true);
            yield return FromText(":", rightMargin: true);
            yield return FromNodeCollection(Members, FromText(",", rightMargin: true));
        }
    }

    public class BindExpressionToken : UnaryItemToken<MemberBinding>
    {
        public BindExpressionToken()
            : base(x => x.Member, x => x.Member)
        {
        }

        public override IEnumerable<Syntax> GetSyntax()
        {
            yield return FromNode(Item);
            yield return SyntaxFactory.FromText("=", true);
            yield return FromNode(Parent);
        }
    }

    public sealed class NewArrayExpressionToken : TypedToken<NewArrayExpression>
    {
        public NewArrayExpressionToken()
            : base(Expressions)
        {
        }

        public static TokenConnection Expressions { get; } = TokenConnection.New<NewArrayExpression>(x => x.Expressions);

        public override IEnumerable<Syntax> GetSyntax()
        {
            yield return FromText("[");
            yield return FromNodeCollection(Expressions, FromText(",", rightMargin: true));
            yield return FromText("]");
        }
    }
}
