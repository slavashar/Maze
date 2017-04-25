using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DataFlow.Web.Models;
using Maze;
using DataFlow.Web.Extensions;

namespace DataFlow.Web.Controllers
{
    public class RunController : Controller
    {
        public static ConcurrentDictionary<Guid, Execution> Executions = new ConcurrentDictionary<Guid, Execution>();

        [HttpPost]
        public IActionResult StartSimpleMap()
        {
            var mapping = SimpleTransform.Example();

            var graph = mapping.ExtractGraph();

            var compiler = new DetailedReactiveCompilerService();

            var executionGraph = compiler.Build(mapping.Container, null /*  */);

            var model = new Execution
            {
                Name = "Simple execution",
                Engine = executionGraph,
                Graph = graph
            };

            if (!Executions.TryAdd(model.Id, model))
            {
                throw new InvalidOperationException();
            }

            model.Engine.Release();

            return RedirectToAction("Index", new { id = model.Id });
        }

        public IActionResult Index(Guid id)
        {
            return View("Graph", Executions[id]);
        }
    }
}