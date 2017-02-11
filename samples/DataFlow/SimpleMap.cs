using System.Linq;
using Maze;
using Maze.Mappings;

namespace DataFlow
{
    public static class SimpleMap
    {
        public static MappingReference<string> Example()
        {
            return Engine
                .Source("Data source", Enumerable.Range(0, 100000))
                .CreateSimpleMapping();
        }

        public static MappingReference<string> CreateSimpleMapping(
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
