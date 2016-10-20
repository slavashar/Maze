using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Maze
{
    public static class TypeExt
    {
        public static bool IsAnonymousType(this Type type)
        {
#if NET45
            return type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Any() && type.Name.Contains("AnonymousType");
#else
            return type.GetTypeInfo().GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Any() && type.Name.Contains("AnonymousType");
#endif
        }

        internal static bool IsNumericType(Type type)
        {
            if (type == null)
            {
                return false;
            }

            if (type == typeof(byte) ||
                type == typeof(sbyte) ||
                type == typeof(decimal) ||
                type == typeof(float) ||
                type == typeof(double) ||
                type == typeof(short) ||
                type == typeof(ushort) ||
                type == typeof(int) ||
                type == typeof(uint) ||
                type == typeof(long) ||
                type == typeof(ulong))
            {
                return true;
            }

            if (type.EqualsGenericDefinition(typeof(Nullable<>)))
            {
                return IsNumericType(Nullable.GetUnderlyingType(type));
            }

            return false;
        }

        internal static Type FindGenericType(this Type type, Type definition)
        {
            while (type != null && type != typeof(object))
            {
                if (type.EqualsGenericDefinition(definition))
                {
                    return type;
                }

#if NET45
                if (definition.IsInterface)
#else
                if (definition.GetTypeInfo().IsInterface)
#endif
                {
                    foreach (var interfaceType in type.GetInterfaces())
                    {
                        var result = FindGenericType(interfaceType, definition);

                        if (result != null)
                        {
                            return result;
                        }
                    }
                }

                type = type.GetTypeInfo().BaseType;
            }

            return null;
        }

        internal static TResult InvokeGenericMethod<TResult>(Delegate method, Type typeArg, params object[] args)
        {
            return (TResult)method.GetGenericMethodDefinition().MakeGenericMethod(typeArg).Invoke(method.Target, args);
        }

        internal static TResult CallGenericMethod<T, TResult>(Func<T, TResult> method, T arg, params Type[] typeArgs)
        {
            return (TResult)method.GetGenericMethodDefinition().MakeGenericMethod(typeArgs).Invoke(method.Target, new object[] { arg });
        }

        internal static MethodInfo GetGenericMethodDefinition(this Delegate deleg)
        {
#if NET45
            return deleg.Method.GetGenericMethodDefinition();
#else
            return deleg.GetMethodInfo().GetGenericMethodDefinition();
#endif
        }

        internal static bool IsGenericType(this Type type)
        {
#if NET45
            return type.IsGenericType;
#else
            return type.GetTypeInfo().IsGenericType;
#endif
        }

        internal static bool EqualsGenericDefinition(this Type type, Type definition)
        {
#if NET45
            return type.IsGenericType && type.GetGenericTypeDefinition() == definition;
#else
            return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == definition;
#endif
        }

        internal static System.Reflection.MethodInfo GetMethodDefinition(Expression<Func<object>> methodCall)
        {
            if (methodCall.Body.NodeType != ExpressionType.Call)
            {
                throw new InvalidOperationException("Expression should be a method call");
            }

            var method = ((MethodCallExpression)methodCall.Body).Method;

            return method.GetGenericMethodDefinition();
        }
    }
}
