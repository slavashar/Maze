using Maze;
using Maze.Nodes;
using Maze.UX;
using Maze.UX.Svg;
using Maze.UX.Web;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace DataFlow.Web.Extensions
{
    [HtmlTargetElement("svg-graph", TagStructure = TagStructure.WithoutEndTag)]
    public class SvgGraphTagHelper : TagHelper
    {
        public Graph<Node> Graph { get; set; }

        public ExecutionGraph Execution { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var grContext = new GraphContext();

            var graph = new GeometryGraphBuilder(grContext).Build(this.Graph);

            output.TagName = "div";
            output.Attributes.Add("class", "maze-graph-container");
            output.Content.SetHtmlContent(new SvgGraphXRenderer(graph, grContext));
            output.TagMode = TagMode.StartTagAndEndTag;
        }
    }
}
