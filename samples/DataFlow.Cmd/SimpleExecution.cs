using System;
using System.Linq;
using System.Reactive.Linq;
using Maze;

namespace DataFlow.Cmd
{
    public class SimpleExecution
    {
        public static void Run()
        {
            var execution = Engine
                .Source("Data source", Enumerable.Range(0, 100000))
                .Map("Transformation",
                     source => from x in source
                               where x % 1000 == 0
                               select "Number: " + x)
                .Build();

            execution.Subscribe(Console.WriteLine);

            execution.Release().Wait();
        }
    }
}
