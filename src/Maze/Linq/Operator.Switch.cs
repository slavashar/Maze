using System;
using System.Collections.Generic;

namespace Maze.Linq
{
    public static partial class Operator
    {
        public interface ISwitchValue<TValue>
        {
            ISwitch<TValue, TResult> Case<TResult>(TValue testValues, TResult result);

            ISwitch<TValue, TResult> Case<TResult>(IEnumerable<TValue> testValues, TResult result);
        }

        public interface ISwitch<TValue, TResult>
        {
            ISwitch<TValue, TResult> Case(TValue testValues, TResult result);

            ISwitch<TValue, TResult> Case(IEnumerable<TValue> testValues, TResult result);

            TResult Default(TResult result);
        }

        public static ISwitchValue<TValue> Switch<TValue>(TValue switchValue)
        {
            throw new InvalidOperationException("This method is not supposed to be used outside an expression");
        }
    }
}
