using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Maze
{
    public sealed class ExpressionComparer : IComparer<Expression>, IEqualityComparer<Expression>
    {
        private static readonly Lazy<ExpressionComparer> DefaultExpressionComparer = new Lazy<ExpressionComparer>(() => new ExpressionComparer());

        private static readonly Lazy<TypeComparer> DefaultTypeComparer = new Lazy<TypeComparer>(() => new TypeComparer());

        private static readonly Lazy<MemberInfoComparer> DefaultMemberInfoComparer = new Lazy<MemberInfoComparer>(() => new MemberInfoComparer(DefaultTypeComparer.Value));

        private static readonly Lazy<IDictionary<ExpressionType, IImplComparer>> ConcreteExpressionComparers;

        private static readonly StringComparer DefaultStringComparer = StringComparer.InvariantCulture;

        private readonly ExpressionComparerImpl defaultImpl = new ExpressionComparerImpl();

        private readonly ConditionalWeakTable<Expression, Hash> hashCache = new ConditionalWeakTable<Expression, Hash>();

        static ExpressionComparer()
        {
            ConcreteExpressionComparers = new Lazy<IDictionary<ExpressionType, IImplComparer>>(() =>
                GetConcreteExpressionComparers()
                .SelectMany(comparer => comparer.Types.Select(type => new { type, comparer }))
                .ToDictionary(x => x.type, x => x.comparer));
        }

        private ExpressionComparer()
        {
        }

        private interface IImplComparer : IComparer<Expression>, IEqualityComparer<Expression>
        {
            IEnumerable<ExpressionType> Types { get; }
        }

        public static ExpressionComparer Default
        {
            get { return DefaultExpressionComparer.Value; }
        }

        public int Compare(Expression x, Expression y)
        {
            return this.defaultImpl.Compare(x, y);
        }

        public bool Equals(Expression x, Expression y)
        {
            return this.defaultImpl.Equals(x, y);
        }

        public int GetHashCode(Expression obj)
        {
            if (obj == null)
            {
                return 0;
            }

            return this.hashCache.GetValue(obj, expr => new Hash(this.defaultImpl.GetHashCode(expr))).Value;
        }

        private static IEnumerable<IImplComparer> GetConcreteExpressionComparers()
        {
            yield return new UnaryExpressionComparer();
            yield return new BinaryExpressionComparer();
            yield return new TypeBinaryExpressionComparer();
            yield return new ConditionalExpressionComparer();
            yield return new ConstantExpressionComparer();
            yield return new ParameterExpressionComparer();
            yield return new MemberExpressionComparer();
            yield return new MethodCallExpressionComparer();
            yield return new LambdaExpressionComparer();
            yield return new SwitchExpressionComparer();
            yield return new NewExpressionComparer();
            yield return new NewArrayExpressionComparer();
            yield return new InvocationExpressionComparer();
            yield return new MemberInitExpressionComparer();
            yield return new ListInitExpressionComparer();
        }

        private class Hash
        {
            public Hash(int value)
            {
                this.Value = value;
            }

            public int Value { get; private set; }
        }

        private abstract class BaseComparer<T> : IComparer<T>, IEqualityComparer<T>
            where T : class
        {
            public int Compare(T x, T y)
            {
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }

                if (x == null)
                {
                    return -1;
                }

                if (y == null)
                {
                    return 1;
                }

                return this.NotNullableCompare(x, y);
            }

            public bool Equals(T x, T y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                return this.NotNullableEquals(x, y);
            }

            public int GetHashCode(T obj)
            {
                if (obj == null)
                {
                    return 0;
                }

                return this.NotNullableGetHashCode(obj);
            }

            protected abstract int NotNullableCompare(T x, T y);

            protected virtual bool NotNullableEquals(T x, T y)
            {
                return x.Equals(y);
            }

            protected virtual int NotNullableGetHashCode(T obj)
            {
                return obj.GetHashCode();
            }
        }

        private abstract class GerericComparer<T> : BaseComparer<T>
            where T : class
        {
            private List<MemberDef> members = new List<MemberDef>();

            protected override int NotNullableCompare(T x, T y)
            {
                foreach (var item in this.members)
                {
                    var result = item.Compare(x, y);

                    if (result != 0)
                    {
                        return result;
                    }
                }

                return 0;
            }

            protected override bool NotNullableEquals(T x, T y)
            {
                foreach (var item in this.members)
                {
                    if (!item.Equals(x, y))
                    {
                        return false;
                    }
                }

                return true;
            }

            protected override int NotNullableGetHashCode(T obj)
            {
                var resilt = 0;

                foreach (var item in this.members)
                {
                    unchecked
                    {
                        resilt += item.GetHashCode(obj);
                    }
                }

                return resilt;
            }

            protected void AddMember<TResult>(Func<T, TResult> member)
            {
                this.members.Add(new MemberDef<TResult>(member, GetComparer<TResult>(), GetEqualityComparer<TResult>()));
            }

            protected void AddMember<TResult, TComparer>(Func<T, TResult> member, TComparer comparer)
                where TComparer : IComparer<TResult>, IEqualityComparer<TResult>
            {
                this.members.Add(new MemberDef<TResult>(member, comparer, comparer));
            }

            protected void AddCollectionMember<TResult>(Func<T, IList<TResult>> member)
            {
                this.members.Add(new MemberCollectionDef<TResult>(member, GetComparer<TResult>(), GetEqualityComparer<TResult>()));
            }

            protected void AddCollectionMember<TResult, TComparer>(Func<T, IList<TResult>> member, TComparer comparer)
                where TComparer : IComparer<TResult>, IEqualityComparer<TResult>
            {
                this.members.Add(new MemberCollectionDef<TResult>(member, comparer, comparer));
            }

            private static IComparer<TResult> GetComparer<TResult>()
            {
                if (typeof(Expression).IsAssignableFrom(typeof(TResult)))
                {
                    return DefaultExpressionComparer.Value as IComparer<TResult>;
                }

                if (typeof(TResult) == typeof(Type))
                {
                    return (IComparer<TResult>)DefaultTypeComparer.Value;
                }

                if (typeof(TResult) == typeof(MemberInfo))
                {
                    return (IComparer<TResult>)DefaultMemberInfoComparer.Value;
                }

                if (typeof(TResult) == typeof(string))
                {
                    return (IComparer<TResult>)DefaultStringComparer;
                }

                return Comparer<TResult>.Default;
            }

            private IEqualityComparer<TResult> GetEqualityComparer<TResult>()
            {
                if (typeof(Expression).IsAssignableFrom(typeof(TResult)))
                {
                    return DefaultExpressionComparer.Value as IEqualityComparer<TResult>;
                }

                if (typeof(TResult) == typeof(Type))
                {
                    return (IEqualityComparer<TResult>)DefaultTypeComparer.Value;
                }

                if (typeof(TResult) == typeof(MemberInfo))
                {
                    return (IEqualityComparer<TResult>)DefaultMemberInfoComparer.Value;
                }

                if (typeof(TResult) == typeof(string))
                {
                    return (IEqualityComparer<TResult>)DefaultStringComparer;
                }

                return EqualityComparer<TResult>.Default;
            }

            private abstract class MemberDef
            {
                public abstract int Compare(T x, T y);

                public abstract bool Equals(T x, T y);

                public abstract int GetHashCode(T obj);
            }

            private class MemberDef<TResult> : MemberDef
            {
                private readonly Func<T, TResult> member;
                private readonly IComparer<TResult> comparer;
                private readonly IEqualityComparer<TResult> equalityComparer;

                public MemberDef(Func<T, TResult> member, IComparer<TResult> comparer, IEqualityComparer<TResult> equalityComparer)
                {
                    this.member = member;
                    this.comparer = comparer;
                    this.equalityComparer = equalityComparer;
                }

                public override int Compare(T x, T y)
                {
                    return this.comparer.Compare(this.member(x), this.member(y));
                }

                public override bool Equals(T x, T y)
                {
                    return this.equalityComparer.Equals(this.member(x), this.member(y));
                }

                public override int GetHashCode(T obj)
                {
                    var value = this.member(obj);
                    return value != null ? this.equalityComparer.GetHashCode(value) : 0;
                }
            }

            private class MemberCollectionDef<TResult> : MemberDef
            {
                private readonly Func<T, IList<TResult>> member;
                private readonly IComparer<TResult> comparer;
                private readonly IEqualityComparer<TResult> equalityComparer;

                public MemberCollectionDef(Func<T, IList<TResult>> member, IComparer<TResult> comparer, IEqualityComparer<TResult> equalityComparer)
                {
                    this.member = member;
                    this.comparer = comparer;
                    this.equalityComparer = equalityComparer;
                }

                public override int Compare(T x, T y)
                {
                    IList<TResult> xCol = this.member(x), yCol = this.member(y);

                    if (ReferenceEquals(xCol, yCol))
                    {
                        return 0;
                    }

                    if (xCol == null)
                    {
                        return -1;
                    }

                    if (yCol == null)
                    {
                        return 1;
                    }

                    for (int i = 0; i < xCol.Count; i++)
                    {
                        var result = this.comparer.Compare(xCol[i], yCol[i]);

                        if (result != 0)
                        {
                            return result;
                        }
                    }

                    return 0;
                }

                public override bool Equals(T x, T y)
                {
                    IList<TResult> xCol = this.member(x), yCol = this.member(y);

                    if (ReferenceEquals(xCol, yCol))
                    {
                        return true;
                    }

                    if (xCol == null || yCol == null)
                    {
                        return false;
                    }

                    for (int i = 0; i < xCol.Count; i++)
                    {
                        if (!this.equalityComparer.Equals(xCol[i], yCol[i]))
                        {
                            return false;
                        }
                    }

                    return true;
                }

                public override int GetHashCode(T obj)
                {
                    IList<TResult> col = this.member(obj);

                    var resilt = 0;

                    if (col == null)
                    {
                        return resilt;
                    }

                    for (int i = 0; i < col.Count; i++)
                    {
                        if (col[i] != null)
                        {
                            unchecked
                            {
                                resilt += this.equalityComparer.GetHashCode(col[i]);
                            }
                        }
                    }

                    return resilt;
                }
            }
        }

        private abstract class GerericExpressionComparer<TExpression> : GerericComparer<TExpression>, IImplComparer
            where TExpression : Expression
        {
            public abstract IEnumerable<ExpressionType> Types { get; }

            public int Compare(Expression x, Expression y)
            {
                return base.Compare((TExpression)x, (TExpression)y);
            }

            public bool Equals(Expression x, Expression y)
            {
                return base.Equals((TExpression)x, (TExpression)y);
            }

            public int GetHashCode(Expression obj)
            {
                return base.GetHashCode((TExpression)obj);
            }
        }

        private class TypeComparer : BaseComparer<Type>
        {
            protected override int NotNullableCompare(Type x, Type y)
            {
                return DefaultStringComparer.Compare(x.FullName, y.FullName);
            }
        }

        private class MemberInfoComparer : BaseComparer<MemberInfo>
        {
            private TypeComparer typeComparer;

            public MemberInfoComparer(TypeComparer typeComparer)
            {
                this.typeComparer = typeComparer;
            }

            protected override int NotNullableCompare(MemberInfo x, MemberInfo y)
            {
                var result = this.typeComparer.Compare(x.DeclaringType, x.DeclaringType);

                if (result != 0)
                {
                    return result;
                }

                return DefaultStringComparer.Compare(x.Name, y.Name);
            }
        }

        private class ExpressionComparerImpl : BaseComparer<Expression>
        {
            protected override int NotNullableCompare(Expression x, Expression y)
            {
                if (x.NodeType != y.NodeType)
                {
                    return x.NodeType - y.NodeType;
                }

                var typeResult = DefaultTypeComparer.Value.Compare(x.Type, y.Type);

                if (typeResult != 0)
                {
                    return typeResult;
                }

                return ConcreteExpressionComparers.Value[x.NodeType].Compare(x, y);
            }

            protected override bool NotNullableEquals(Expression x, Expression y)
            {
                if (x.NodeType != y.NodeType)
                {
                    return false;
                }

                if (!DefaultTypeComparer.Value.Equals(x.Type, y.Type))
                {
                    return false;
                }

                return ConcreteExpressionComparers.Value[x.NodeType].Equals(x, y);
            }

            protected override int NotNullableGetHashCode(Expression obj)
            {
                var result = DefaultTypeComparer.Value.GetHashCode(obj.Type);

                return unchecked(result + ConcreteExpressionComparers.Value[obj.NodeType].GetHashCode(obj));
            }
        }

        private class UnaryExpressionComparer : GerericExpressionComparer<UnaryExpression>
        {
            public UnaryExpressionComparer()
            {
                this.AddMember(x => x.Method);
                this.AddMember(x => x.Operand);
            }

            public override IEnumerable<ExpressionType> Types
            {
                get
                {
                    yield return ExpressionType.UnaryPlus;
                    yield return ExpressionType.Negate;
                    yield return ExpressionType.NegateChecked;
                    yield return ExpressionType.Not;
                    yield return ExpressionType.Convert;
                    yield return ExpressionType.ConvertChecked;
                    yield return ExpressionType.ArrayLength;
                    yield return ExpressionType.Quote;
                    yield return ExpressionType.TypeAs;
                }
            }
        }

        private class BinaryExpressionComparer : GerericExpressionComparer<BinaryExpression>
        {
            public BinaryExpressionComparer()
            {
                this.AddMember(x => x.Method);
                this.AddMember(x => x.Left);
                this.AddMember(x => x.Right);
            }

            public override IEnumerable<ExpressionType> Types
            {
                get
                {
                    yield return ExpressionType.Add;
                    yield return ExpressionType.AddChecked;
                    yield return ExpressionType.Subtract;
                    yield return ExpressionType.SubtractChecked;
                    yield return ExpressionType.Multiply;
                    yield return ExpressionType.MultiplyChecked;
                    yield return ExpressionType.Divide;
                    yield return ExpressionType.Modulo;
                    yield return ExpressionType.Power;
                    yield return ExpressionType.And;
                    yield return ExpressionType.AndAlso;
                    yield return ExpressionType.Or;
                    yield return ExpressionType.OrElse;
                    yield return ExpressionType.LessThan;
                    yield return ExpressionType.LessThanOrEqual;
                    yield return ExpressionType.GreaterThan;
                    yield return ExpressionType.GreaterThanOrEqual;
                    yield return ExpressionType.Equal;
                    yield return ExpressionType.NotEqual;
                    yield return ExpressionType.Coalesce;
                    yield return ExpressionType.ArrayIndex;
                    yield return ExpressionType.RightShift;
                    yield return ExpressionType.LeftShift;
                    yield return ExpressionType.ExclusiveOr;
                }
            }
        }

        private class TypeBinaryExpressionComparer : GerericExpressionComparer<TypeBinaryExpression>
        {
            public TypeBinaryExpressionComparer()
            {
                this.AddMember(x => x.TypeOperand);
                this.AddMember(x => x.Expression);
            }

            public override IEnumerable<ExpressionType> Types
            {
                get { yield return ExpressionType.TypeIs; }
            }
        }

        private class ConditionalExpressionComparer : GerericExpressionComparer<ConditionalExpression>
        {
            public ConditionalExpressionComparer()
            {
                this.AddMember(x => x.Test);
                this.AddMember(x => x.IfTrue);
                this.AddMember(x => x.IfFalse);
            }

            public override IEnumerable<ExpressionType> Types
            {
                get { yield return ExpressionType.Conditional; }
            }
        }

        private class ConstantExpressionComparer : GerericExpressionComparer<ConstantExpression>
        {
            public ConstantExpressionComparer()
            {
                this.AddMember(x => x.Value);
            }

            public override IEnumerable<ExpressionType> Types
            {
                get { yield return ExpressionType.Constant; }
            }
        }

        private class ParameterExpressionComparer : GerericExpressionComparer<ParameterExpression>
        {
            public ParameterExpressionComparer()
            {
                this.AddMember(x => x.Name);
            }

            public override IEnumerable<ExpressionType> Types
            {
                get { yield return ExpressionType.Parameter; }
            }
        }

        private class MemberExpressionComparer : GerericExpressionComparer<MemberExpression>
        {
            public MemberExpressionComparer()
            {
                this.AddMember(x => x.Member);
                this.AddMember(x => x.Expression);
            }

            public override IEnumerable<ExpressionType> Types
            {
                get { yield return ExpressionType.MemberAccess; }
            }
        }

        private class MethodCallExpressionComparer : GerericExpressionComparer<MethodCallExpression>
        {
            public MethodCallExpressionComparer()
            {
                this.AddMember(x => x.Method);
                this.AddMember(x => x.Object);
                this.AddCollectionMember(x => x.Arguments);
            }

            public override IEnumerable<ExpressionType> Types
            {
                get { yield return ExpressionType.Call; }
            }
        }

        private class LambdaExpressionComparer : GerericExpressionComparer<LambdaExpression>
        {
            public LambdaExpressionComparer()
            {
                this.AddMember(x => x.Name);
                this.AddCollectionMember(x => x.Parameters);
                this.AddMember(x => x.ReturnType);
                this.AddMember(x => x.TailCall);
                this.AddMember(x => x.Body);
            }

            public override IEnumerable<ExpressionType> Types
            {
                get { yield return ExpressionType.Lambda; }
            }
        }

        private class SwitchExpressionComparer : GerericExpressionComparer<SwitchExpression>
        {
            public SwitchExpressionComparer()
            {
                this.AddMember(x => x.Comparison);
                this.AddMember(x => x.SwitchValue);
                this.AddCollectionMember(x => x.Cases, new SwitchCaseComparer());
                this.AddMember(x => x.DefaultBody);
            }

            public override IEnumerable<ExpressionType> Types
            {
                get { yield return ExpressionType.Switch; }
            }

            private class SwitchCaseComparer : GerericComparer<SwitchCase>
            {
                public SwitchCaseComparer()
                {
                    this.AddCollectionMember(x => x.TestValues);
                    this.AddMember(x => x.Body);
                }
            }
        }

        private class NewExpressionComparer : GerericExpressionComparer<NewExpression>
        {
            public NewExpressionComparer()
            {
                this.AddMember(x => x.Constructor);
                this.AddCollectionMember(x => x.Members);
                this.AddCollectionMember(x => x.Arguments);
            }

            public override IEnumerable<ExpressionType> Types
            {
                get { yield return ExpressionType.New; }
            }
        }

        private class NewArrayExpressionComparer : GerericExpressionComparer<NewArrayExpression>
        {
            public NewArrayExpressionComparer()
            {
                this.AddCollectionMember(x => x.Expressions);
            }

            public override IEnumerable<ExpressionType> Types
            {
                get
                {
                    yield return ExpressionType.NewArrayInit;
                    yield return ExpressionType.NewArrayBounds;
                }
            }
        }

        private class InvocationExpressionComparer : GerericExpressionComparer<InvocationExpression>
        {
            public InvocationExpressionComparer()
            {
                this.AddMember(x => x.Expression);
                this.AddCollectionMember(x => x.Arguments);
            }

            public override IEnumerable<ExpressionType> Types
            {
                get { yield return ExpressionType.Invoke; }
            }
        }

        private class MemberInitExpressionComparer : GerericExpressionComparer<MemberInitExpression>
        {
            public MemberInitExpressionComparer()
            {
                this.AddMember(x => x.NewExpression);
                this.AddCollectionMember(x => x.Bindings, new MemberBindingComparer());
            }

            public override IEnumerable<ExpressionType> Types
            {
                get { yield return ExpressionType.MemberInit; }
            }

            private class MemberBindingComparer : IComparer<MemberBinding>, IEqualityComparer<MemberBinding>
            {
                private readonly MemberAssignmentComparer memberAssignmentComparer = new MemberAssignmentComparer();
                private readonly MemberMemberBindingComparer memberMemberBindingComparer;
                private readonly MemberListBindingComparer memberListBindingComparer = new MemberListBindingComparer();

                public MemberBindingComparer()
                {
                    this.memberMemberBindingComparer = new MemberMemberBindingComparer(this);
                }

                public int Compare(MemberBinding x, MemberBinding y)
                {
                    if (ReferenceEquals(x, y))
                    {
                        return 0;
                    }

                    if (x == null)
                    {
                        return -1;
                    }

                    if (y == null)
                    {
                        return 1;
                    }

                    if (x.BindingType != y.BindingType)
                    {
                        return x.BindingType - y.BindingType;
                    }

                    switch (x.BindingType)
                    {
                        case MemberBindingType.Assignment:
                            return this.memberAssignmentComparer.Compare((MemberAssignment)x, (MemberAssignment)y);
                        case MemberBindingType.MemberBinding:
                            return this.memberMemberBindingComparer.Compare((MemberMemberBinding)x, (MemberMemberBinding)y);
                        case MemberBindingType.ListBinding:
                            return this.memberListBindingComparer.Compare((MemberListBinding)x, (MemberListBinding)y);
                        default:
                            throw new InvalidOperationException();
                    }
                }

                public bool Equals(MemberBinding x, MemberBinding y)
                {
                    if (ReferenceEquals(x, y))
                    {
                        return true;
                    }

                    if (x == null || y == null)
                    {
                        return false;
                    }

                    if (x.BindingType != y.BindingType)
                    {
                        return false;
                    }

                    switch (x.BindingType)
                    {
                        case MemberBindingType.Assignment:
                            return this.memberAssignmentComparer.Equals((MemberAssignment)x, (MemberAssignment)y);
                        case MemberBindingType.MemberBinding:
                            return this.memberMemberBindingComparer.Equals((MemberMemberBinding)x, (MemberMemberBinding)y);
                        case MemberBindingType.ListBinding:
                            return this.memberListBindingComparer.Equals((MemberListBinding)x, (MemberListBinding)y);
                        default:
                            throw new InvalidOperationException();
                    }
                }

                public int GetHashCode(MemberBinding obj)
                {
                    if (obj == null)
                    {
                        return 0;
                    }

                    switch (obj.BindingType)
                    {
                        case MemberBindingType.Assignment:
                            return this.memberAssignmentComparer.GetHashCode((MemberAssignment)obj);
                        case MemberBindingType.MemberBinding:
                            return this.memberMemberBindingComparer.GetHashCode((MemberMemberBinding)obj);
                        case MemberBindingType.ListBinding:
                            return this.memberListBindingComparer.GetHashCode((MemberListBinding)obj);
                        default:
                            throw new InvalidOperationException();
                    }
                }
            }

            private class MemberAssignmentComparer : GerericComparer<MemberAssignment>
            {
                public MemberAssignmentComparer()
                {
                    this.AddMember(x => x.Member);
                    this.AddMember(x => x.Expression);
                }
            }

            private class MemberMemberBindingComparer : GerericComparer<MemberMemberBinding>
            {
                public MemberMemberBindingComparer(MemberBindingComparer memberBindingComparer)
                {
                    this.AddMember(x => x.Member);
                    this.AddCollectionMember(x => x.Bindings, memberBindingComparer);
                }
            }

            private class MemberListBindingComparer : GerericComparer<MemberListBinding>
            {
                public MemberListBindingComparer()
                {
                    this.AddMember(x => x.Member);
                    this.AddCollectionMember(x => x.Initializers, new ElementInitComparer());
                }

                private class ElementInitComparer : GerericComparer<ElementInit>
                {
                    public ElementInitComparer()
                    {
                        this.AddMember(x => x.AddMethod);
                        this.AddCollectionMember(x => x.Arguments);
                    }
                }
            }
        }

        private class ListInitExpressionComparer : GerericExpressionComparer<ListInitExpression>
        {
            public ListInitExpressionComparer()
            {
                this.AddMember(x => x.NewExpression);
                this.AddCollectionMember(x => x.Initializers, new ElementInitComparer());
            }

            public override IEnumerable<ExpressionType> Types
            {
                get { yield return ExpressionType.ListInit; }
            }

            private class ElementInitComparer : GerericComparer<ElementInit>
            {
                public ElementInitComparer()
                {
                    this.AddMember(x => x.AddMethod);
                    this.AddCollectionMember(x => x.Arguments);
                }
            }
        }
    }
}
