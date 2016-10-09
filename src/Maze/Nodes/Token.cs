using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Maze.Nodes
{
    public interface IElementToken<out TElement>
    {
        TokenConnection Get(MemberInfo member);
    }

    public abstract class Token
    {
        public Token(params TokenConnection[] connections)
        {
            this.Connections = connections.ToImmutableArray();
        }

        public ImmutableArray<TokenConnection> Connections { get; }

        public abstract IEnumerable<Syntax> GetSyntax();

        protected static TextSyntax FromText(string txt, bool leftMargin = false, bool rightMargin = false)
        {
            return TextSyntax.Create(txt, SyntaxStyle.GetStyle(leftMargin, rightMargin));
        }

        protected static Syntax FromNode(TokenConnection connection, bool leftMargin = false, bool rightMargin = false)
        {
            return new NodeSyntax(connection, SyntaxStyle.GetStyle(leftMargin, rightMargin));
        }

        protected static Syntax FromNodeCollection(TokenConnection connection, TextSyntax separetor, bool leftMargin = false, bool rightMargin = false)
        {
            return new NodeContainerSyntax(connection, separetor, SyntaxStyle.GetStyle(leftMargin, rightMargin));
        }
    }

    public class TokenConnection
    {
        public TokenConnection(bool parent)
        {
            this.Parent = parent;
        }

        public bool Parent { get; }

        public static TokenConnection New(bool parent = false)
        {
            return new TokenConnection(parent);
        }

        public static TokenConnection<T> New<T>(Expression<Func<T, object>> selector, bool parent = false)
        {
            return new TokenConnection<T>(parent, ((MemberExpression)selector.Body).Member);
        }
    }

    public class TokenConnection<TElement> : TokenConnection
    {
        public TokenConnection(bool parent, MemberInfo member)
            : base(parent)
        {
            this.Member = member;
        }

        public MemberInfo Member { get; }
    }

    public abstract class TypedToken<TElement> : Token, IElementToken<TElement>
    {
        public TypedToken(params TokenConnection[] connections)
            : base(connections)
        {
        }

        public TokenConnection Get(MemberInfo member)
        {
            return this.Connections.OfType<TokenConnection<TElement>>().SingleOrDefault(x => x.Member == member);
        }
    }

    public class UnaryToken : Token
    {
        private readonly TextSyntax leader;
        private readonly TextSyntax trailer;

        public UnaryToken()
            : base(Parent)
        {
        }

        public UnaryToken(TextSyntax leader, TextSyntax trailer)
            : this()
        {
            this.leader = leader;
            this.trailer = trailer;
        }

        public static TokenConnection Parent { get; } = TokenConnection.New(true);

        public override IEnumerable<Syntax> GetSyntax()
        {
            if (this.leader != null)
            {
                yield return this.leader;
            }

            yield return FromNode(Parent);

            if (this.trailer != null)
            {
                yield return this.trailer;
            }
        }
    }

    public class UnaryToken<TElement> : UnaryToken, IElementToken<TElement>
    {
        private readonly MemberInfo parent;

        public UnaryToken(Expression<Func<TElement, object>> parentSelector, TextSyntax leader = null, TextSyntax trailer = null)
            : base(leader, trailer)
        {
            this.parent = ((MemberExpression)parentSelector.Body).Member;
        }

        public TokenConnection Get(MemberInfo member)
        {
            if (member == this.parent)
            {
                return Parent;
            }

            return null;
        }
    }

    public class ItemToken : Token
    {
        private readonly TextSyntax leader;
        private readonly TextSyntax trailer;

        public ItemToken()
            : base(Item)
        {
        }

        public ItemToken(TextSyntax leader, TextSyntax trailer)
            : this()
        {
            this.leader = leader;
            this.trailer = trailer;
        }

        public static TokenConnection Item { get; } = TokenConnection.New();

        public override IEnumerable<Syntax> GetSyntax()
        {
            if (this.leader != null)
            {
                yield return this.leader;
            }

            yield return FromNode(Item);

            if (this.trailer != null)
            {
                yield return this.trailer;
            }
        }
    }

    public class ItemToken<TElement> : ItemToken, IElementToken<TElement>
    {
        private readonly MemberInfo item;

        public ItemToken(Expression<Func<TElement, object>> itemSelector, TextSyntax leader = null, TextSyntax trailer = null)
            : base(leader, trailer)
        {
            this.item = ((MemberExpression)itemSelector.Body).Member;
        }

        public TokenConnection Get(MemberInfo member)
        {
            if (member == this.item)
            {
                return Item;
            }

            return null;
        }
    }

    public class UnaryItemToken : Token
    {
        private readonly TextSyntax leader;
        private readonly TextSyntax delimiter;
        private readonly TextSyntax trailer;

        public UnaryItemToken()
            : base(Parent, Item)
        {
        }

        public UnaryItemToken(TextSyntax leader, TextSyntax delimiter, TextSyntax trailer)
            : this()
        {
            this.leader = leader;
            this.delimiter = delimiter;
            this.trailer = trailer;
        }

        public static TokenConnection Parent { get; } = TokenConnection.New(true);

        public static TokenConnection Item { get; } = TokenConnection.New();

        public override IEnumerable<Syntax> GetSyntax()
        {
            if (this.leader != null)
            {
                yield return this.leader;
            }

            yield return FromNode(Parent);

            if (this.delimiter != null)
            {
                yield return this.delimiter;
            }

            yield return FromNode(Item);

            if (this.trailer != null)
            {
                yield return this.trailer;
            }
        }
    }

    public class UnaryItemToken<TElement> : UnaryItemToken, IElementToken<TElement>
    {
        private readonly MemberInfo parent;
        private readonly MemberInfo item;

        public UnaryItemToken(Expression<Func<TElement, object>> parentSelector, Expression<Func<TElement, object>> itemSelector, TextSyntax leader = null, TextSyntax delimiter = null, TextSyntax trailer = null)
            : base(leader, delimiter, trailer)
        {
            this.parent = ((MemberExpression)parentSelector.Body).Member;
            this.item = ((MemberExpression)itemSelector.Body).Member;
        }

        public TokenConnection Get(MemberInfo member)
        {
            if (member == this.parent)
            {
                return Parent;
            }

            if (member == this.item)
            {
                return Item;
            }

            return null;
        }
    }

    public class BinaryToken : Token
    {
        private readonly TextSyntax leader;
        private readonly TextSyntax delimiter;
        private readonly TextSyntax trailer;

        public BinaryToken()
           : base(Left, Right)
        {
        }

        public BinaryToken(TextSyntax leader, TextSyntax delimiter, TextSyntax trailer)
            : this()
        {
            this.leader = leader;
            this.delimiter = delimiter;
            this.trailer = trailer;
        }

        public static TokenConnection Left { get; } = TokenConnection.New(true);

        public static TokenConnection Right { get; } = TokenConnection.New(true);

        public override IEnumerable<Syntax> GetSyntax()
        {
            if (this.leader != null)
            {
                yield return this.leader;
            }

            yield return FromNode(Left);

            if (this.delimiter != null)
            {
                yield return this.delimiter;
            }

            yield return FromNode(Right);

            if (this.trailer != null)
            {
                yield return this.trailer;
            }
        }
    }

    public class BinaryToken<TElement> : BinaryToken, IElementToken<TElement>
    {
        private readonly MemberInfo left;
        private readonly MemberInfo right;

        public BinaryToken(Expression<Func<TElement, object>> leftSelector, Expression<Func<TElement, object>> rightSelector, TextSyntax leader = null, TextSyntax delimiter = null, TextSyntax trailer = null)
            : base(leader, delimiter, trailer)
        {
            this.left = ((MemberExpression)leftSelector.Body).Member;
            this.right = ((MemberExpression)rightSelector.Body).Member;
        }

        public TokenConnection Get(MemberInfo member)
        {
            if (member == this.left)
            {
                return Left;
            }

            if (member == this.right)
            {
                return Right;
            }

            return null;
        }
    }
}
