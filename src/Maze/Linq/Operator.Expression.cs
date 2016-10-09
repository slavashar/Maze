using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Maze.Linq
{
    public static partial class Operator
    {
        public static TResult Expression<T, TResult>(Expression<Func<T, TResult>> expression, T arg)
        {
            throw new InvalidOperationException("This method is not supposed to be used outside an expression");
        }

        public static TResult Expression<T1, T2, TResult>(Expression<Func<T1, T2, TResult>> expression, T1 arg1, T2 arg2)
        {
            throw new InvalidOperationException("This method is not supposed to be used outside an expression");
        }

        public static TResult Expression<T1, T2, T3, TResult>(Expression<Func<T1, T2, T3, TResult>> expression, T1 arg1, T2 arg2, T3 arg3)
        {
            throw new InvalidOperationException("This method is not supposed to be used outside an expression");
        }

        public static TResult Expression<T1, T2, T3, T4, TResult>(Expression<Func<T1, T2, T3, T4, TResult>> expression, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            throw new InvalidOperationException("This method is not supposed to be used outside an expression");
        }

        public static TResult Expression<T1, T2, T3, T4, T5, TResult>(Expression<Func<T1, T2, T3, T4, T5, TResult>> expression, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            throw new InvalidOperationException("This method is not supposed to be used outside an expression");
        }

        public static TResult Expression<T1, T2, T3, T4, T5, T6, TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, TResult>> expression, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            throw new InvalidOperationException("This method is not supposed to be used outside an expression");
        }

        public static TResult Expression<T1, T2, T3, T4, T5, T6, T7, TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, TResult>> expression, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            throw new InvalidOperationException("This method is not supposed to be used outside an expression");
        }

        public static TResult Expression<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>> expression, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            throw new InvalidOperationException("This method is not supposed to be used outside an expression");
        }

        public static TResult Expression<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>> expression, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        {
            throw new InvalidOperationException("This method is not supposed to be used outside an expression");
        }
    }
}
