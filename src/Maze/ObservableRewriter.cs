﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reflection;
using Maze.Reactive;
using AbstractTypeEmitter = Castle.DynamicProxy.Generators.Emitters.AbstractTypeEmitter;
using ArgumentReference = Castle.DynamicProxy.Generators.Emitters.SimpleAST.ArgumentReference;
using AssignStatement = Castle.DynamicProxy.Generators.Emitters.SimpleAST.AssignStatement;
using MethodEmitter = Castle.DynamicProxy.Generators.Emitters.MethodEmitter;
using ModuleScope = Castle.DynamicProxy.ModuleScope;
using Observable = System.Reactive.Linq.Observable;
using ObservableMaze = Maze.Reactive.Observable;
using ReferenceExpression = Castle.DynamicProxy.Generators.Emitters.SimpleAST.ReferenceExpression;
using ReturnStatement = Castle.DynamicProxy.Generators.Emitters.SimpleAST.ReturnStatement;

namespace Maze
{
    public class ObservableRewriter : ExpressionVisitor
    {
        private static readonly IDictionary<MethodInfo, MethodInfo[]> MethodMap = new Dictionary<MethodInfo, MethodInfo[]>
        {
            [new Func<IEnumerable<object>, Func<object, object>, IEnumerable<object>>(Enumerable.Select).Method.GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, Func<object, object>, IObservable<object>>(Observable.Select).Method.GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, Func<object, object>, IEnumerable<object>>(Enumerable.Select).Method.GetGenericMethodDefinition()
            },

            [new Func<IEnumerable<object>, Func<object, int, object>, IEnumerable<object>>(Enumerable.Select).Method.GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, Func<object, int, object>, IObservable<object>>(Observable.Select).Method.GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, Func<object, int, object>, IEnumerable<object>>(Enumerable.Select).Method.GetGenericMethodDefinition()
            },

            [new Func<IQueryable<object>, Expression<Func<object, object>>, IQueryable<object>>(Queryable.Select).Method.GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, Func<object, object>, IObservable<object>>(Observable.Select).Method.GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, Func<object, object>, IEnumerable<object>>(Enumerable.Select).Method.GetGenericMethodDefinition()
            },

            [new Func<IQueryable<object>, Expression<Func<object, int, object>>, IQueryable<object>>(Queryable.Select).Method.GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, Func<object, int, object>, IObservable<object>>(Observable.Select).Method.GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, Func<object, int, object>, IEnumerable<object>>(Enumerable.Select).Method.GetGenericMethodDefinition()
            },

            [new Func<IEnumerable<object>, Func<object, IEnumerable<object>>, IEnumerable<object>>(Enumerable.SelectMany).Method.GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, Func<object, IObservable<object>>, IObservable<object>>(Observable.SelectMany).Method.GetGenericMethodDefinition(),
                new Func<IObservable<object>, Func<object, IEnumerable<object>>, IObservable<object>>(Observable.SelectMany).Method.GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, Func<object, IEnumerable<object>>, IEnumerable<object>>(Enumerable.SelectMany).Method.GetGenericMethodDefinition()
            },

            [new Func<IEnumerable<object>, Func<object, IEnumerable<object>>, Func<object, object, object>, IEnumerable<object>>(Enumerable.SelectMany).Method.GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, Func<object, IObservable<object>>, Func<object, object, object>, IObservable<object>>(Observable.SelectMany).Method.GetGenericMethodDefinition(),
                new Func<IObservable<object>, Func<object, IEnumerable<object>>, Func<object, object, object>, IObservable<object>>(Observable.SelectMany).Method.GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, Func<object, IEnumerable<object>>, Func<object, object, object>, IEnumerable<object>>(Enumerable.SelectMany).Method.GetGenericMethodDefinition()
            },

            [new Func<IQueryable<object>, Expression<Func<object, IEnumerable<object>>>, IQueryable<object>>(Queryable.SelectMany).Method.GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, Func<object, IObservable<object>>, IObservable<object>>(Observable.SelectMany).Method.GetGenericMethodDefinition(),
                new Func<IObservable<object>, Func<object, IEnumerable<object>>, IObservable<object>>(Observable.SelectMany).Method.GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, Func<object, IEnumerable<object>>, IEnumerable<object>>(Enumerable.SelectMany).Method.GetGenericMethodDefinition()
            },

            [new Func<IQueryable<object>, Expression<Func<object, IEnumerable<object>>>, Expression<Func<object, object, object>>, IQueryable<object>>(Queryable.SelectMany).Method.GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, Func<object, IObservable<object>>, Func<object, object, object>, IObservable<object>>(Observable.SelectMany).Method.GetGenericMethodDefinition(),
                new Func<IObservable<object>, Func<object, IEnumerable<object>>, Func<object, object, object>, IObservable<object>>(Observable.SelectMany).Method.GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, Func<object, IEnumerable<object>>, Func<object, object, object>, IEnumerable<object>>(Enumerable.SelectMany).Method.GetGenericMethodDefinition()
            },

            [new Func<IEnumerable<object>, Func<object, bool>, IEnumerable<object>>(Enumerable.Where).Method.GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, Func<object, bool>, IObservable<object>>(Observable.Where).Method.GetGenericMethodDefinition(),
                new Func<IObservable<object>, Func<object, IObservable<bool>>, IObservable<object>>(ObservableMaze.Where).Method.GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, Func<object, bool>, IEnumerable<object>>(Enumerable.Where).Method.GetGenericMethodDefinition()
            },

            [new Func<IQueryable<object>, Expression<Func<object, bool>>, IQueryable<object>>(Queryable.Where).Method.GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, Func<object, bool>, IObservable<object>>(Observable.Where).Method.GetGenericMethodDefinition(),
                new Func<IObservable<object>, Func<object, IObservable<bool>>, IObservable<object>>(ObservableMaze.Where).Method.GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, Func<object, bool>, IEnumerable<object>>(Enumerable.Where).Method.GetGenericMethodDefinition()
            },

            [new Func<IEnumerable<object>, object>(Enumerable.First).Method.GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>>(Observable.FirstAsync).Method.GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, object>(Enumerable.First).Method.GetGenericMethodDefinition(),
            },

            [new Func<IEnumerable<object>, object>(Enumerable.FirstOrDefault).Method.GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>>(Observable.FirstOrDefaultAsync).Method.GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, object>(Enumerable.FirstOrDefault).Method.GetGenericMethodDefinition(),
            },

            [new Func<IEnumerable<object>, object>(Enumerable.Last).Method.GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>>(Observable.LastAsync).Method.GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, object>(Enumerable.Last).Method.GetGenericMethodDefinition(),
            },

            [new Func<IEnumerable<object>, object>(Enumerable.LastOrDefault).Method.GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>>(Observable.LastOrDefaultAsync).Method.GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, object>(Enumerable.LastOrDefault).Method.GetGenericMethodDefinition(),
            },

            [new Func<IEnumerable<object>, object>(Enumerable.Single).Method.GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>>(Observable.SingleAsync).Method.GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, object>(Enumerable.Single).Method.GetGenericMethodDefinition(),
            },

            [new Func<IEnumerable<object>, object>(Enumerable.SingleOrDefault).Method.GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>>(Observable.SingleOrDefaultAsync).Method.GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, object>(Enumerable.SingleOrDefault).Method.GetGenericMethodDefinition(),
            },

            [new Func<IEnumerable<object>, int>(Enumerable.Count).Method.GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<int>>(Observable.Count).Method.GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, int>(Enumerable.Count).Method.GetGenericMethodDefinition(),
            },

            [new Func<IEnumerable<object>, object>(Enumerable.Max).Method.GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>>(Observable.Max).Method.GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, object>(Enumerable.Max).Method.GetGenericMethodDefinition()
            },

            [new Func<IEnumerable<object>, object>(Enumerable.Min).Method.GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>>(Observable.Min).Method.GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, object>(Enumerable.Min).Method.GetGenericMethodDefinition()
            },

            [new Func<IEnumerable<object>, object, bool>(Enumerable.Contains).Method.GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, object, IObservable<bool>>(Observable.Contains).Method.GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, object, bool>(Enumerable.Contains).Method.GetGenericMethodDefinition(),
            },

            [new Func<IEnumerable<object>, IEnumerable<object>, Func<object, object>, Func<object, object>, Func<object, object, object>, IEnumerable<object>>(Enumerable.Join).Method.GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>, Func<object, object>, Func<object, object>, Func<object, object, object>, IObservable<object>>(ObservableMaze.Join).Method.GetGenericMethodDefinition(),
                new Func<IObservable<object>, IObservable<object>, Func<object, IObservable<object>>, Func<object, IObservable<object>>, Func<object, object, object>, IObservable<object>>(ObservableMaze.Join).Method.GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, IEnumerable<object>, Func<object, object>, Func<object, object>, Func<object, object, object>, IEnumerable<object>>(Enumerable.Join).Method.GetGenericMethodDefinition()
            },

            [new Func<IQueryable<object>, IEnumerable<object>, Expression<Func<object, object>>, Expression<Func<object, object>>, Expression<Func<object, object, object>>, IQueryable<object>>(Queryable.Join).Method.GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>, Func<object, object>, Func<object, object>, Func<object, object, object>, IObservable<object>>(ObservableMaze.Join).Method.GetGenericMethodDefinition(),
                new Func<IObservable<object>, IObservable<object>, Func<object, IObservable<object>>, Func<object, IObservable<object>>, Func<object, object, object>, IObservable<object>>(ObservableMaze.Join).Method.GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, IEnumerable<object>, Func<object, object>, Func<object, object>, Func<object, object, object>, IEnumerable<object>>(Enumerable.Join).Method.GetGenericMethodDefinition()
            },

            [new Func<IEnumerable<object>, IEnumerable<object>, Func<object, object>, Func<object, object>, Func<object, IEnumerable<object>, object>, IEnumerable<object>>(Enumerable.GroupJoin).Method.GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>, Func<object, object>, Func<object, object>, Func<object, IObservable<object>, object>, IObservable<object>>(ObservableMaze.GroupJoin).Method.GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, IEnumerable<object>, Func<object, object>, Func<object, object>, Func<object, IEnumerable<object>, object>, IEnumerable<object>>(Enumerable.GroupJoin).Method.GetGenericMethodDefinition()
            },

            [new Func<IQueryable<object>, IEnumerable<object>, Expression<Func<object, object>>, Expression<Func<object, object>>, Expression<Func<object, IEnumerable<object>, object>>, IQueryable<object>>(Queryable.GroupJoin).Method.GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>, Func<object, object>, Func<object, object>, Func<object, IObservable<object>, object>, IObservable<object>>(ObservableMaze.GroupJoin).Method.GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, IEnumerable<object>, Func<object, object>, Func<object, object>, Func<object, IEnumerable<object>, object>, IEnumerable<object>>(Enumerable.GroupJoin).Method.GetGenericMethodDefinition()
            },

            [new Func<IEnumerable<object>, Func<object, object>, IEnumerable<IGrouping<object, object>>>(Enumerable.GroupBy).Method.GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, Func<object, object>, IObservable<IGroupedObservable<object, object>>>(Observable.GroupBy).Method.GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, Func<object, object>, IEnumerable<IGrouping<object, object>>>(Enumerable.GroupBy).Method.GetGenericMethodDefinition()
            },

            [new Func<IEnumerable<object>, IEnumerable<object>, IEnumerable<object>>(Enumerable.Concat).Method.GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>, IObservable<object>>(Observable.Merge).Method.GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, IEnumerable<object>, IEnumerable<object>>(Enumerable.Concat).Method.GetGenericMethodDefinition()
            },

            [new Func<IQueryable<object>, IQueryable<object>, IQueryable<object>>(Queryable.Concat).Method.GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>, IObservable<object>>(Observable.Merge).Method.GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, IEnumerable<object>, IEnumerable<object>>(Enumerable.Concat).Method.GetGenericMethodDefinition()
            },
        };

        private readonly ModuleScope scope;

        private readonly ImmutableDictionary<ParameterExpression, ParameterExpression> parameterMap;
        private readonly ImmutableDictionary<MemberInfo, ParameterExpression> memberMap;

        public ObservableRewriter()
            : this(new ModuleScope(), ImmutableDictionary<ParameterExpression, ParameterExpression>.Empty, ImmutableDictionary<MemberInfo, ParameterExpression>.Empty)
        {
        }

        private ObservableRewriter(ModuleScope scope, ImmutableDictionary<ParameterExpression, ParameterExpression> parameterMap, ImmutableDictionary<MemberInfo, ParameterExpression> memberMap)
        {
            this.scope = scope;
            this.parameterMap = parameterMap;
            this.memberMap = memberMap;
        }

        public static LambdaExpression ChangeParameters(LambdaExpression lambda)
        {
            // Convert the parameter to observable
            var newParams = Visit(lambda.Parameters, VisitLambdaParameter);

            var mapParams = ImmutableDictionary<ParameterExpression, ParameterExpression>.Empty;

            // add changed parameters to the dictionary
            if (newParams != lambda.Parameters)
            {
                for (var i = 0; i < lambda.Parameters.Count; i++)
                {
                    if (lambda.Parameters[i] != newParams[i])
                    {
                        mapParams = mapParams.Add(lambda.Parameters[i], newParams[i]);
                    }
                }
            }

            var visitor = new ObservableRewriter(new ModuleScope(), mapParams, ImmutableDictionary<MemberInfo, ParameterExpression>.Empty);

            var expr = visitor.Visit(lambda.Body);

            // if nothing changed return the original expression
            if (newParams == lambda.Parameters && expr == lambda.Body)
            {
                return lambda;
            }

            var type = lambda.Type.GetGenericTypeDefinition().MakeGenericType(newParams.Select(x => x.Type).Concat(new[] { expr.Type }).ToArray());

            return Expression.Lambda(type, expr, newParams);
        }

        protected override Expression VisitNew(NewExpression newexpr)
        {
            if (newexpr.Arguments.Count == 0)
            {
                return newexpr;
            }

            if (newexpr.Members == null)
            {
                throw new Exception("Only anonymous types allowed.");
            }

            var args = Visit(newexpr.Arguments, this.Visit);

            if (args == newexpr.Arguments)
            {
                return newexpr;
            }

            return this.CreateProxy(args, newexpr.Members.Select(x => x.Name).ToList());
        }

        protected override Expression VisitMemberInit(MemberInitExpression init)
        {
            if (init.Bindings.Any(x => x.BindingType != MemberBindingType.Assignment))
            {
                throw new NotImplementedException();
            }

            if (init.NewExpression.Arguments.Count > 0)
            {
                throw new NotImplementedException();
            }

            var initBindings = init.Bindings.Cast<MemberAssignment>();

            var list = initBindings.Select(ma => this.Visit(ma.Expression)).ToList();

            // if all expression the same return the original expression
            if (list.Zip(initBindings, (x, ma) => x == ma.Expression).All(x => x))
            {
                return init;
            }

            // if the visited expression can be assign to original type
            if (list.Zip(initBindings, (x, ma) => ((PropertyInfo)ma.Member).PropertyType.IsAssignableFrom(x.Type)).All(x => x))
            {
                return Expression.MemberInit(init.NewExpression, list.Zip(initBindings, (x, ma) => Expression.Bind(ma.Member, x)));
            }

            return this.CreateProxy(list, initBindings.Select(x => x.Member.Name).ToList());
        }

        protected override Expression VisitParameter(ParameterExpression parameter)
        {
            ParameterExpression newParam;
            return this.parameterMap.TryGetValue(parameter, out newParam) ? newParam : parameter;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            var expression = this.Visit(node.Expression);

            if (expression == node.Expression)
            {
                return node;
            }

            ParameterExpression memberExpression;
            if (this.memberMap.TryGetValue(node.Member, out memberExpression))
            {
                return memberExpression;
            }

            // dynamically crated type
            if (expression.Type != node.Type)
            {
                return Expression.PropertyOrField(expression, node.Member.Name);
            }

            return node.Member.MemberType == MemberTypes.Property
                    ? Expression.Property(expression, (PropertyInfo)node.Member)
                    : Expression.Field(expression, (FieldInfo)node.Member);
        }

        protected override Expression VisitMethodCall(MethodCallExpression methodCall)
        {
            if (!methodCall.Method.IsGenericMethod)
            {
                return base.VisitMethodCall(methodCall);
            }

            var genericOriginalMethod = methodCall.Method.GetGenericMethodDefinition();
            var genericOriginalArgs = genericOriginalMethod.GetGenericArguments();
            var genericOriginalParams = genericOriginalMethod.GetParameters();

            // find the possible candidates for the replacement
            MethodInfo[] candidates;
            if (!MethodMap.TryGetValue(genericOriginalMethod, out candidates))
            {
                return base.VisitMethodCall(methodCall);
            }

            var methodArgs = new Type[genericOriginalArgs.Length];

            var exprArgs = new Expression[methodCall.Arguments.Count];

            for (int index = 0; index < methodCall.Arguments.Count; index++)
            {
                var genericArgumentType = genericOriginalParams[index].ParameterType;

                // the parameter is a concrete type like T
                if (Array.IndexOf(genericOriginalArgs, genericArgumentType) >= 0)
                {
                    exprArgs[index] = this.Visit(methodCall.Arguments[index]);

                    var argInd = Array.IndexOf(genericOriginalArgs, genericArgumentType);

                    if (methodArgs[argInd] == null)
                    {
                        methodArgs[argInd] = exprArgs[index].Type;
                    }

                    continue;
                }

                // the parameter is source like IEnumerable<T>
                if (IsEnumerableType(genericArgumentType))
                {
                    exprArgs[index] = this.Visit(methodCall.Arguments[index]);

                    var argInd = Array.IndexOf(genericOriginalArgs, genericArgumentType.GetGenericArguments().Single());

                    if (methodArgs[argInd] == null)
                    {
                        methodArgs[argInd] = (exprArgs[index].Type.FindGenericType(typeof(IObservable<>)) ?? exprArgs[index].Type.FindGenericType(typeof(IEnumerable<>))).GetGenericArguments().Single();
                    }

                    continue;
                }

                if (genericArgumentType.IsGenericType && genericArgumentType.GetGenericTypeDefinition() == typeof(Expression<>))
                {
                    genericArgumentType = genericArgumentType.GetGenericArguments().Single();
                }

                // the parameter is projection like Func<T, TResult>
                if (genericArgumentType.IsGenericType &&
                    (genericArgumentType.GetGenericTypeDefinition() == typeof(Func<,>) ||
                     genericArgumentType.GetGenericTypeDefinition() == typeof(Func<,,>) ||
                     genericArgumentType.GetGenericTypeDefinition() == typeof(Func<,,,>) ||
                     genericArgumentType.GetGenericTypeDefinition() == typeof(Func<,,,,>)))
                {
                    var lambda = methodCall.Arguments[index].NodeType == ExpressionType.Quote
                        ? (LambdaExpression)((UnaryExpression)methodCall.Arguments[index]).Operand
                        : (LambdaExpression)methodCall.Arguments[index];

                    var underlyings = genericArgumentType.GetGenericArguments().Take(lambda.Parameters.Count)
                        .Select(p => IsEnumerableType(p)
                                     ? typeof(IObservable<>).MakeGenericType(methodArgs[Array.IndexOf(genericOriginalArgs, p.GetGenericArguments().Single())])
                                     : methodArgs[Array.IndexOf(genericOriginalArgs, p)])
                        .ToImmutableArray();

                    exprArgs[index] = ObservableUnderlyingRewriter.VisitUnderlyingLambda(this.scope, this.parameterMap, this.memberMap, underlyings, lambda);

                    var returnParam = genericArgumentType.GetGenericArguments().Last();
                    var returnParamArguments = returnParam.GetGenericArguments();

                    // TODO
                    if ((methodCall.Method.Name == "Join" || methodCall.Method.Name == "JoinGroup") && index == 2)
                    {
                        var t = exprArgs[index].Type.GetGenericArguments().Last();

                        var tobs = t.FindGenericType(typeof(IObservable<>));

                        methodArgs[Array.IndexOf(genericOriginalArgs, returnParam)] = tobs != null ? tobs.GetGenericArguments().Single() : t;
                    }

                    // the result type is defined by argument
                    else if (returnParam.IsGenericParameter)
                    {
                        var returnInd = Array.IndexOf(genericOriginalArgs, returnParam);

                        if (methodArgs[returnInd] == null)
                        {
                            methodArgs[returnInd] = exprArgs[index].Type.GetGenericArguments().Last();
                        }
                    }

                    // the type is observable or enumerable
                    else if (returnParamArguments.Length > 0)
                    {
                        var returnInd = Array.IndexOf(genericOriginalArgs, returnParam.GetGenericArguments().Single());

                        if (methodArgs[returnInd] == null)
                        {
                            var t = exprArgs[index].Type.GetGenericArguments().Last();

                            methodArgs[returnInd] = (t.FindGenericType(typeof(IObservable<>)) ?? t.FindGenericType(typeof(IEnumerable<>))).GetGenericArguments().Single();
                        }
                    }

                    continue;
                }

                throw new InvalidOperationException("Unknown type of parameter");
            }

            var method = candidates
                .Select(x => x.MakeGenericMethod(methodArgs))
                .Single(x => x.GetParameters().Zip(exprArgs, (p, e) => p.ParameterType.IsAssignableFrom(e.Type)).All(r => r));

            return Expression.Call(method, exprArgs);
        }

        protected NewExpression CreateProxy(IReadOnlyCollection<Expression> arguments, IReadOnlyList<string> members)
        {
            var emiter = new Castle.DynamicProxy.Generators.Emitters.ClassEmitter(
                this.scope, "DynamicProxy_1", typeof(DynamicProxy), Enumerable.Empty<Type>());

            var fields = arguments.Select((a, i) =>
                emiter.CreateField("__" + members[i], a.Type, FieldAttributes.Public | FieldAttributes.InitOnly)).ToArray();

            var args = fields.Select(field => new ArgumentReference(field.Reference.FieldType)).ToArray();

            var constructor = emiter.CreateConstructor(args);

            for (var i = 0; i < fields.Length; i++)
            {
                constructor.CodeBuilder.AddStatement(new AssignStatement(fields[i], args[i].ToExpression()));

                var property = emiter.CreateProperty(members[i], PropertyAttributes.None, fields[i].Reference.FieldType, Type.EmptyTypes);

                // TODO: get rid of the reflection
                var methodEmitterCnst = typeof(MethodEmitter).GetConstructor(
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new Type[] { typeof(AbstractTypeEmitter), typeof(string), typeof(MethodAttributes), typeof(Type), typeof(Type[]) },
                    null);

                var getter = (MethodEmitter)methodEmitterCnst.Invoke(new object[] { emiter, "get_" + members[i], MethodAttributes.Public, fields[i].Reference.FieldType, Type.EmptyTypes });

                getter.CodeBuilder.AddStatement(new ReturnStatement(new ReferenceExpression(fields[i])));

                property.GetType().GetField("getMethod", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(property, getter);
            }

            var type = emiter.BuildType();

            return Expression.New(type.GetConstructors().Single(), arguments, members.Select(name => type.GetProperty(name)));
        }

        private static ParameterExpression VisitLambdaParameter(ParameterExpression parameter)
        {
            var type = VisitType(parameter.Type);

            if (type == parameter.Type)
            {
                return parameter;
            }

            return Expression.Parameter(type, parameter.Name);
        }

        private static Type VisitType(Type type)
        {
            if (type.IsGenericType)
            {
                var args = type.GetGenericArguments().Select(VisitType);

                if (type.GetGenericTypeDefinition() == typeof(IGrouping<,>))
                {
                    return typeof(IGroupedObservable<,>).MakeGenericType(args.ToArray());
                }

                if (type.GetGenericTypeDefinition() == typeof(IEnumerable<>) || type.GetGenericTypeDefinition() == typeof(IQueryable<>))
                {
                    return typeof(IObservable<>).MakeGenericType(args.ToArray());
                }
            }

            return type;
        }

        private static bool IsEnumerableType(Type type)
        {
            return type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IEnumerable<>) || type.GetGenericTypeDefinition() == typeof(IQueryable<>));
        }

        private class ObservableUnderlyingRewriter : ObservableRewriter
        {
            public ObservableUnderlyingRewriter(ModuleScope scope, ImmutableDictionary<ParameterExpression, ParameterExpression> parameterMap, ImmutableDictionary<MemberInfo, ParameterExpression> memberMap)
                : base(scope, parameterMap, memberMap)
            {
            }

            public static Expression VisitUnderlyingLambda(ModuleScope scope, ImmutableDictionary<ParameterExpression, ParameterExpression> parameterMap, ImmutableDictionary<MemberInfo, ParameterExpression> memberMap, ImmutableArray<Type> parameterTypes, LambdaExpression lambda)
            {
                var parameters = new List<ParameterExpression>();

                var observableParameters = new Dictionary<ParameterExpression, Expression>();

                for (int index = 0; index < lambda.Parameters.Count; index++)
                {
                    var parameter = lambda.Parameters[index];

                    if (parameter.Type == parameterTypes[index])
                    {
                        parameters.Add(parameter);
                        continue;
                    }

                    var foundObs = parameterTypes[index].FindGenericType(typeof(IObservable<>));

                    if (foundObs != null)
                    {
                        var foundEnum = parameter.Type.FindGenericType(typeof(IEnumerable<>)) ?? parameter.Type.FindGenericType(typeof(IQueryable<>));

                        // original parameter was enumerable
                        if (foundEnum != null)
                        {
                            if (foundObs.GetGenericArguments()[0] != foundEnum.GetGenericArguments()[0])
                            {
                                throw new InvalidOperationException();
                            }

                            var param = Expression.Parameter(parameterTypes[index], parameter.Name);

                            parameters.Add(param);
                            parameterMap = parameterMap.Add(parameter, param);

                            continue;
                        }

                        // original parameter was an element
                        else
                        {
                            if (foundObs.GetGenericArguments()[0] != parameter.Type)
                            {
                                throw new InvalidOperationException();
                            }

                            var obsparam = Expression.Parameter(parameterTypes[index], parameter.Name);
                            parameters.Add(obsparam);

                            var param = Expression.Parameter(parameter.Type, "__" + parameter.Name);

                            parameterMap = parameterMap.Add(parameter, param);
                            observableParameters.Add(param, obsparam);

                            continue;
                        }
                    }

                    // dynamic parameter
                    else
                    {
                        var param = Expression.Parameter(parameterTypes[index], parameter.Name);
                        parameters.Add(param);
                        parameterMap = parameterMap.Add(parameter, param);

                        // create parameter for every property changed from concrete type to observable
                        foreach (var prop in parameterTypes[index].GetProperties().Select(p => new { member = p, obs = p.PropertyType.FindGenericType(typeof(IObservable<>)) })
                            .Where(x => x.obs != null))
                        {
                            var originalMember = parameter.Type.GetProperty(prop.member.Name);

                            // avoid if the original property was enumerable
                            if (!IsEnumerableType(originalMember.PropertyType))
                            {
                                var memberparam = Expression.Parameter(originalMember.PropertyType, "__" + parameter.Name + "_" + prop.member.Name);

                                memberMap = memberMap.Add(originalMember, memberparam);
                                observableParameters.Add(memberparam, Expression.Property(param, prop.member));
                            }
                        }

                        continue;
                    }
                }

                var rewriter = new ObservableUnderlyingRewriter(scope, parameterMap, memberMap);

                var body = rewriter.Visit(lambda.Body);

                if (observableParameters.Count == 0)
                {
                    return Expression.Lambda(body, parameters);
                }

                // try to simplify the body
                if (body.NodeType == ExpressionType.Parameter)
                {
                    Expression expr;
                    if (observableParameters.TryGetValue((ParameterExpression)body, out expr))
                    {
                        return Expression.Lambda(expr, parameters);
                    }
                }

                if (observableParameters.Count == 1)
                {
                    var select = new Func<IObservable<object>, Func<object, object>, IObservable<object>>(Observable.Select).Method.GetGenericMethodDefinition();

                    var tp = observableParameters.Single();

                    var call = Expression.Call(select.MakeGenericMethod(tp.Key.Type, body.Type), tp.Value, Expression.Lambda(body, tp.Key));

                    return Expression.Lambda(call, parameters);
                }

                var combineLatest = typeof(Observable).GetMethods().Single(x => x.Name == "CombineLatest" && x.GetParameters().Length == observableParameters.Count + 1).GetGenericMethodDefinition();

                var combineLatestCall = Expression.Call(
                    combineLatest.MakeGenericMethod(observableParameters.Keys.Select(x => x.Type).Concat(new[] { body.Type }).ToArray()),
                    observableParameters.Values.Concat(new[] { Expression.Lambda(body, observableParameters.Keys) }));

                return Expression.Lambda(combineLatestCall, parameters);
            }
        }
    }
}