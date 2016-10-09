using System;

namespace Maze.Linq
{
    public static partial class Operator
    {
        public interface IIfThen<TValue>
        {
            IIfThen<TValue> ElseIf(bool testValues, TValue result);

            TValue Else(TValue result);
        }

        public static IIfThen<TValue> If<TValue>(bool testValues, TValue result)
        {
            throw new InvalidOperationException("This method is not supposed to be used outside an expression");
        }
    }
}
