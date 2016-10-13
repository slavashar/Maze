using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using Maze.Nodes;
using Microsoft.Reactive.Testing;
using Xunit;
using static Microsoft.Reactive.Testing.ReactiveTest;

namespace Maze.Facts
{
    public class MetricObservableRewriterFacts
    {
        [Fact]
        public void attach_a_tracker()
        {
            Expression<Func<IQueryable<int>, IQueryable<string>>> expr =
                numbers => numbers.Select(x => x.ToString());

            var rewriter = new MetricObservableRewriter(
                ObservableRewriter.ChangeParameters(expr.Parameters),
                (MethodCallExpression)expr.Body);

            var result = (Expression<Func<IObservable<int>, IObservable<string>>>)rewriter.Visit(expr);

            var transform = result.Compile();
            
            var tracked = rewriter.Trackers[(MethodCallExpression)expr.Body].Get<string>().ToList().ToTask();

            var scheduler = new TestScheduler();

            var observable = scheduler
                .CreateHotObservable(
                    OnNext(10, 1),
                    OnNext(10, 2),
                    OnNext(10, 3),
                    OnCompleted<int>(10));

            transform(observable).Subscribe(new Subject<string>());

            scheduler.AdvanceBy(100);

            tracked.IsCompleted.ShouldBeTrue();

            tracked.Result.ShouldEqual("1", "2", "3");
        }

        [Fact]
        public void attach_multiple_trackers()
        {
            Expression<Func<IQueryable<int>, IQueryable<string>>> expr =
                numbers => from x in numbers
                           where x % 3 == 0
                           select (x + 1).ToString();

            var graph = ExpressionNodeBuilder.Parse(expr).ToGraph();

            var expressions = graph.Nodes.OfType<IElementNode<Expression>>().Select(x => x.Element).ToArray();
            
            var rewriter = new MetricObservableRewriter(
                ObservableRewriter.ChangeParameters(expr.Parameters),
                expressions);

            var result = (Expression<Func<IObservable<int>, IObservable<string>>>)rewriter.Visit(expr);

            var transform = result.Compile();

            var parameter = rewriter.Trackers[expressions.Single(x => x is ParameterExpression)].Get<int>().ToList().ToTask();
            var where = rewriter.Trackers[expressions.Single(x => (x as MethodCallExpression)?.Method.Name == "Where")].Get<int>().ToList().ToTask();
            var select = rewriter.Trackers[expressions.Single(x => (x as MethodCallExpression)?.Method.Name == "Select")].Get<string>().ToList().ToTask();

            var scheduler = new TestScheduler();

            var observable = scheduler
                .CreateHotObservable(
                    OnNext(10, 1),
                    OnNext(10, 2),
                    OnNext(10, 3),
                    OnNext(10, 6),
                    OnNext(10, 9),
                    OnCompleted<int>(10));

            transform(observable).Subscribe(new Subject<string>());

            scheduler.AdvanceBy(100);

            parameter.IsCompleted.ShouldBeTrue();
            where.IsCompleted.ShouldBeTrue();
            select.IsCompleted.ShouldBeTrue();

            parameter.Result.ShouldEqual(1, 2, 3, 6, 9);
            where.Result.ShouldEqual(3, 6, 9);
            select.Result.ShouldEqual("4", "7", "10");
        }
    }
}
