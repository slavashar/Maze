using CommandLine;
using Maze;
using System;

namespace DataFlow.Cmd
{
    [Verb("simple", HelpText = "Execute a simple mapping.")]
    public class SimpleOption : IExample
    {
        public void Execute()
        {
            var execution = SimpleMap.Example()
                .Build();

            execution.Subscribe(Console.WriteLine);

            execution.Release().Wait();
        }
    }
}
