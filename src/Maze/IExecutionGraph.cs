using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Maze.Mappings;

namespace Maze
{
    public interface IExecutionGraph
    {
        void Release();

        IObservable<TElement> GetStream<TElement>(MappingReference<TElement> mapping);
    }

    internal interface IExecutionGraphBuilder<TNode>
    {
        TNode CreateExpressionNode<TElement>(IMapping<TElement> mapping, LambdaExpression expression, IEnumerable<TNode> sourceNodes);
    }
}
