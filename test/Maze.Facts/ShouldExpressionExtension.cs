using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Xunit.Sdk;

namespace Maze.Facts
{
    public static class ShouldExpressionExtension
    {
        public static void ShouldEqual(this Expression actual, Expression expected)
        {
            Assert(expected, actual);
        }

        public static void ShouldEqualExpression<TDelegate>(this Expression actual, Expression<TDelegate> expected)
        {
            Assert(expected, actual);
        }

        public static void ShouldEqual<TDelegate>(this Expression<TDelegate> actual, Expression<TDelegate> expected)
        {
            Assert(expected, actual);
        }

        private static void Assert(Expression expected, Expression actual)
        {
            if (expected == null || actual == null)
            {
                if (expected != null || actual != null)
                {
                    throw new Exception();
                }

                return;
            }

            if (expected.NodeType != actual.NodeType)
            {
                throw new Exception();
            }

            switch (expected.NodeType)
            {
                case ExpressionType.UnaryPlus:
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.ArrayLength:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                    AssertUnary((UnaryExpression)expected, (UnaryExpression)actual);
                    break;
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
                    AssertBinary((BinaryExpression)expected, (BinaryExpression)actual);
                    break;
                case ExpressionType.TypeIs:
                    AssertTypeIs((TypeBinaryExpression)expected, (TypeBinaryExpression)actual);
                    break;
                case ExpressionType.Conditional:
                    AssertConditional((ConditionalExpression)expected, (ConditionalExpression)actual);
                    break;
                case ExpressionType.Constant:
                    AssertConstant((ConstantExpression)expected, (ConstantExpression)actual);
                    break;
                case ExpressionType.Parameter:
                    AssertParameter((ParameterExpression)expected, (ParameterExpression)actual);
                    break;
                case ExpressionType.MemberAccess:
                    AssertMemberAccess((MemberExpression)expected, (MemberExpression)actual);
                    break;
                case ExpressionType.Call:
                    AssertMethodCall((MethodCallExpression)expected, (MethodCallExpression)actual);
                    break;
                case ExpressionType.Lambda:
                    AssertLambda((LambdaExpression)expected, (LambdaExpression)actual);
                    break;
                case ExpressionType.Switch:
                    AssertSwitch((SwitchExpression)expected, (SwitchExpression)actual);
                    break;
                case ExpressionType.New:
                    AssertNew((NewExpression)expected, (NewExpression)actual);
                    break;
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    AssertNewArray((NewArrayExpression)expected, (NewArrayExpression)actual);
                    break;
                case ExpressionType.Invoke:
                    AssertInvocation((InvocationExpression)expected, (InvocationExpression)actual);
                    break;
                case ExpressionType.MemberInit:
                    AssertMemberInit((MemberInitExpression)expected, (MemberInitExpression)actual);
                    break;
                case ExpressionType.ListInit:
                    AssertListInit((ListInitExpression)expected, (ListInitExpression)actual);
                    break;
                default:
                    throw new InvalidOperationException("Unhandled Expression Type: " + expected.NodeType);
            }
        }

        private static void AssertCollection<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual, Action<T, T> asert)
        {
            if (expected.Count != actual.Count)
            {
                throw new EqualException(expected.Count, actual.Count);
            }

            for (int i = 0; i < expected.Count; i++)
            {
                asert(expected[i], actual[i]);
            }
        }

        private static void AssertName(string expected, string actual)
        {
            if (expected != actual)
            {
                throw new Exception();
            }
        }

        private static void AssertType(Type expected, Type actual)
        {
            if (expected.IsAnonymous())
            {
                return;
            }

            if (expected.IsGenericType || actual.IsGenericType)
            {
                if (expected.IsAssignableFrom(actual))
                {
                    return;
                }

                if(!expected.IsGenericType || !actual.IsGenericType)
                {
                    throw new Exception();
                }

                if (expected.GetGenericTypeDefinition() != actual.GetGenericTypeDefinition())
                {
                    string expectedTypeName = expected.FullName;
                    string actualTypeName = actual.FullName;

                    if (expectedTypeName == actualTypeName)
                    {
                        expectedTypeName += string.Format(" ({0})", expected.GetTypeInfo().Assembly.GetName().FullName);
                        actualTypeName += string.Format(" ({0})", actual.GetTypeInfo().Assembly.GetName().FullName);
                    }

                    throw new IsTypeException(expectedTypeName, actualTypeName);
                }

                AssertCollection(expected.GetGenericArguments(), actual.GetGenericArguments(), AssertType);
            }
            else if (expected != actual)
            {
                if (expected == typeof(object) && typeof(TypeFactory.DynamicProxy).IsAssignableFrom(actual))
                {
                    return;
                }

                string expectedTypeName = expected.FullName;
                string actualTypeName = actual.FullName;

                if (expectedTypeName == actualTypeName)
                {
                    expectedTypeName += string.Format(" ({0})", expected.GetTypeInfo().Assembly.GetName().FullName);
                    actualTypeName += string.Format(" ({0})", actual.GetTypeInfo().Assembly.GetName().FullName);
                }

                throw new IsTypeException(expectedTypeName, actualTypeName);
            }
        }

        private static void AssertMember(MemberInfo expected, MemberInfo actual)
        {
            if (expected.MemberType != actual.MemberType)
            {
                throw new Exception();
            }

            switch (expected.MemberType)
            {
                case MemberTypes.Property:
                    AssertProperty((PropertyInfo)expected, (PropertyInfo)actual);
                    break;
                default:
                    throw new InvalidOperationException("Unhandled Member Type: " + expected.MemberType);
            }
        }

        private static void AssertMethod(MethodInfo expected, MethodInfo actual)
        {
            AssertName(expected.Name, actual.Name);

            AssertType(expected.DeclaringType, actual.DeclaringType);

            AssertType(expected.ReturnType, actual.ReturnType);

            if (expected.IsGenericMethod || actual.IsGenericMethod)
            {
                if (!expected.IsGenericMethod || !actual.IsGenericMethod)
                {
                    throw new Exception();
                }

                AssertCollection(expected.GetGenericArguments(), actual.GetGenericArguments(), AssertType);
            }

            AssertCollection(expected.GetParameters(), actual.GetParameters(), AssertParameterInfo);
        }

        private static void AssertProperty(PropertyInfo expected, PropertyInfo actual)
        {
            AssertName(expected.Name, actual.Name);

            AssertType(expected.DeclaringType, actual.DeclaringType);

            AssertType(expected.PropertyType, actual.PropertyType);
        }

        private static void AssertParameterInfo(ParameterInfo expected, ParameterInfo actual)
        {
            AssertName(expected.Name, actual.Name);

            AssertType(expected.ParameterType, actual.ParameterType);
        }

        private static void AssertBinding(MemberBinding expected, MemberBinding actual)
        {
            if (expected.BindingType != actual.BindingType)
            {
                throw new Exception();
            }

            AssertMember(expected.Member, actual.Member);

            switch (expected.BindingType)
            {
                case MemberBindingType.Assignment:
                    AssertMemberAssignment((MemberAssignment)expected, (MemberAssignment)actual);
                    break;
                case MemberBindingType.MemberBinding:
                    AssertMemberMemberBinding((MemberMemberBinding)expected, (MemberMemberBinding)actual);
                    break;
                case MemberBindingType.ListBinding:
                    AssertMemberListBinding((MemberListBinding)expected, (MemberListBinding)actual);
                    break;
                default:
                    throw new InvalidOperationException("Unhandled Binding Type: " + expected.BindingType);
            }
        }

        private static void AssertMemberAssignment(MemberAssignment expected, MemberAssignment actual)
        {
            Assert(expected.Expression, actual.Expression);
        }

        private static void AssertMemberMemberBinding(MemberMemberBinding expected, MemberMemberBinding actual)
        {
            AssertCollection(expected.Bindings, actual.Bindings, AssertBinding);
        }

        private static void AssertMemberListBinding(MemberListBinding expected, MemberListBinding actual)
        {
            AssertCollection(expected.Initializers, actual.Initializers, AssertElementInitializer);
        }

        private static void AssertElementInitializer(ElementInit expected, ElementInit actual)
        {
            AssertMethod(expected.AddMethod, actual.AddMethod);

            AssertCollection(expected.Arguments, actual.Arguments, Assert);
        }

        private static void AssertUnary(UnaryExpression expected, UnaryExpression actual)
        {
            Assert(expected.Operand, actual.Operand);
            
            AssertType(expected.Type, actual.Type);

            if (expected.Method != null || actual.Method != null)
            {
                if (expected.Method == null || actual.Method == null)
                {
                    throw new Exception();
                }

                AssertMethod(expected.Method, actual.Method);
            }
        }

        private static void AssertBinary(BinaryExpression expected, BinaryExpression actual)
        {
            Assert(expected.Left, actual.Left);

            Assert(expected.Right, actual.Right);

            Assert(expected.Conversion, actual.Conversion);

            AssertType(expected.Type, actual.Type);

            if (expected.Method != null || actual.Method != null)
            {
                if (expected.Method == null || actual.Method == null)
                {
                    throw new Exception();
                }

                AssertMethod(expected.Method, actual.Method);
            }
        }

        private static void AssertTypeIs(TypeBinaryExpression expected, TypeBinaryExpression actual)
        {
            Assert(expected.Expression, actual.Expression);

            AssertType(expected.TypeOperand, actual.TypeOperand);
        }

        private static void AssertConstant(ConstantExpression expected, ConstantExpression actual)
        {
            Xunit.Assert.Equal(expected.Value, actual.Value);
        }

        private static void AssertConditional(ConditionalExpression expected, ConditionalExpression actual)
        {
            Assert(expected.Test, actual.Test);
            Assert(expected.IfTrue, actual.IfTrue);
            Assert(expected.IfFalse, actual.IfFalse);
        }

        private static void AssertParameter(ParameterExpression expected, ParameterExpression actual)
        {
            AssertType(expected.Type, actual.Type);

            if (expected.Name != actual.Name)
            {
                throw new Exception();
            }
        }

        private static void AssertMemberAccess(MemberExpression expected, MemberExpression actual)
        {
            AssertMember(expected.Member, actual.Member);

            Assert(expected.Expression, actual.Expression);
        }

        private static void AssertMethodCall(MethodCallExpression expected, MethodCallExpression actual)
        {
            AssertMethod(expected.Method, actual.Method);

            Assert(expected.Object, actual.Object);

            AssertCollection(expected.Arguments, actual.Arguments, Assert);
        }

        private static void AssertLambda(LambdaExpression expected, LambdaExpression actual)
        {
            AssertType(expected.ReturnType, actual.ReturnType);

            AssertCollection(expected.Parameters, actual.Parameters, AssertParameter);

            Assert(expected.Body, actual.Body);
        }

        private static void AssertSwitch(SwitchExpression expected, SwitchExpression actual)
        {
            Assert(expected.SwitchValue, actual.SwitchValue);

            AssertMethod(expected.Comparison, actual.Comparison);

            AssertCollection(expected.Cases, actual.Cases, AssertSwitchCase);

            Assert(expected.DefaultBody, actual.DefaultBody);
        }

        private static void AssertSwitchCase(SwitchCase expected, SwitchCase actual)
        {
            AssertCollection(expected.TestValues, actual.TestValues, Assert);

            Assert(expected.Body, actual.Body);
        }

        private static void AssertNew(NewExpression expected, NewExpression actual)
        {
            AssertType(expected.Type, actual.Type);

            AssertCollection(expected.Arguments, actual.Arguments, Assert);

            if (expected.Members != null || actual.Members != null)
            {
                if (expected.Members == null || actual.Members == null)
                {
                    throw new Exception();
                }

                AssertCollection(expected.Members, actual.Members, AssertMember);
            }
        }

        private static void AssertMemberInit(MemberInitExpression expected, MemberInitExpression actual)
        {
            AssertType(expected.Type, actual.Type);
            
            Assert(expected.NewExpression, actual.NewExpression);

            AssertCollection(expected.Bindings, actual.Bindings, AssertBinding);
        }

        private static void AssertListInit(ListInitExpression expected, ListInitExpression actual)
        {
            throw new NotImplementedException();
        }

        private static void AssertNewArray(NewArrayExpression expected, NewArrayExpression actual)
        {
            AssertType(expected.Type, actual.Type);

            AssertCollection(expected.Expressions, actual.Expressions, Assert);
        }

        private static void AssertInvocation(InvocationExpression expected, InvocationExpression actual)
        {
            throw new NotImplementedException();
        }

        private static bool IsAnonymous(this Type type)
        {
            return Attribute.IsDefined(type, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false)
                && type.Name.StartsWith("<>") && type.Name.Contains("AnonymousType")
                && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }
    }
}
