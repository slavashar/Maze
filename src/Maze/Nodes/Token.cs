namespace Maze.Nodes
{
    public abstract class Token
    {
    }

    public abstract class UnaryToken : Token
    {
        public abstract string Format { get; }
    }

    public abstract class ItemToken : Token
    {
        public abstract string Format { get; }
    }

    public abstract class UnaryItemToken : Token
    {
        public abstract string Format { get; }
    }

    public abstract class BinaryToken : Token
    {
        public abstract string Format { get; }
    }

    public sealed class NoneToken : Token
    {
        private UnaryNoneToken unary = new UnaryNoneToken();
        private ItemNoneToken item = new ItemNoneToken();
        private UnaryItemNoneToken unaryitem = new UnaryItemNoneToken();

        public static implicit operator UnaryToken(NoneToken token)
        {
            return token.unary;
        }

        public static implicit operator ItemToken(NoneToken token)
        {
            return token.item;
        }

        public static implicit operator UnaryItemToken(NoneToken token)
        {
            return token.unaryitem;
        }

        private class UnaryNoneToken : UnaryToken
        {
            public override string Format
            {
                get { return string.Empty; }
            }
        }

        private class ItemNoneToken : ItemToken
        {
            public override string Format
            {
                get { return string.Empty; }
            }
        }

        private class UnaryItemNoneToken : UnaryItemToken
        {
            public override string Format
            {
                get { return string.Empty; }
            }
        }
    }
}
