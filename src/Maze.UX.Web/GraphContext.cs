using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Msagl.Core.Layout;

namespace Maze.UX.Web
{
    public class GraphContext
    {
        private readonly ConditionalWeakTable<Node, string> labels = new ConditionalWeakTable<Node, string>();

        public void SetNodeLabel(Node geometryNode, string label)
        {
            this.labels.Add(geometryNode, label);
        }

        public string GetNodeLabel(Node geometryNode)
        {
            return this.labels.GetValue(geometryNode, n => string.Empty);
        }
    }
}
