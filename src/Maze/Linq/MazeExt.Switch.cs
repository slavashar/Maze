using System;

namespace Maze.Linq
{
    public static partial class MazeExt
    {
        public interface IMazeSwitch<TValue>
        {
            IMazeSwitch<TValue, TResult> Case<TResult>(TValue testValues, TResult result);
        }

        public interface IMazeSwitch<TValue, TResult>
        {
            IMazeSwitch<TValue, TResult> Case(TValue testValues, TResult result);

            TResult Default(TResult result);
        }

        public static IMazeSwitch<TValue> Switch<TValue>(TValue switchValue)
        {
            throw new InvalidOperationException("This method is not supposed to be used outside an expression");
        }
    }
}
