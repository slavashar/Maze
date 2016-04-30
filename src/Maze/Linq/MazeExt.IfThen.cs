using System;

namespace Maze.Linq
{
    public static partial class MazeExt
    {
        public interface IMazeIf
        {
            IMazeIfThen<TValue> Then<TValue>(TValue result);
        }

        public interface IMazeIf<TValue>
        {
            IMazeIfThen<TValue> Then(TValue result);
        }

        public interface IMazeIfThen<TValue>
        {
            IMazeIf<TValue> ElseIf(bool testValues);

            TValue Else(TValue result);
        }

        public static IMazeIf If(bool testValues)
        {
            throw new InvalidOperationException("This method is not supposed to be used outside an expression");
        }
    }
}
