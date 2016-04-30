using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Maze.Mappings;

namespace Maze
{
    public static class Engine
    {
        public static MappingReference<TElement> Create<TElement>(IEnumerable<TElement> items)
        {
            return ContainerReference.Empty.Add(new EnumerableMapping<TElement>(items));
        }

        public static MappingReference<TElement> Map<TSource, TElement>(Expression<Func<IQueryable<TSource>, IQueryable<TElement>>> map)
        {
            return ContainerReference.Empty.Add(new ExpressionMapping<TElement>(map, ImmutableList.Create<IMapping>(new AnonymousMapping<TSource>())));
        }

        public static ComponentMappingReference<TComponent> CreateComponent<TComponent>()
        {
            var constractors = typeof(TComponent).GetConstructors();

            if (constractors.Length == 0)
            {
                throw new InvalidOperationException("No public contractor found");
            }
            else if (constractors.Length > 1)
            {
                throw new InvalidOperationException("Only one contractor expected");
            }

            var context = new MappingContext();

            var args = constractors[0].GetParameters()
                .Select(x => TypeExt.CallGenericMethod(context.CreateSource<object>, x.Name, x.ParameterType.GetGenericArguments().Single()))
                .ToArray();

            var sourceMappings = args.ToImmutableDictionary(
                x => x.Expression,
                x => (IMapping)Activator.CreateInstance(typeof(AnonymousMapping<>).MakeGenericType(x.ElementType)));

            var instance = CreateMappingFromMappingContext(context, (TComponent)constractors[0].Invoke(args), sourceMappings);

            return ContainerReference.Empty.Add(instance);
        }

        public static ComponentMappingReference<TComponent> CreateComponent<TComponent>(Expression<Func<TComponent>> map)
        {
            return ContainerReference.Empty.Add(CreateMappingFromExpressionComponent<TComponent>(map, ImmutableDictionary<Expression, IMapping>.Empty));
        }

        public static ComponentMappingReference<TComponent> MapComponent<TSource, TComponent>(Expression<Func<IQueryable<TSource>, TComponent>> map)
        {
            var sourceMappings = ImmutableDictionary<Expression, IMapping>.Empty.Add(map.Parameters.Single(), new AnonymousMapping<TSource>());

            return ContainerReference.Empty.Add(CreateMappingFromExpressionComponent<TComponent>(map, sourceMappings));
        }

        public static ContainerReference Combine(params ContainerReference[] mappings)
        {
            switch (mappings.Length)
            {
                case 0:
                    return ContainerReference.Empty;
                case 1:
                    return mappings[0];
            }

            var result = mappings[0];

            for (int i = 1; i < mappings.Length; i++)
            {
                result = result.Add(mappings[i]);
            }

            return result;
        }

        public static IExecutionGraph Build(this ContainerReference mapping)
        {
            var compiler = new SimpleCompiler();

            return compiler.Build(mapping);
        }

        public static IObservable<TElement> Execute<TElement>(this MappingReference<TElement> mapping)
        {
            var compiler = new SimpleCompiler();

            var exec = compiler.Build(mapping);

            var subject = new System.Reactive.Subjects.ReplaySubject<TElement>();

            exec.GetStream(mapping).Subscribe(subject);
            exec.Release();

            return subject;
        }

        public static IObservable<TElement> Execute<TElement, TSource>(this MappingReference<TElement> mapping, params TSource[] source)
        {
            var compiler = new SimpleCompiler();

            var exec = compiler.Build(Combine(Create(source), mapping));

            var subject = new System.Reactive.Subjects.ReplaySubject<TElement>();

            exec.GetStream(mapping).Subscribe(subject);
            exec.Release();

            return subject;
        }

        public static IExecutionGraph Compile(this ContainerReference mapping)
        {
            var compiler = new SimpleCompiler();

            return compiler.Build(mapping);
        }

        public static MappingReference<TElement> Get<TElement>(this ContainerReference mapping)
        {
            var instance = mapping.Mappings.OfType<IMapping<TElement>>().Single();

            return mapping.Wrap(instance);
        }

        public static MappingReference<TElement> Get<TComponent, TElement>(
            this ComponentMappingReference<TComponent> mapping, Expression<Func<TComponent, IQueryable<TElement>>> selector)
        {
            var path = new List<PropertyInfo>();

            Expression item = selector.Body;
            while (item.NodeType == ExpressionType.MemberAccess)
            {
                var memberExpr = (MemberExpression)item;
                if (memberExpr.Member is PropertyInfo)
                {
                    path.Insert(0, (PropertyInfo)memberExpr.Member);
                    item = memberExpr.Expression;
                }
                else
                {
                    break;
                }
            }

            if (path.Count == 0)
            {
                throw new InvalidOperationException();
            }

            var instance = (IMapping<TElement>)mapping.Instance.Mappings[string.Join(".", path.Select(x => x.Name))];

            return mapping.Wrap(instance);
        }

        public static MappingReference<TElement> Map<TSource, TElement>(
            this MappingReference<TSource> mapping, Expression<Func<IQueryable<TSource>, IQueryable<TElement>>> map)
        {
            return mapping.Add(new ExpressionMapping<TElement>(map, ImmutableList.Create<IMapping>(mapping.Instance)));
        }

        public static ComponentMappingReference<TComponent> MapComponent<TSourceElement, TComponent>(
            this MappingReference<TSourceElement> mapping, Expression<Func<IQueryable<TSourceElement>, TComponent>> map)
        {
            var source = ImmutableDictionary<Expression, IMapping>.Empty.Add(map.Parameters.Single(), mapping.Instance);

            var instance = CreateMappingFromExpressionComponent<TComponent>(map, source);

            return mapping.Add(instance);
        }

        public static ComponentMappingReference<TComponent> MapComponent<TSourceComponent, TComponent>(
            this ComponentMappingReference<TSourceComponent> mapping, Expression<Func<TSourceComponent, TComponent>> map)
        {
            var parameter = map.Parameters.Single();

            var source = mapping.Instance.Mappings.ToImmutableDictionary(x => x.Key.Split('.').Aggregate((Expression)parameter, (ex, name) => Expression.Property(ex, name)), x => x.Value);

            var instance = CreateMappingFromExpressionComponent<TComponent>(map, source);

            return mapping.Add(instance);
        }

        public static ComponentMappingReference<CombinedComponentMapping<TComponentFirst, TComponentSecond>.Component> Combine<TComponentFirst, TComponentSecond>(
            this ComponentMappingReference<TComponentFirst> firstMapping, ComponentMappingReference<TComponentSecond> secondMapping)
        {
            return ContainerReference.Combine(firstMapping, secondMapping);
        }

        /// <summary>
        /// Creates Component Mapping from <paramref name="expression"/>
        /// </summary>
        /// <typeparam name="TComponent">Type of the component</typeparam>
        /// <param name="expression">Expression of the mapping</param>
        /// <param name="sources">Dictionary of the source mappings</param>
        /// <returns>the mapping</returns>
        private static IComponentMapping<TComponent> CreateMappingFromExpressionComponent<TComponent>(LambdaExpression expression, ImmutableDictionary<Expression, IMapping> sources)
        {
            NewExpression body;
            if (expression.Body.NodeType != ExpressionType.New || (body = expression.Body as NewExpression) == null)
            {
                throw new InvalidOperationException("Only new expression is supported for component definition");
            }

            if (body.Members != null)
            {
                // TODO: check property type
                var props = body.Members.Select(x => new { x.Name, Type = ((PropertyInfo)x).PropertyType.GetGenericArguments().Single() }).ToList();

                var mappings = body.Arguments
                    .Select((arg, i) => CreateMapping(props[i].Type, Expression.Lambda(arg, expression.Parameters), sources))
                    .Select((mapping, i) => new KeyValuePair<string, IMapping>(props[i].Name, mapping))
                    .ToImmutableDictionary();

                return new ComponentMapping<TComponent>(mappings);
            }

            var context = new MappingContext();

            var proxyArgs = body.Arguments
                .Select(x => TypeExt.InvokeGenericMethod<object>(new Func<LambdaExpression, IQueryable<object>>(
                    context.CreateSource<object>), x.Type.GetGenericArguments().Single(), Expression.Lambda(x, expression.Parameters)))
                .ToArray();

            return CreateMappingFromMappingContext(context, (TComponent)body.Constructor.Invoke(proxyArgs), sources);
        }

        private static ComponentMapping<TComponent> CreateMappingFromMappingContext<TComponent>(
            MappingContext context, TComponent component, ImmutableDictionary<Expression, IMapping> sources)
        {
            var items = GetComponentQueries(ImmutableList<PropertyInfo>.Empty, component, typeof(TComponent)).ToList();

            var depends = items.ToDictionary(
                item => item,
                item => new HashSet<ComponentQueryContainer>(items.Where(x => x != item).Where(x => context.IsSubset(item.Query, x.Query))));

            // remove not direct parents
            foreach (var item in items)
            {
                var remove = new HashSet<ComponentQueryContainer>();
                foreach (var parent in depends[item])
                {
                    foreach (var parentParent in depends[parent])
                    {
                        if (!context.IsSubset(item.Query, parentParent.Query, parent.Query))
                        {
                            remove.Add(parentParent);
                        }
                    }
                }

                depends[item].SymmetricExceptWith(remove);
            }

            var mappings = ImmutableDictionary.CreateBuilder<string, IMapping>();

            foreach (var item in depends.Where(x => x.Value.Count == 0).Select(x => x.Key))
            {
                var lambda = context.GetExpression(item.Query);

                var mapping = CreateMapping(item.ElementType, lambda, sources);

                mappings.Add(item.Name, mapping);
            }

            while (mappings.Count != items.Count)
            {
                foreach (var tuple in depends.Where(x => !mappings.ContainsKey(x.Key.Name)).Where(x => x.Value.All(m => mappings.ContainsKey(m.Name))))
                {
                    var lambda = context.GetExpression(tuple.Key.Query, tuple.Value.ToDictionary(x => x.Query, x => "this_" + x.Name));

                    var instanceSource = tuple.Value.ToDictionary(
                        x => (Expression)lambda.Parameters.Single(p => p.Name == "this_" + x.Name), x => mappings[x.Name]);

                    var mapping = CreateMapping(tuple.Key.ElementType, lambda, sources.AddRange(instanceSource));

                    mappings.Add(tuple.Key.Name, mapping);
                }
            }

            return new ComponentMapping<TComponent>(mappings.ToImmutable());
        }

        private static IMapping CreateMapping(Type elementType, LambdaExpression expression, ImmutableDictionary<Expression, IMapping> sourceMapping)
        {
            return TypeExt.InvokeGenericMethod<IMapping>(
                new Func<LambdaExpression, ImmutableDictionary<Expression, IMapping>, IMapping>(CreateMappingExpression<object>),
                elementType,
                expression,
                sourceMapping);
        }

        private static IMapping<TElement> CreateMappingExpression<TElement>(LambdaExpression expression, ImmutableDictionary<Expression, IMapping> sourceMapping)
        {
            if (expression.Body.NodeType == ExpressionType.Call && ((MethodCallExpression)expression.Body).Method.Name == "AsQueryable")
            {
                var enumerableExpression = Expression.Lambda<Func<IEnumerable<TElement>>>(((MethodCallExpression)expression.Body).Arguments[0]);

                var factory = enumerableExpression.Compile();

                return new EnumerableMapping<TElement>(factory.Invoke());
            }

            var component = expression.Parameters.SingleOrDefault(x => !typeof(IQueryable).IsAssignableFrom(x.Type));

            if (component == null)
            {
                var sources = expression.Parameters.Select(x => sourceMapping[x]).ToImmutableList();

                return new ExpressionMapping<TElement>(expression, sources);
            }
            else
            {
                var visitor = new FindSourceVisitor(
                    component, sourceMapping.Where(x => x.Key is MemberExpression).ToImmutableDictionary(ExpressionComparer.Default));

                var newexpression = Expression.Lambda(
                    visitor.Visit(expression.Body), visitor.Parameters.SelectMany(x => x == component ? visitor.Parameters : new[] { x }));

                var sources = expression.Parameters.SelectMany(x => x == component ? visitor.Sources : new[] { sourceMapping[x] }).ToImmutableList();

                return new ExpressionMapping<TElement>(newexpression, sources);
            }
        }

        private static IEnumerable<ComponentQueryContainer> GetComponentQueries(ImmutableList<PropertyInfo> path, object component, Type componentType)
        {
            foreach (var prop in componentType.GetProperties())
            {
                var find = prop.PropertyType.FindGenericType(typeof(IQueryable<>));

                if (find != null)
                {
                    yield return new ComponentQueryContainer(
                        path.Add(prop),
                        find.GetGenericArguments().Single(),
                        (IQueryable)prop.GetValue(component));
                }
                else if (prop.PropertyType.IsClass)
                {
                    foreach (var item in GetComponentQueries(path.Add(prop), prop.GetValue(component), prop.PropertyType))
                    {
                        yield return item;
                    }
                }
            }
        }

        private class FindSourceVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression sourceParameter;
            private readonly ImmutableDictionary<Expression, IMapping> sourceMappings;

            private readonly IDictionary<ParameterExpression, IMapping> sources = new Dictionary<ParameterExpression, IMapping>();
            private readonly IDictionary<Expression, ParameterExpression> parameters = new Dictionary<Expression, ParameterExpression>(ExpressionComparer.Default);

            public FindSourceVisitor(ParameterExpression sourceParameter, ImmutableDictionary<Expression, IMapping> sourceMappings)
            {
                this.sourceParameter = sourceParameter;
                this.sourceMappings = sourceMappings;
            }

            public IEnumerable<ParameterExpression> Parameters
            {
                get { return this.parameters.Values; }
            }

            public IEnumerable<IMapping> Sources
            {
                get { return this.parameters.Values.Select(x => this.sources[x]); }
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                ParameterExpression parameter;
                if (this.parameters.TryGetValue(node, out parameter))
                {
                    return parameter;
                }

                IMapping mapping;
                if (this.sourceMappings.TryGetValue(node, out mapping))
                {
                    var path = new List<PropertyInfo>();

                    Expression item = node;
                    while (item.NodeType == ExpressionType.MemberAccess)
                    {
                        var memberExpr = (MemberExpression)item;
                        path.Insert(0, (PropertyInfo)memberExpr.Member);
                        item = memberExpr.Expression;
                    }

                    if (item != this.sourceParameter)
                    {
                        throw new InvalidOperationException("This should not be reachable");
                    }

                    var name = this.sourceParameter + "_" + string.Join("_", path.Select(x => x.Name));

                    var find = ((PropertyInfo)node.Member).PropertyType.FindGenericType(typeof(IQueryable<>));

                    parameter = Expression.Parameter(((PropertyInfo)node.Member).PropertyType, name);

                    this.parameters.Add(node, parameter);
                    this.sources.Add(parameter, mapping);

                    return parameter;
                }

                return base.VisitMember(node);
            }
        }

        private class ComponentQueryContainer
        {
            private readonly ImmutableList<PropertyInfo> path;
            private readonly Type elementType;
            private readonly IQueryable query;

            public ComponentQueryContainer(ImmutableList<PropertyInfo> path, Type elementType, IQueryable query)
            {
                this.path = path;
                this.elementType = elementType;
                this.query = query;
            }

            public IQueryable Query
            {
                get { return this.query; }
            }

            public Type ElementType
            {
                get { return this.elementType; }
            }

            public string Name
            {
                get { return string.Join(".", this.path.Select(x => x.Name)); }
            }
        }
    }
}
