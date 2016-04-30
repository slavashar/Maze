namespace Maze.Nodes
{
    public static class Tokens
    {
        public static NoneToken None { get; } = new NoneToken();

        public static BinaryToken Add { get; } = new OperatorToken("+");

        public static BinaryToken Subtract { get; } = new OperatorToken("-");

        public static BinaryToken Multiply { get; } = new OperatorToken("*");

        public static BinaryToken Divide { get; } = new OperatorToken("/");

        public static BinaryToken Modulo { get; } = new OperatorToken("%");

        public static BinaryToken Power { get; } = new OperatorToken("*");

        public static BinaryToken NotEqual { get; } = new OperatorToken("!=");

        public static BinaryToken Coalesce { get; } = new OperatorToken("??");

        public static BinaryToken Equal { get; } = new OperatorToken("==");

        public static BinaryToken And { get; } = new OperatorToken("and");

        public static BinaryToken Or { get; } = new OperatorToken("or");

        public static BinaryToken LessThan { get; } = new OperatorToken("<");

        public static BinaryToken LessThanOrEqual { get; } = new OperatorToken("<=");

        public static BinaryToken GreaterThan { get; } = new OperatorToken(">");

        public static BinaryToken GreaterThanOrEqual { get; } = new OperatorToken(">=");

        public static BinaryToken Index { get; } = new ArrayIndexToken();

        public static UnaryToken Negate { get; } = new NegateToken();

        public static UnaryItemToken Member { get; } = new MemberToken();

        public static UnaryToken Brackets { get; } = new BracketsToken('(', ')');

        public static UnaryToken SquareBrackets { get; } = new BracketsToken('[', ']');

        public static UnaryToken SingleQuotes { get; } = new BracketsToken('\'', '\'');

        public static UnaryToken DoubleQuotes { get; } = new BracketsToken('"', '"');

        public static ItemToken Parameter { get; } = new ParameterToken();

        public static ItemToken Constant { get; } = new ConstantToken();

        public static UnaryItemToken Bind { get; } = new BindToken();

        public static UnaryItemToken As { get; } = new AsToken();

        public static UnaryItemToken Convert { get; } = new ConvertToken();
        
        private sealed class NegateToken : UnaryToken
        {
            public override string Format
            {
                get { return "-{0}"; }
            }
        }

        private sealed class MemberToken : UnaryItemToken
        {
            public override string Format
            {
                get { return "{0}.{1}"; }
            }
        }

        private sealed class BracketsToken : UnaryToken
        {
            private readonly char open, close;

            public BracketsToken(char open, char close)
            {
                this.open = open;
                this.close = close;
            }

            public override string Format
            {
                get { return string.Concat(this.open, "{0}", this.close); }
            }
        }

        private sealed class AsToken : UnaryItemToken
        {
            public override string Format
            {
                get { return "({0} AS {1})"; }
            }
        }

        private sealed class ConvertToken : UnaryItemToken
        {
            public override string Format
            {
                get { return "({0} AS {1})"; }
            }
        }

        private sealed class ParameterToken : ItemToken
        {
            public override string Format
            {
                get { return "{0}"; }
            }
        }

        private sealed class ConstantToken : ItemToken
        {
            public override string Format
            {
                get { return "{0}"; }
            }
        }

        private sealed class BindToken : UnaryItemToken
        {
            public override string Format
            {
                get { return "{1} = {0}"; }
            }
        }

        private sealed class OperatorToken : BinaryToken
        {
            private readonly string format;

            public OperatorToken(string simbol)
            {
                this.format = "{0} " + simbol + " {1}";
            }

            public override string Format
            {
                get { return this.format; }
            }
        }

        private sealed class ArrayIndexToken : BinaryToken
        {
            public override string Format
            {
                get { return "{0}[{1}]"; }
            }
        }
    }
}
