using System.Linq;
using Maze;
using Maze.Mappings;

namespace DataFlow
{
    public static class SimpleTransform
    {
        public static MappingReference<string> Example()
        {
            return Engine
                .Source("Input Data", Enumerable.Range(0, 100000))
                .CreateSimpleTransformation();
        }

        public static MappingReference<string> CreateSimpleTransformation(
            this MappingReference<int> sourceMapping)
        {
            return sourceMapping
                .Map("Transformation",
                     source => from x in source
                               where x % 1000 == 0
                               select "Number: " + x);
        }
    }
}
