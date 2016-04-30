using System;

namespace Maze.Mappings
{
    internal interface IAnonymousMapping : IMapping
    {
        Type ElementType { get; }
    }
}
