using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Maze
{
    public class EnumerableRewriter : ExpressionVisitor
    {
        private static readonly ILookup<string, MethodInfo> EnumerableMethods;

        private readonly ImmutableDictionary<ParameterExpression, ParameterExpression> replaced;

        static EnumerableRewriter()
        {
            EnumerableMethods = typeof(Enumerable).GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Concat(typeof(Maze.Linq.Enumerable).GetMethods(BindingFlags.Static | BindingFlags.Public))
                .ToLookup(m => m.Name);
        }

        public EnumerableRewriter()
            : this(ImmutableDictionary<ParameterExpression, ParameterExpression>.Empty)
        {
        }

        private EnumerableRewriter(ImmutableDictionary<ParameterExpression, ParameterExpression> replaced)
        {
            this.replaced = replaced;
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            List<ParameterExpression> parameters = new List<ParameterExpression>(node.Parameters.Count);

            var newReplaced = this.replaced;

            for (int i = 0; i < node.Parameters.Count; i++)
            {
                var param = node.Parameters[i];

                if (param.Type.EqualsGenericDefinition(typeof(IQueryable<>)))
                {
                    var newParam = Expression.Parameter(typeof(IEnumerable<>).MakeGenericType(param.Type.GetGenericArguments()), param.Name);
                    newReplaced = newReplaced.Add(param, newParam);

                    parameters.Add(newParam);
                }
                else
                {
                    parameters.Add(param);
                }
            }

            var rewriter = new EnumerableRewriter(newReplaced);

            var body = rewriter.Visit(node.Body);

            if (body == node.Body)
            {
                return node;
            }

            return Expression.Lambda(body, parameters);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            ParameterExpression result;
            if (this.replaced.TryGetValue(node, out result))
            {
                return result;
            }

            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType.Name == "Queryable")
            {
                var typeArgs = node.Method.IsGenericMethod ? node.Method.GetGenericArguments() : null;

                var args = new Expression[node.Arguments.Count];
                for (int i = 0; i < node.Arguments.Count; i++)
                {
                    var expr = this.Visit(node.Arguments[i]);

                    while (expr.NodeType == ExpressionType.Quote)
                    {
                        expr = ((UnaryExpression)expr).Operand;
                    }

                    args[i] = expr;
                }

                var method = FindEnumerableMethod(node.Method.Name, new ReadOnlyCollection<Expression>(args), typeArgs);

                return Expression.Call(method, args);
            }

            return base.VisitMethodCall(node);
        }

        private static MethodInfo FindEnumerableMethod(string name, ReadOnlyCollection<Expression> args, Type[] typeArgs)
        {
            MethodInfo mi = EnumerableMethods[name].FirstOrDefault(m => ArgsMatch(m, args, typeArgs));

            if (mi == null)
            {
                throw new InvalidOperationException("Enumerable method is not found: " + name);
            }

            return typeArgs != null ? mi.MakeGenericMethod(typeArgs) : mi;
        }

        private static bool ArgsMatch(MethodInfo method, ReadOnlyCollection<Expression> args, Type[] typeArgs)
        {
            ParameterInfo[] @params = method.GetParameters();

            if (@params.Length != args.Count)
            {
                return false;
            }

            if (!method.IsGenericMethod && typeArgs != null && typeArgs.Length > 0)
            {
                return false;
            }

            if (!method.IsGenericMethodDefinition && method.IsGenericMethod && method.ContainsGenericParameters)
            {
                method = method.GetGenericMethodDefinition();
            }

            if (method.IsGenericMethodDefinition)
            {
                if (typeArgs == null || typeArgs.Length == 0)
                {
                    return false;
                }

                if (method.GetGenericArguments().Length != typeArgs.Length)
                {
                    return false;
                }

                method = method.MakeGenericMethod(typeArgs);
                @params = method.GetParameters();
            }

            for (int i = 0, n = args.Count; i < n; i++)
            {
                Type parameterType = @params[i].ParameterType;
                if (parameterType == null)
                {
                    return false;
                }

                if (parameterType.IsByRef)
                {
                    parameterType = parameterType.GetElementType();
                }

                Expression arg = args[i];
                if (!parameterType.IsAssignableFrom(arg.Type))
                {
                    if (arg.NodeType == ExpressionType.Quote)
                    {
                        arg = ((UnaryExpression)arg).Operand;
                    }

                    if (!parameterType.IsAssignableFrom(arg.Type))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public class Replaced
        {
            public Replaced(ImmutableDictionary<ParameterExpression, ParameterExpression> replaced)
            {
                this.ReplacedItems = replaced;
            }

            public ImmutableDictionary<ParameterExpression, ParameterExpression> ReplacedItems { get; private set; }
        }
    }
}
