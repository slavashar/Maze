using CommandLine;

namespace DataFlow.Cmd
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var options = Parser.Default.ParseArguments<
                SimpleOption, 
                MultipleSourcesOptions>(args);

            ((options as Parsed<object>).Value as IExample)?.Execute();
        }
    }
}
