using Maze.Mappings;

namespace Maze
{
    public interface IMappingCompilerService
    {
        ExecutionGraph Build(MappingContainer mapping);
    }
}
