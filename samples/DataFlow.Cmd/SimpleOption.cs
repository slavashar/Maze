using CommandLine;
using Maze;
using System;
using System.Linq;

namespace DataFlow.Cmd
{
    [Verb("simple", HelpText = "Execute a simple mapping.")]
    public class SimpleOption : IExample
    {
        public void Execute()
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
