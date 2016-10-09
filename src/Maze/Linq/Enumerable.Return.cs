using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maze.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TResult> Return<TResult>(TResult value)
        {
            yield return value;
        }
    }
}
