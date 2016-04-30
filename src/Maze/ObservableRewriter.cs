using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reflection;
using Castle.DynamicProxy;
using Castle.DynamicProxy.Generators.Emitters;
using Maze.Reactive;

namespace Maze
{
    using ArgumentReference = Castle.DynamicProxy.Generators.Emitters.SimpleAST.ArgumentReference;
    using AssignStatement = Castle.DynamicProxy.Generators.Emitters.SimpleAST.AssignStatement;
    using ReferenceExpression = Castle.DynamicProxy.Generators.Emitters.SimpleAST.ReferenceExpression;
    using ReturnStatement = Castle.DynamicProxy.Generators.Emitters.SimpleAST.ReturnStatement;

    public class ObservableRewriter : BaseObservableRewriter
    {
        public ObservableRewriter()
            : base(new ModuleScope(), ImmutableDictionary<ParameterExpression, ParameterExpression>.Empty)
        {
        }

        private ObservableRewriter(ModuleScope scope, ImmutableDictionary<ParameterExpression, ParameterExpression> map)
            : base(scope, map)
        {
        }

        protected override Expression VisitLambda(LambdaExpression lambda)
        {
            var newParams = VisitCollection(lambda.Parameters, this.VisitLambdaParameter);

            var mapParams = this.Map;

            if (newParams != lambda.Parameters)
            {
                for (var i = 0; i < lambda.Parameters.Count; i++)
                {
                    mapParams = mapParams.Add(lambda.Parameters[i], newParams[i]);
                }
            }

            var visitor = new ObservableRewriter(this.Scope, mapParams);

            var expr = visitor.Visit(lambda.Body);

            if (newParams == lambda.Parameters && expr == lambda.Body)
            {
                return lambda;
            }

            // TODO: review this implementation
            var returnType = lambda.Body.Type == lambda.ReturnType ? expr.Type : this.VisitType(lambda.ReturnType);

            var type = lambda.Type.GetGenericTypeDefinition().MakeGenericType(newParams.Select(x => x.Type).Concat(new[] { returnType }).ToArray());

            return Expression.Lambda(type, expr, newParams);
        }

        protected virtual ParameterExpression VisitLambdaParameter(ParameterExpression parameter)
        {
            var type = this.VisitType(parameter.Type);

            if (type == parameter.Type)
            {
                return parameter;
            }

            return Expression.Parameter(type, parameter.Name);
        }

        protected virtual Type VisitType(Type type)
        {
            if (type.IsGenericType)
            {
                var args = VisitCollection(type.GetGenericArguments(), this.VisitType);

                if (type.GetGenericTypeDefinition() == typeof(IGrouping<,>))
                {
                    return typeof(IGroupedObservable<,>).MakeGenericType(args.ToArray());
                }

                if (type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return typeof(IObservable<>).MakeGenericType(args.ToArray());
                }
            }

            return type;
        }
    }

    public class BaseObservableRewriter : ExpressionVisitor2
    {
        private static readonly IDictionary<MethodInfo, MethodInfo[]> MethodMap = new Dictionary<MethodInfo, MethodInfo[]>
        {
            {
                new Func<IEnumerable<object>, Func<object, object>, IEnumerable<object>>(Enumerable.Select<object, object>).Method.GetGenericMethodDefinition(),
                new[] { new Func<IObservable<object>, Func<object, object>, IObservable<object>>(Observable.Select<object, object>).Method.GetGenericMethodDefinition() }
            },
            {
                new Func<IEnumerable<object>, Func<object, bool>, IEnumerable<object>>(Enumerable.Where<object>).Method.GetGenericMethodDefinition(),
                new[]
                {
                    new Func<IObservable<object>, Func<object, bool>, IObservable<object>>(Observable.Where<object>).Method.GetGenericMethodDefinition(),
                    new Func<IObservable<object>, Func<object, IObservable<bool>>, IObservable<object>>(ObservableMaze.Where<object>).Method.GetGenericMethodDefinition(),
                }
            },
            {
                new Func<IEnumerable<object>, object>(Enumerable.First<object>).Method.GetGenericMethodDefinition(),
                new[] { new Func<IObservable<object>, IObservable<object>>(Observable.FirstAsync<object>).Method.GetGenericMethodDefinition() }
            },
            {
                new Func<IEnumerable<object>, object>(Enumerable.Last<object>).Method.GetGenericMethodDefinition(),
                new[] { new Func<IObservable<object>, IObservable<object>>(Observable.LastAsync<object>).Method.GetGenericMethodDefinition() }
            },
            {
                new Func<IEnumerable<object>, int>(Enumerable.Count<object>).Method.GetGenericMethodDefinition(),
                new[] { new Func<IObservable<object>, IObservable<int>>(Observable.Count<object>).Method.GetGenericMethodDefinition() }
            },
            {
                new Func<IEnumerable<object>, object>(Enumerable.Max<object>).Method.GetGenericMethodDefinition(),
                new[] { new Func<IObservable<object>, IObservable<object>>(Observable.Max<object>).Method.GetGenericMethodDefinition() }
            },
            {
                new Func<IEnumerable<object>, object>(Enumerable.Min<object>).Method.GetGenericMethodDefinition(),
                new[] { new Func<IObservable<object>, IObservable<object>>(Observable.Min<object>).Method.GetGenericMethodDefinition() }
            },
            {
                new Func<IEnumerable<object>, object, bool>(Enumerable.Contains<object>).Method.GetGenericMethodDefinition(),
                new[] { new Func<IObservable<object>, object, IObservable<bool>>(Observable.Contains<object>).Method.GetGenericMethodDefinition() }
            },
            {
                new Func<IEnumerable<object>, IEnumerable<object>, Func<object, object>, Func<object, object>, Func<object, object, object>, IEnumerable<object>>(Enumerable.Join<object, object, object, object>).Method.GetGenericMethodDefinition(),
                new[] { new Func<IObservable<object>, IObservable<object>, Func<object, object>, Func<object, object>, Func<object, object, object>, IObservable<object>>(ObservableMaze.Join<object, object, object, object>).Method.GetGenericMethodDefinition() }
            },
            {
                new Func<IEnumerable<object>, IEnumerable<object>, Func<object, object>, Func<object, object>, Func<object, IEnumerable<object>, object>, IEnumerable<object>>(Enumerable.GroupJoin<object, object, object, object>).Method.GetGenericMethodDefinition(),
                new[] { new Func<IObservable<object>, IObservable<object>, Func<object, object>, Func<object, object>, Func<object, IObservable<object>, object>, IObservable<object>>(ObservableMaze.GroupJoin<object, object, object, object>).Method.GetGenericMethodDefinition() }
            },
            {
                new Func<IEnumerable<object>, Func<object, object>, IEnumerable<IGrouping<object, object>>>(Enumerable.GroupBy<object, object>).Method.GetGenericMethodDefinition(),
                new[] { new Func<IObservable<object>, Func<object, object>, IObservable<IGroupedObservable<object, object>>>(Observable.GroupBy<object, object>).Method.GetGenericMethodDefinition() }
            },
        };

        private readonly ImmutableDictionary<ParameterExpression, ParameterExpression> map;

        private readonly ModuleScope scope;

        public BaseObservableRewriter(ModuleScope scope, ImmutableDictionary<ParameterExpression, ParameterExpression> map)
        {
            this.map = map;
            this.scope = scope;
        }

        protected ImmutableDictionary<ParameterExpression, ParameterExpression> Map
        {
            get { return this.map; }
        }

        protected ModuleScope Scope
        {
            get { return this.scope; }
        }

        protected override Expression VisitParameter(ParameterExpression parameter)
        {
            ParameterExpression newParam;
            return this.Map.TryGetValue(parameter, out newParam) ? newParam : parameter;
        }

        protected override Expression VisitMethodCall(MethodCallExpression methodCall)
        {
            if (!methodCall.Method.IsGenericMethod || !MethodMap.ContainsKey(methodCall.Method.GetGenericMethodDefinition()))
            {
                return base.VisitMethodCall(methodCall);
            }

            var genericOriginalMethod = methodCall.Method.GetGenericMethodDefinition();
            var genericOriginalArgs = genericOriginalMethod.GetGenericArguments();
            var genericOriginalParams = genericOriginalMethod.GetParameters();

            var candidates = MethodMap[genericOriginalMethod];

            var methodArgs = new Type[genericOriginalArgs.Length];

            var exprArgs = new Expression[methodCall.Arguments.Count];

            for (int index = 0; index < methodCall.Arguments.Count; index++)
            {
                var type = genericOriginalParams[index].ParameterType;

                if (Array.IndexOf(genericOriginalArgs, type) >= 0)
                {
                    exprArgs[index] = this.Visit(methodCall.Arguments[index]);

                    var argInd = Array.IndexOf(genericOriginalArgs, type);

                    if (methodArgs[argInd] == null)
                    {
                        methodArgs[argInd] = exprArgs[index].Type;
                    }
                }
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    exprArgs[index] = this.Visit(methodCall.Arguments[index]);

                    var argInd = Array.IndexOf(genericOriginalArgs, type.GetGenericArguments()[0]);

                    if (methodArgs[argInd] == null)
                    {
                        methodArgs[argInd] = exprArgs[index].Type.FindGenericType(typeof(IObservable<>)).GetGenericArguments()[0];
                    }
                }
                else if (type.IsGenericType &&
                    (type.GetGenericTypeDefinition() == typeof(Func<,>) ||
                     type.GetGenericTypeDefinition() == typeof(Func<,,>) ||
                     type.GetGenericTypeDefinition() == typeof(Func<,,,>) ||
                     type.GetGenericTypeDefinition() == typeof(Func<,,,,>)))
                {
                    var lambda = (LambdaExpression)methodCall.Arguments[index];

                    var candidateParam = candidates.First().MakeGenericMethod(methodArgs.Select(x => x ?? typeof(object)).ToArray()).GetParameters()[index];

                    var underlyings = candidateParam.ParameterType.GetGenericArguments().Take(lambda.Parameters.Count).ToArray();

                    if (underlyings.Zip(lambda.Type.GetGenericArguments(), (x1, x2) => x1 == x2).All(x => x))
                    {
                        exprArgs[index] = this.Visit(lambda);
                    }
                    else
                    {
                        var rewriter = new ObservableMemberRewriter2(this.Scope, this.Map, underlyings);

                        exprArgs[index] = rewriter.Visit(lambda);
                    }

                    var returnParams = type.GetGenericArguments().Last();
                    if (returnParams.IsGenericParameter)
                    {
                        var returnInd = Array.IndexOf(genericOriginalArgs, returnParams);

                        if (methodArgs[returnInd] == null)
                        {
                            methodArgs[returnInd] = exprArgs[index].Type.GetGenericArguments().Last();
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException("Unknown type of parameter");
                }
            }

            var method = candidates
                .Select(x => x.MakeGenericMethod(methodArgs))
                .Single(x => x.GetParameters().Zip(exprArgs, (p, e) => p.ParameterType.IsAssignableFrom(e.Type)).All(r => r));

            return Expression.Call(method, exprArgs);
        }

        protected override NewExpression VisitNew(NewExpression newexpr)
        {
            if (newexpr.Arguments.Count == 0)
            {
                return newexpr;
            }

            if (newexpr.Members == null)
            {
                throw new Exception("Only anonymous types allowed.");
            }

            var args = VisitCollection(newexpr.Arguments, this.Visit);

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

            var list = new List<Expression>();

            foreach (MemberAssignment member in init.Bindings)
            {
                list.Add(this.Visit(member.Expression));
            }

            if (list.Select((x, i) => ((MemberAssignment)init.Bindings[i]).Expression == x).All(x => x))
            {
                return init;
            }

            return this.CreateProxy(list, init.Bindings.Cast<MemberAssignment>().Select(x => x.Member.Name).ToList());
        }

        protected override Expression VisitNewArray(NewArrayExpression newArray)
        {
            var array = base.VisitNewArray(newArray);

            var method = new Func<IEnumerable<object>, IObservable<object>>(Observable.ToObservable<object>).Method.GetGenericMethodDefinition();

            return Expression.Call(method.MakeGenericMethod(array.Type.GetElementType()), array);
        }

        protected override Expression VisitMemberAccess(MemberExpression node)
        {
            var expression = this.Visit(node.Expression);

            if (node.Expression == expression)
            {
                return node;
            }

            if (node.Expression.Type != expression.Type)
            {
                var member = expression.Type.GetMember(node.Member.Name).Single();

                return Expression.MakeMemberAccess(expression, member);
            }

            return node.Update(expression);
        }

        protected NewExpression CreateProxy(IReadOnlyCollection<Expression> arguments, IReadOnlyList<string> members)
        {
            var emiter = new ClassEmitter(this.Scope, "DynamicProxy_1", typeof(DynamicProxy), Enumerable.Empty<Type>());

            var fields = arguments.Select((a, i) => emiter.CreateField("__" + members[i], a.Type, FieldAttributes.Public | FieldAttributes.InitOnly)).ToArray();

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
    }

    public class ObservableMemberRewriter2 : BaseObservableRewriter
    {
        private readonly Type[] parameterTypes;

        public ObservableMemberRewriter2(ModuleScope scope, ImmutableDictionary<ParameterExpression, ParameterExpression> map, Type[] parameterTypes)
            : base(scope, map)
        {
            this.parameterTypes = parameterTypes;
        }

        protected override Expression VisitLambda(LambdaExpression lambda)
        {
            var newMap = this.Map;

            var arguments = new Dictionary<Expression, ParameterExpression>();

            var members = new Dictionary<Expression, MemberOverride>();
            var obsParams = new Dictionary<ParameterExpression, ParameterOverride>();

            var list = new List<ParameterExpression>();

            for (int index = 0; index < lambda.Parameters.Count; index++)
            {
                var parameter = lambda.Parameters[index];

                var foundObs = this.parameterTypes[index].FindGenericType(typeof(IObservable<>));

                if (foundObs != null)
                {
                    var foundEnum = parameter.Type.FindGenericType(typeof(IEnumerable<>));

                    if (foundEnum != null)
                    {
                        if (foundObs.GetGenericArguments()[0] != foundEnum.GetGenericArguments()[0])
                        {
                            throw new InvalidOperationException();
                        }

                        var param = Expression.Parameter(this.parameterTypes[index], parameter.Name);

                        list.Add(param);
                        newMap = newMap.Add(parameter, param);
                    }
                    else
                    {
                        if (foundObs.GetGenericArguments()[0] != parameter.Type)
                        {
                            throw new InvalidOperationException();
                        }

                        var param = Expression.Parameter(this.parameterTypes[index], parameter.Name + "_obs");

                        list.Add(param);
                        obsParams.Add(parameter, new ParameterOverride { ObsParameter = param });
                    }
                }
                else
                {
                    var param = Expression.Parameter(this.parameterTypes[index], parameter.Name);

                    list.Add(param);
                    members.Add(parameter, new MemberOverride { Expression = param, Parameters = new Dictionary<MemberInfo, ParameterExpression>() });
                }
            }

            var rewriter = new Rewriter(this.Scope, newMap, arguments, obsParams, members);

            var body = rewriter.Visit(lambda.Body);

            if (arguments.Count == 0)
            {
                return Expression.Lambda(body, list);
            }

            if (arguments.Count == 1)
            {
                var arg = arguments.Values.Single();

                if (body == arg)
                {
                    return Expression.Lambda(arguments.Keys.Single(), list);
                }

                var methodSelect = new Func<IObservable<object>, Func<object, object>, IObservable<object>>(Observable.Select<object, object>).Method.GetGenericMethodDefinition();

                var call = Expression.Call(methodSelect.MakeGenericMethod(arg.Type, body.Type), arguments.Keys.Single(), Expression.Lambda(body, arg));

                return Expression.Lambda(call, list);
            }

            var method = typeof(Observable).GetMethods().Single(x => x.Name == "CombineLatest" && x.GetGenericArguments().Length == (arguments.Count + 1));

            var combineparams = new List<Expression>();

            var combinelabdaparams = new List<ParameterExpression>();

            foreach (var item in arguments)
            {
                combineparams.Add(item.Key);
                combinelabdaparams.Add(item.Value);
            }

            combineparams.Add(Expression.Lambda(body, combinelabdaparams));

            var combine = Expression.Call(
                method.MakeGenericMethod(combinelabdaparams.Select(x => x.Type).Concat(new[] { body.Type }).ToArray()),
                combineparams);

            return Expression.Lambda(combine, list);
        }

        private class Rewriter : BaseObservableRewriter
        {
            private readonly IDictionary<Expression, ParameterExpression> arguments;

            private readonly IDictionary<ParameterExpression, ParameterOverride> parameters;

            private readonly IDictionary<Expression, MemberOverride> members;

            public Rewriter(
                ModuleScope scope,
                ImmutableDictionary<ParameterExpression, ParameterExpression> map,
                IDictionary<Expression, ParameterExpression> arguments,
                IDictionary<ParameterExpression, ParameterOverride> parameters,
                IDictionary<Expression, MemberOverride> members)
                : base(scope, map)
            {
                this.arguments = arguments;
                this.parameters = parameters;
                this.members = members;
            }

            protected override Expression VisitParameter(ParameterExpression parameter)
            {
                ParameterOverride item;
                if (!this.parameters.TryGetValue(parameter, out item))
                {
                    return base.VisitParameter(parameter);
                }

                if (!item.Used)
                {
                    this.arguments.Add(item.ObsParameter, parameter);
                    item.Used = true;
                }

                return parameter;
            }

            protected override Expression VisitMemberAccess(MemberExpression node)
            {
                MemberOverride item;
                if (!this.members.TryGetValue(node.Expression, out item))
                {
                    return base.VisitMemberAccess(node);
                }

                var member = item.Expression.Type.GetMember(node.Member.Name).Single();

                var expr = Expression.MakeMemberAccess(item.Expression, member);

                var observableType = expr.Type.FindGenericType(typeof(IObservable<>));
                if (observableType == null)
                {
                    return expr;
                }

                var underlying = observableType.GetGenericArguments()[0];

                ParameterExpression result;
                if (item.Parameters.TryGetValue(member, out result))
                {
                    return result;
                }

                string name = null;

                if (node.Expression is ParameterExpression)
                {
                    name = ((ParameterExpression)node.Expression).Name + "_" + member.Name.ToLower();
                }

                result = name != null ? Expression.Parameter(underlying, name) : Expression.Parameter(underlying);

                this.arguments.Add(expr, result);

                item.Parameters.Add(member, result);

                return result;
            }
        }

        private class ParameterOverride
        {
            public ParameterExpression ObsParameter { get; set; }

            public bool Used { get; set; }
        }

        private class MemberOverride
        {
            public ParameterExpression Expression { get; set; }

            public IDictionary<MemberInfo, ParameterExpression> Parameters { get; set; }
        }
    }
}
