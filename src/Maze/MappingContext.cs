using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;

using Maze.Linq;

namespace Maze
{
    public class MappingContext : IQueryProvider
    {
        private static readonly ExpressionVisitor Visitor = new OperatorVisitor();

        private readonly Dictionary<Expression, ExpressionNode> nodes = new Dictionary<Expression, ExpressionNode>(ExpressionComparer.Default);

        public IQueryable<TElement> CreateSource<TElement>(ParameterExpression parameter)
        {
            return this.CreateNode<TElement>(parameter, new[] { parameter });
        }

        public IQueryable<TElement> CreateSource<TElement>(string name = null)
        {
            var parameter = Expression.Parameter(typeof(IQueryable<TElement>), name);

            return this.CreateNode<TElement>(parameter, new[] { parameter });
        }

        public IQueryable<TElement> CreateSource<TElement>(LambdaExpression expression)
        {
            return this.CreateNode<TElement>(expression.Body, expression.Parameters);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return this.CreateNode<TElement>(Visitor.Visit(expression), null);
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return TypeExt.CallGenericMethod(this.CreateQuery<object>, expression, expression.Type);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            throw new NotImplementedException();
        }

        public object Execute(Expression expression)
        {
            return TypeExt.CallGenericMethod(this.Execute<object>, expression, expression.Type);
        }

        public LambdaExpression GetExpression(IQueryable query)
        {
            ExpressionNode node;
            if (query.Provider != this || (node = query as ExpressionNode) == null)
            {
                throw new InvalidOperationException("The query is not origin from this provider");
            }

            return Expression.Lambda(query.Expression, node.GetParametes(ImmutableHashSet<ExpressionNode>.Empty));
        }

        public LambdaExpression GetExpression(IQueryable query, params IQueryable[] parents)
        {
            ExpressionNode node;
            if (query.Provider != this || (node = query as ExpressionNode) == null)
            {
                throw new InvalidOperationException("The query is not origin from this provider");
            }

            var expression = query.Expression;

            foreach (var parent in parents)
            {
                ExpressionNode parentNode;
                if (parent.Provider != this || (parentNode = parent as ExpressionNode) == null)
                {
                    throw new InvalidOperationException("The query is not origin from this provider");
                }

                var param = Expression.Constant(parent, parent.Expression.Type);

                expression = new ReplaceExpression(parent.Expression, param).Visit(expression);
            }

            var parameters = node.GetParametes(parents.Cast<ExpressionNode>().ToImmutableHashSet());

            return Expression.Lambda(expression, parameters);
        }

        public LambdaExpression GetExpression(IQueryable query, IDictionary<IQueryable, string> parents)
        {
            ExpressionNode node;
            if (query.Provider != this || (node = query as ExpressionNode) == null)
            {
                throw new InvalidOperationException("The query is not origin from this provider");
            }

            var parameters = new HashSet<ParameterExpression>();

            var expression = query.Expression;

            foreach (var tuple in parents)
            {
                ExpressionNode parentNode;
                if (tuple.Key.Provider != this || (parentNode = tuple.Key as ExpressionNode) == null)
                {
                    throw new InvalidOperationException("The query is not origin from this provider");
                }

                var param = Expression.Parameter(tuple.Key.Expression.Type, tuple.Value);

                expression = new ReplaceExpression(tuple.Key.Expression, param).Visit(expression);

                parameters.Add(param);
            }

            parameters.UnionWith(node.GetParametes(parents.Select(x => (ExpressionNode)x.Key).ToImmutableHashSet()));

            return Expression.Lambda(expression, parameters);
        }

        public bool IsSubset(IQueryable query, IQueryable parent)
        {
            ExpressionNode parentNode, queryNode;
            if (parent.Provider != this || query.Provider != this || (parentNode = parent as ExpressionNode) == null || (queryNode = query as ExpressionNode) == null)
            {
                throw new InvalidOperationException("The query is not origin from this provider");
            }

            return queryNode.IsSubset(parentNode);
        }

        public bool IsSubset(IQueryable query, IQueryable parent, IQueryable from)
        {
            ExpressionNode parentNode, queryNode, fromNode;
            if (parent.Provider != this ||
                query.Provider != this ||
                from.Provider != this ||
                (parentNode = parent as ExpressionNode) == null ||
                (queryNode = query as ExpressionNode) == null ||
                (fromNode = from as ExpressionNode) == null)
            {
                throw new InvalidOperationException("The query is not origin from this provider");
            }

            return queryNode.IsSubset(parentNode, fromNode);
        }

        private ExpressionNode<TElement> CreateNode<TElement>(Expression expression, IEnumerable<ParameterExpression> parameters)
        {
            ExpressionNode existing;
            if (this.nodes.TryGetValue(expression, out existing))
            {
                return (ExpressionNode<TElement>)existing;
            }

            var parents = ImmutableList.CreateBuilder<ExpressionNode>();

            if (expression.NodeType == ExpressionType.Call)
            {
                foreach (var arg in ((MethodCallExpression)expression).Arguments)
                {
                    ExpressionNode item;
                    if (this.nodes.TryGetValue(arg, out item))
                    {
                        parents.Add(item);
                    }
                }
            }

            var node = new ExpressionNode<TElement>(this, expression, parents.ToImmutable(), parameters);
            this.nodes.Add(expression, node);
            return node;
        }

        private abstract class ExpressionNode
        {
            private readonly ImmutableList<ExpressionNode> parents;
            private readonly IEnumerable<ParameterExpression> parameters;

            public ExpressionNode(ImmutableList<ExpressionNode> parents, IEnumerable<ParameterExpression> parameters)
            {
                this.parents = parents;
                this.parameters = parameters;
            }

            public bool IsSubset(ExpressionNode parent)
            {
                if (this == parent)
                {
                    return true;
                }

                return this.parents.Any(x => x.IsSubset(parent));
            }

            public bool IsSubset(ExpressionNode parent, ExpressionNode from)
            {
                if (this == from)
                {
                    return false;
                }

                if (this == parent)
                {
                    return true;
                }

                return this.parents.Any(x => x.IsSubset(parent, from));
            }

            public IEnumerable<ParameterExpression> GetParametes(ISet<ExpressionNode> untils)
            {
                var set = new HashSet<ParameterExpression>();

                this.PassParameters(set, new HashSet<ExpressionNode>(untils));

                return set;
            }

            private void PassParameters(HashSet<ParameterExpression> set, ISet<ExpressionNode> until)
            {
                if (this.parameters != null)
                {
                    set.UnionWith(this.parameters);
                }

                foreach (var parent in this.parents)
                {
                    if (!until.Contains(parent))
                    {
                        parent.PassParameters(set, until);
                    }
                }
            }
        }

        private class ExpressionNode<TElement> : ExpressionNode, IQueryable<TElement>
        {
            private readonly MappingContext mappingContext;
            private readonly Expression expression;

            public ExpressionNode(
                MappingContext mappingContext,
                Expression expression,
                ImmutableList<ExpressionNode> parents,
                IEnumerable<ParameterExpression> parameters)
                : base(parents, parameters)
            {
                this.mappingContext = mappingContext;
                this.expression = expression;
            }

            public Expression Expression
            {
                get { return this.expression; }
            }

            public Type ElementType
            {
                get { return typeof(TElement); }
            }

            public IQueryProvider Provider
            {
                get { return this.mappingContext; }
            }

            public IEnumerator<TElement> GetEnumerator()
            {
                throw new InvalidOperationException("This query used only for retrieving the expression");
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        private class ReplaceExpression : ExpressionVisitor
        {
            private readonly Expression expression, replacement;

            public ReplaceExpression(Expression expression, Expression replacement)
            {
                this.expression = expression;
                this.replacement = replacement;
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                var args = VisitCollection(node.Arguments, x => x == this.expression ? this.replacement : this.Visit(x));

                if (args == node.Arguments)
                {
                    return node;
                }

                return node.Update(node.Object, args);
            }

            private static IReadOnlyList<T> VisitCollection<T>(IReadOnlyList<T> original, Func<T, T> visitor)
            {
                List<T> list = null;

                for (int i = 0, n = original.Count; i < n; i++)
                {
                    T p = visitor(original[i]);

                    if (list != null)
                    {
                        list.Add(p);
                    }
                    else if (!object.ReferenceEquals(p, original[i]))
                    {
                        list = new List<T>(n);
                        for (int j = 0; j < i; j++)
                        {
                            list.Add(original[j]);
                        }

                        list.Add(p);
                    }
                }

                if (list != null)
                {
                    return list;
                }

                return original;
            }
        }
    }
}
