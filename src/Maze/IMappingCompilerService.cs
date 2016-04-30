using Maze.Mappings;

namespace Maze
{
    public interface IMappingCompilerService
    {
        IExecutionGraph Build(ContainerReference mapping);
    }
}
