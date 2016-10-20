using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reflection;
using Maze.Reactive;
using Observable = System.Reactive.Linq.Observable;
using ObservableMaze = Maze.Reactive.Observable;

namespace Maze
{
    public class ObservableRewriter : ExpressionVisitor
    {
        private static readonly IDictionary<MethodInfo, MethodInfo[]> MethodMap = new Dictionary<MethodInfo, MethodInfo[]>
        {
            [new Func<IEnumerable<object>, Func<object, object>, IEnumerable<object>>(Enumerable.Select).GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, Func<object, object>, IObservable<object>>(Observable.Select).GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, Func<object, object>, IEnumerable<object>>(Enumerable.Select).GetGenericMethodDefinition()
            },

            [new Func<IEnumerable<object>, Func<object, int, object>, IEnumerable<object>>(Enumerable.Select).GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, Func<object, int, object>, IObservable<object>>(Observable.Select).GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, Func<object, int, object>, IEnumerable<object>>(Enumerable.Select).GetGenericMethodDefinition()
            },

            [new Func<IQueryable<object>, Expression<Func<object, object>>, IQueryable<object>>(Queryable.Select).GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, Func<object, object>, IObservable<object>>(Observable.Select).GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, Func<object, object>, IEnumerable<object>>(Enumerable.Select).GetGenericMethodDefinition()
            },

            [new Func<IQueryable<object>, Expression<Func<object, int, object>>, IQueryable<object>>(Queryable.Select).GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, Func<object, int, object>, IObservable<object>>(Observable.Select).GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, Func<object, int, object>, IEnumerable<object>>(Enumerable.Select).GetGenericMethodDefinition()
            },

            [new Func<IEnumerable<object>, Func<object, IEnumerable<object>>, IEnumerable<object>>(Enumerable.SelectMany).GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, Func<object, IObservable<object>>, IObservable<object>>(Observable.SelectMany).GetGenericMethodDefinition(),
                new Func<IObservable<object>, Func<object, IEnumerable<object>>, IObservable<object>>(Observable.SelectMany).GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, Func<object, IEnumerable<object>>, IEnumerable<object>>(Enumerable.SelectMany).GetGenericMethodDefinition()
            },

            [new Func<IEnumerable<object>, Func<object, IEnumerable<object>>, Func<object, object, object>, IEnumerable<object>>(Enumerable.SelectMany).GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, Func<object, IObservable<object>>, Func<object, object, object>, IObservable<object>>(Observable.SelectMany).GetGenericMethodDefinition(),
                new Func<IObservable<object>, Func<object, IEnumerable<object>>, Func<object, object, object>, IObservable<object>>(Observable.SelectMany).GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, Func<object, IEnumerable<object>>, Func<object, object, object>, IEnumerable<object>>(Enumerable.SelectMany).GetGenericMethodDefinition()
            },

            [new Func<IQueryable<object>, Expression<Func<object, IEnumerable<object>>>, IQueryable<object>>(Queryable.SelectMany).GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, Func<object, IObservable<object>>, IObservable<object>>(Observable.SelectMany).GetGenericMethodDefinition(),
                new Func<IObservable<object>, Func<object, IEnumerable<object>>, IObservable<object>>(Observable.SelectMany).GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, Func<object, IEnumerable<object>>, IEnumerable<object>>(Enumerable.SelectMany).GetGenericMethodDefinition()
            },

            [new Func<IQueryable<object>, Expression<Func<object, IEnumerable<object>>>, Expression<Func<object, object, object>>, IQueryable<object>>(Queryable.SelectMany).GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, Func<object, IObservable<object>>, Func<object, object, object>, IObservable<object>>(Observable.SelectMany).GetGenericMethodDefinition(),
                new Func<IObservable<object>, Func<object, IEnumerable<object>>, Func<object, object, object>, IObservable<object>>(Observable.SelectMany).GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, Func<object, IEnumerable<object>>, Func<object, object, object>, IEnumerable<object>>(Enumerable.SelectMany).GetGenericMethodDefinition()
            },

            [new Func<IEnumerable<object>, Func<object, bool>, IEnumerable<object>>(Enumerable.Where).GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, Func<object, bool>, IObservable<object>>(Observable.Where).GetGenericMethodDefinition(),
                new Func<IObservable<object>, Func<object, IObservable<bool>>, IObservable<object>>(ObservableMaze.Where).GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, Func<object, bool>, IEnumerable<object>>(Enumerable.Where).GetGenericMethodDefinition()
            },

            [new Func<IQueryable<object>, Expression<Func<object, bool>>, IQueryable<object>>(Queryable.Where).GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, Func<object, bool>, IObservable<object>>(Observable.Where).GetGenericMethodDefinition(),
                new Func<IObservable<object>, Func<object, IObservable<bool>>, IObservable<object>>(ObservableMaze.Where).GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, Func<object, bool>, IEnumerable<object>>(Enumerable.Where).GetGenericMethodDefinition()
            },

            [new Func<IEnumerable<object>, object>(Enumerable.First).GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>>(Observable.FirstAsync).GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, object>(Enumerable.First).GetGenericMethodDefinition(),
            },

            [new Func<IEnumerable<object>, object>(Enumerable.FirstOrDefault).GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>>(Observable.FirstOrDefaultAsync).GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, object>(Enumerable.FirstOrDefault).GetGenericMethodDefinition(),
            },

            [new Func<IEnumerable<object>, object>(Enumerable.Last).GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>>(Observable.LastAsync).GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, object>(Enumerable.Last).GetGenericMethodDefinition(),
            },

            [new Func<IEnumerable<object>, object>(Enumerable.LastOrDefault).GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>>(Observable.LastOrDefaultAsync).GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, object>(Enumerable.LastOrDefault).GetGenericMethodDefinition(),
            },

            [new Func<IEnumerable<object>, object>(Enumerable.Single).GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>>(Observable.SingleAsync).GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, object>(Enumerable.Single).GetGenericMethodDefinition(),
            },

            [new Func<IEnumerable<object>, object>(Enumerable.SingleOrDefault).GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>>(Observable.SingleOrDefaultAsync).GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, object>(Enumerable.SingleOrDefault).GetGenericMethodDefinition(),
            },

            [new Func<IEnumerable<object>, int>(Enumerable.Count).GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<int>>(Observable.Count).GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, int>(Enumerable.Count).GetGenericMethodDefinition(),
            },

            [new Func<IEnumerable<object>, object>(Enumerable.Max).GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>>(Observable.Max).GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, object>(Enumerable.Max).GetGenericMethodDefinition()
            },

            [new Func<IEnumerable<object>, object>(Enumerable.Min).GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>>(Observable.Min).GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, object>(Enumerable.Min).GetGenericMethodDefinition()
            },

            [new Func<IEnumerable<object>, object, bool>(Enumerable.Contains).GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, object, IObservable<bool>>(Observable.Contains).GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, object, bool>(Enumerable.Contains).GetGenericMethodDefinition(),
            },

            [new Func<IEnumerable<object>, IEnumerable<object>, Func<object, object>, Func<object, object>, Func<object, object, object>, IEnumerable<object>>(Enumerable.Join).GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>, Func<object, object>, Func<object, object>, Func<object, object, object>, IObservable<object>>(ObservableMaze.Join).GetGenericMethodDefinition(),
                new Func<IObservable<object>, IObservable<object>, Func<object, IObservable<object>>, Func<object, IObservable<object>>, Func<object, object, object>, IObservable<object>>(ObservableMaze.Join).GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, IEnumerable<object>, Func<object, object>, Func<object, object>, Func<object, object, object>, IEnumerable<object>>(Enumerable.Join).GetGenericMethodDefinition()
            },

            [new Func<IQueryable<object>, IEnumerable<object>, Expression<Func<object, object>>, Expression<Func<object, object>>, Expression<Func<object, object, object>>, IQueryable<object>>(Queryable.Join).GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>, Func<object, object>, Func<object, object>, Func<object, object, object>, IObservable<object>>(ObservableMaze.Join).GetGenericMethodDefinition(),
                new Func<IObservable<object>, IObservable<object>, Func<object, IObservable<object>>, Func<object, IObservable<object>>, Func<object, object, object>, IObservable<object>>(ObservableMaze.Join).GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, IEnumerable<object>, Func<object, object>, Func<object, object>, Func<object, object, object>, IEnumerable<object>>(Enumerable.Join).GetGenericMethodDefinition()
            },

            [new Func<IEnumerable<object>, IEnumerable<object>, Func<object, object>, Func<object, object>, Func<object, IEnumerable<object>, object>, IEnumerable<object>>(Enumerable.GroupJoin).GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>, Func<object, object>, Func<object, object>, Func<object, IObservable<object>, object>, IObservable<object>>(ObservableMaze.GroupJoin).GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, IEnumerable<object>, Func<object, object>, Func<object, object>, Func<object, IEnumerable<object>, object>, IEnumerable<object>>(Enumerable.GroupJoin).GetGenericMethodDefinition()
            },

            [new Func<IQueryable<object>, IEnumerable<object>, Expression<Func<object, object>>, Expression<Func<object, object>>, Expression<Func<object, IEnumerable<object>, object>>, IQueryable<object>>(Queryable.GroupJoin).GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>, Func<object, object>, Func<object, object>, Func<object, IObservable<object>, object>, IObservable<object>>(ObservableMaze.GroupJoin).GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, IEnumerable<object>, Func<object, object>, Func<object, object>, Func<object, IEnumerable<object>, object>, IEnumerable<object>>(Enumerable.GroupJoin).GetGenericMethodDefinition()
            },

            [new Func<IEnumerable<object>, Func<object, object>, IEnumerable<IGrouping<object, object>>>(Enumerable.GroupBy).GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, Func<object, object>, IObservable<IGroupedObservable<object, object>>>(Observable.GroupBy).GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, Func<object, object>, IEnumerable<IGrouping<object, object>>>(Enumerable.GroupBy).GetGenericMethodDefinition()
            },

            [new Func<IEnumerable<object>, IEnumerable<object>, IEnumerable<object>>(Enumerable.Concat).GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>, IObservable<object>>(Observable.Merge).GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, IEnumerable<object>, IEnumerable<object>>(Enumerable.Concat).GetGenericMethodDefinition()
            },

            [new Func<IQueryable<object>, IQueryable<object>, IQueryable<object>>(Queryable.Concat).GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>, IObservable<object>>(Observable.Merge).GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, IEnumerable<object>, IEnumerable<object>>(Enumerable.Concat).GetGenericMethodDefinition()
            },

            [new Func<IQueryable<object>, IQueryable<object>, IQueryable<object>>(Queryable.Union).GetGenericMethodDefinition()] = new[]
            {
                new Func<IObservable<object>, IObservable<object>, IObservable<object>>(Observable.Merge).GetGenericMethodDefinition(),
                new Func<IEnumerable<object>, IEnumerable<object>, IEnumerable<object>>(Enumerable.Union).GetGenericMethodDefinition()
            },
        };

        private readonly TypeFactory typeFactory;

        private readonly ImmutableDictionary<ParameterExpression, ParameterExpression> parameterMap;
        private readonly ImmutableDictionary<MemberInfo, ParameterExpression> memberMap;

        public ObservableRewriter()
            : this(new TypeFactory(), ImmutableDictionary<ParameterExpression, ParameterExpression>.Empty, ImmutableDictionary<MemberInfo, ParameterExpression>.Empty)
        {
        }

        public ObservableRewriter(ImmutableDictionary<ParameterExpression, ParameterExpression> parameterMap)
            : this(new TypeFactory(), parameterMap, ImmutableDictionary<MemberInfo, ParameterExpression>.Empty)
        {
        }

        protected ObservableRewriter(TypeFactory typeFactory, ImmutableDictionary<ParameterExpression, ParameterExpression> parameterMap, ImmutableDictionary<MemberInfo, ParameterExpression> memberMap)
        {
            this.typeFactory = typeFactory;
            this.parameterMap = parameterMap;
            this.memberMap = memberMap;
        }

        protected TypeFactory TypeFactory
        {
            get { return this.typeFactory; }
        }

        public static ImmutableDictionary<ParameterExpression, ParameterExpression> ChangeParameters(
            System.Collections.ObjectModel.ReadOnlyCollection<ParameterExpression> parameters)
        {
            // Convert the parameter to observable
            var newParams = Visit(parameters, VisitLambdaParameter);

            var builder = ImmutableDictionary.CreateBuilder<ParameterExpression, ParameterExpression>();

            // add changed parameters to the dictionary
            if (newParams != parameters)
            {
                for (var i = 0; i < parameters.Count; i++)
                {
                    if (parameters[i] != newParams[i])
                    {
                        builder.Add(parameters[i], newParams[i]);
                    }
                }
            }

            return builder.ToImmutable();
        }

        public static LambdaExpression ChangeParameters(LambdaExpression lambda)
        {
            var mapParams = ChangeParameters(lambda.Parameters);

            var visitor = new ObservableRewriter(new TypeFactory(), mapParams, ImmutableDictionary<MemberInfo, ParameterExpression>.Empty);

            return (LambdaExpression)visitor.Visit(lambda);
        }

        protected override Expression VisitLambda<T>(Expression<T> lambda)
        {
            var parameters = Visit(lambda.Parameters, x => this.parameterMap.GetValueOrDefault(x, x));

            var body = this.Visit(lambda.Body);

            // if nothing changed return the original expression
            if (parameters == lambda.Parameters && body == lambda.Body)
            {
                return lambda;
            }

            var type = lambda.Type.GetGenericTypeDefinition().MakeGenericType(parameters.Select(x => x.Type).Concat(new[] { body.Type }).ToArray());

            return Expression.Lambda(type, body, parameters);
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

            return this.typeFactory.CreateProxy(args, newexpr.Members.Select(x => x.Name).ToList());
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

            return this.typeFactory.CreateProxy(list, initBindings.Select(x => x.Member.Name).ToList());
        }

        protected override Expression VisitParameter(ParameterExpression parameter)
        {
            return this.parameterMap.GetValueOrDefault(parameter, parameter);
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

            if (node.Member is PropertyInfo)
            {
                return Expression.Property(expression, (PropertyInfo)node.Member);
            }

            if (node.Member is FieldInfo)
            {
                return Expression.Field(expression, (FieldInfo)node.Member);
            }

            throw new InvalidOperationException("Unknown member type: " + node.Member.GetType().Name);
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

                if (genericArgumentType.EqualsGenericDefinition(typeof(Expression<>)))
                {
                    genericArgumentType = genericArgumentType.GetGenericArguments().Single();
                }

                // the parameter is projection like Func<T, TResult>
                if (genericArgumentType.EqualsGenericDefinition(typeof(Func<,>)) ||
                    genericArgumentType.EqualsGenericDefinition(typeof(Func<,,>)) ||
                    genericArgumentType.EqualsGenericDefinition(typeof(Func<,,,>)) ||
                    genericArgumentType.EqualsGenericDefinition(typeof(Func<,,,,>)) ||
                    genericArgumentType.EqualsGenericDefinition(typeof(Func<,,,,,>)))
                {
                    var lambda = methodCall.Arguments[index].NodeType == ExpressionType.Quote
                        ? (LambdaExpression)((UnaryExpression)methodCall.Arguments[index]).Operand
                        : (LambdaExpression)methodCall.Arguments[index];

                    var underlyings = genericArgumentType.GetGenericArguments().Take(lambda.Parameters.Count)
                        .Select(p => IsEnumerableType(p)
                                     ? typeof(IObservable<>).MakeGenericType(methodArgs[Array.IndexOf(genericOriginalArgs, p.GetGenericArguments().Single())])
                                     : methodArgs[Array.IndexOf(genericOriginalArgs, p)])
                        .ToImmutableArray();

                    exprArgs[index] = VisitUnderlyingLambda(this, this.parameterMap, this.memberMap, underlyings, lambda);

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

        protected virtual ObservableRewriter CreateNestedRewriter(ImmutableDictionary<ParameterExpression, ParameterExpression> parameterMap, ImmutableDictionary<MemberInfo, ParameterExpression> memberMap)
        {
            return new ObservableRewriter(this.typeFactory, parameterMap, memberMap);
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
            if (type.IsGenericType())
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
            return type.IsGenericType() && (type.GetGenericTypeDefinition() == typeof(IEnumerable<>) || type.GetGenericTypeDefinition() == typeof(IQueryable<>));
        }

        private static Expression VisitUnderlyingLambda(ObservableRewriter current, ImmutableDictionary<ParameterExpression, ParameterExpression> parameterMap, ImmutableDictionary<MemberInfo, ParameterExpression> memberMap, ImmutableArray<Type> parameterTypes, LambdaExpression lambda)
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

            var rewriter = current.CreateNestedRewriter(parameterMap, memberMap);

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
                var select = new Func<IObservable<object>, Func<object, object>, IObservable<object>>(Observable.Select).GetGenericMethodDefinition();

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

    public class MetricObservableRewriter : ObservableRewriter
    {
        private readonly Dictionary<Expression, ObservableTracker> trackers;

        public MetricObservableRewriter()
        {
        }

        public MetricObservableRewriter(
            ImmutableDictionary<ParameterExpression, ParameterExpression> parameterMap,
            params Expression[] tracking)
            : base(parameterMap)
        {
            this.trackers = tracking.ToDictionary(x => x, x => (ObservableTracker)null);
        }

        protected MetricObservableRewriter(
            TypeFactory typeFactory,
            ImmutableDictionary<ParameterExpression, ParameterExpression> parameterMap,
            ImmutableDictionary<MemberInfo, ParameterExpression> memberMap,
            Dictionary<Expression, ObservableTracker> trackers)
            : base (typeFactory, parameterMap, memberMap)
        {
            this.trackers = trackers;
        }

        public Dictionary<Expression, ObservableTracker> Trackers
        {
            get { return this.trackers; }
        }

        protected override Expression VisitParameter(ParameterExpression parameter)
        {
            return this.Attach(parameter, base.VisitParameter(parameter));
        }

        protected override Expression VisitMethodCall(MethodCallExpression methodCall)
        {
            return this.Attach(methodCall, base.VisitMethodCall(methodCall));
        }

        protected override ObservableRewriter CreateNestedRewriter(ImmutableDictionary<ParameterExpression, ParameterExpression> parameterMap, ImmutableDictionary<MemberInfo, ParameterExpression> memberMap)
        {
            return new MetricObservableRewriter(this.TypeFactory, parameterMap, memberMap, this.trackers);
        }

        private Expression Attach(Expression key, Expression expression)
        {
            if (!this.trackers.ContainsKey(key) || this.trackers[key] != null)
            {
                return expression;
            }

            var type = expression.Type.FindGenericType(typeof(IObservable<>)).GenericTypeArguments.Single();

            var tracker = (ObservableTracker)Activator.CreateInstance(typeof(ObservableTracker<>).MakeGenericType(type));

            this.trackers[key] = tracker;

            var trackMethod = TypeExt.GetMethodDefinition(() => ObservableMaze.Track<object>(null, null)).MakeGenericMethod(type);
            var publishMethod = TypeExt.GetMethodDefinition(() => Observable.Publish<object>(null)).MakeGenericMethod(type);
            var refCountMethod = TypeExt.GetMethodDefinition(() => Observable.RefCount<object>(null)).MakeGenericMethod(type);

            var track = Expression.Call(trackMethod, expression, Expression.Constant(tracker));
            var publish = Expression.Call(publishMethod, track);
            var refCount = Expression.Call(refCountMethod, publish);

            return refCount;
        }
    }
}