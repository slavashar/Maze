using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Maze;
using Maze.Mappings;

namespace DataFlow.Web
{
    public static class MappingConfig
    {
        public static MappingReference<string> Example1()
        {
            return Engine.Source("Data source", Enumerable.Range(0, 100000).Select(x => Task.Delay(10).ContinueWith(_ => x).Result))
                         .Map(source =>
                              from x in source
                              where x % 1000 == 0
                              select "Number: " + x);
        }
    }
}
