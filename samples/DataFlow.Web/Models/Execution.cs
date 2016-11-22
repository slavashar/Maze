using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Maze;
using Maze.Nodes;

namespace DataFlow.Web.Models
{
    public class Execution
    {
        public Guid Id { get; } = Guid.NewGuid();

        public string Name { get; set; }

        public ExecutionGraph Engine { get; set; }

        public Node Graph { get; set; }
    }

    public class ExecutionBlock
    {
        public Guid Id { get; } = Guid.NewGuid();

        public Node Node { get; set; }
    }

    public class ExecutionBlock<TElement> : ExecutionBlock
    {

    }
}
