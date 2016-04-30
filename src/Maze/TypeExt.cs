using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Maze
{
    public static class TypeExt
    {
        public static bool IsAnonymousType(this Type type)
        {
            return type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Any() && type.Name.Contains("AnonymousType");
        }

        internal static bool IsNumericType(Type type)
        {
            if (type == null)
            {
                return false;
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;

                default:
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        return IsNumericType(Nullable.GetUnderlyingType(type));
                    }

                    return false;
            }
        }

        internal static Type FindGenericType(this Type type, Type definition)
        {
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == definition)
                {
                    return type;
                }

                if (definition.IsInterface)
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

                type = type.BaseType;
            }

            return null;
        }

        internal static TResult InvokeGenericMethod<TResult>(Delegate method, Type typeArg, params object[] args)
        {
            return (TResult)method.Method.GetGenericMethodDefinition().MakeGenericMethod(typeArg).Invoke(method.Target, args);
        }

        internal static TResult CallGenericMethod<T, TResult>(Func<T, TResult> method, T arg, params Type[] typeArgs)
        {
            return (TResult)method.Method.GetGenericMethodDefinition().MakeGenericMethod(typeArgs).Invoke(method.Target, new object[] { arg });
        }
    }
}
