using Maze.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using Xunit;
using Observable = System.Reactive.Linq.Observable;
using ObservableMaze = Maze.Reactive.Observable;

namespace Maze.Facts
{
    public class ObservableRewriterFacts
    {
        [Fact]
        public void rewrite_a_anonymous_type_constractor()
        {
            Expression<Func<IEnumerable<int>, object>> expr = 
                number => new
                {
                    Result = number
                };

            var result = ObservableRewriter.ChangeParameters(expr);

            result.ShouldEqualExpression<Func<IObservable<int>, dynamic>>(
                number => new
                {
                    Result = number
                });
        }

        [Fact]
        public void rewrite_a_member_init_statement()
        {
            Expression<Func<IEnumerable<int>, Result>> expr = 
                values => new Result
                {
                    Value = values.First()
                };

            var result = ObservableRewriter.ChangeParameters(expr);

            result.ShouldEqualExpression<Func<IObservable<int>, dynamic>>(
                values => new
                {
                    Value = values.FirstAsync()
                });
        }

        [Fact]
        public void rewrite_parameter()
        {
            Expression<Func<IEnumerable<int>, IEnumerable<int>>> expr = values => values;

            var result = ObservableRewriter.ChangeParameters(expr);

            result.Body.Type.ShouldEqual(typeof(IObservable<int>));
        }

        [Fact]
        public void rewrite_member()
        {
            Expression<Func<IEnumerable<IEnumerable<int>>, IEnumerable<IEnumerable<int>>>> expr = 
                source => source.Select(values => new { values }).Select(x => x.values);

            var result = ObservableRewriter.ChangeParameters(expr);

            result.ShouldEqualExpression<Func<IObservable<IObservable<int>>, IObservable<IObservable<int>>>>(
                source => source.Select(values => new { values }).Select(x => x.values));
        }

        [Fact]
        public void rewrite_a_select_statement()
        {
            Expression<Func<IEnumerable<int>, IEnumerable<string>>> expr = source => source.Select(x => x.ToString());

            var result = ObservableRewriter.ChangeParameters(expr);

            result.ShouldEqualExpression<Func<IObservable<int>, IObservable<string>>>(
                source => source.Select(x => x.ToString()));
        }

        [Fact]
        public void rewrite_a_select_queryable_statement()
        {
            Expression<Func<IQueryable<int>, IQueryable<string>>> expr = source => source.Select(x => x.ToString());

            var result = ObservableRewriter.ChangeParameters(expr);

            result.ShouldEqualExpression<Func<IObservable<int>, IObservable<string>>>(
                source => source.Select(x => x.ToString()));
        }

        [Fact]
        public void rewrite_a_select_many_queryable_statement()
        {
            Expression<Func<IQueryable<int>, IQueryable<int>>> expr = source => source.SelectMany(x => new[] { x - 1, x, x + 1 });

            var result = ObservableRewriter.ChangeParameters(expr);

            result.ShouldEqualExpression<Func<IObservable<int>, IObservable<int>>>(
                source => source.SelectMany(x => new[] { x - 1, x, x + 1 }));
        }

        [Fact]
        public void rewrite_a_select_many_from_a_collection_queryable_statement()
        {
            Expression<Func<IQueryable<SourceWithCildCollection>, IQueryable<int>>> expr = source => source.SelectMany(x => x.Items);

            var result = ObservableRewriter.ChangeParameters(expr);

            result.ShouldEqualExpression<Func<IObservable<SourceWithCildCollection>, IObservable<int>>>(
                source => source.SelectMany(x => x.Items));
        }

        [Fact]
        public void rewrite_a_where_statement()
        {
            Expression<Func<IEnumerable<int>, IEnumerable<int>>> expr = source => source.Where(x => x != 0);

            var result = ObservableRewriter.ChangeParameters(expr);

            result.ShouldEqualExpression<Func<IObservable<int>, IObservable<int>>>(source => source.Where(x => x != 0));
        }

        [Fact]
        public void rewrite_a_where_queryable_statement()
        {
            Expression<Func<IQueryable<int>, IQueryable<int>>> expr = source => source.Where(x => x != 0);

            var result = ObservableRewriter.ChangeParameters(expr);

            result.ShouldEqualExpression<Func<IObservable<int>, IObservable<int>>>(source => source.Where(x => x != 0));
        }

        [Fact]
        public void rewrite_a_where_obs_statement()
        {
            Expression<Func<IEnumerable<IEnumerable<int>>, IEnumerable<int>>> expr = 
                source => source.Select(x => x.Count()).Where(x => x != 0);

            var result = ObservableRewriter.ChangeParameters(expr);

            result.ShouldEqualExpression<Func<IObservable<IObservable<int>>, IObservable<IObservable<int>>>>(
                source => source.Select(x => x.Count()).Where(x => x.Select(__x => __x != 0)));
        }

        [Fact]
        public void rewrite_a_group_by_statement()
        {
            Expression<Func<IEnumerable<int>, IEnumerable<IGrouping<string, int>>>> expr = source => source.GroupBy(x => x.ToString());

            var result = ObservableRewriter.ChangeParameters(expr);

            result.ShouldEqualExpression<Func<IObservable<int>, IObservable<IGroupedObservable<string, int>>>>(
                source => source.GroupBy(x => x.ToString()));
        }

        [Fact]
        public void rewrite_a_count_statement()
        {
            Expression<Func<IEnumerable<int>, int>> expr = source => source.Count();

            var result = ObservableRewriter.ChangeParameters(expr);

            result.ShouldEqualExpression<Func<IObservable<int>, IObservable<int>>>(source => source.Count());
        }

        [Fact]
        public void rewrite_a_first_statement()
        {
            Expression<Func<IEnumerable<int>, int>> expr = source => source.First();

            var result = ObservableRewriter.ChangeParameters(expr);

            result.ShouldEqualExpression<Func<IObservable<int>, IObservable<int>>>(source => source.FirstAsync());
        }

        [Fact]
        public void rewrite_a_last_statement()
        {
            Expression<Func<IEnumerable<int>, int>> expr = source => source.Last();

            var result = ObservableRewriter.ChangeParameters(expr);

            result.ShouldEqualExpression<Func<IObservable<int>, IObservable<int>>>(source => source.LastAsync());
        }

        [Fact]
        public void rewrite_a_last_of_children_statement()
        {
            Expression<Func<IQueryable<SourceWithCildCollection>, IQueryable<int>>> expr = source => source.Select(x => x.Items.Last());

            var result = ObservableRewriter.ChangeParameters(expr);

            result.ShouldEqualExpression<Func<IObservable<SourceWithCildCollection>, IObservable<int>>>(source => source.Select(x => x.Items.Last()));
        }

        [Fact]
        public void rewrite_a_contains_statement()
        {
            Expression<Func<IEnumerable<int>, bool>> expr = source => source.Contains(0);

            var result = ObservableRewriter.ChangeParameters(expr);

            result.ShouldEqualExpression<Func<IObservable<int>, IObservable<bool>>>(source => source.Contains(0));
        }

        [Fact]
        public void rewrite_a_join_statement()
        {
            Expression<Func<IEnumerable<int>, IEnumerable<Source>, IEnumerable<string>>> expr =
                (items, source) => items.Join(source, x => x, x => x.Id, (i, s) => i + s.Txt);

            var result = ObservableRewriter.ChangeParameters(expr);

            result.ShouldEqualExpression<Func<IObservable<int>, IObservable<Source>, IObservable<string>>>(
                (items, source) => items.Join(source, x => x, x => x.Id, (i, s) => i + s.Txt));
        }

        [Fact]
        public void rewrite_a_join_statement_on_changed_collection()
        {
            Expression<Func<IEnumerable<IEnumerable<string>>, IEnumerable<string>>> expr =
                source => source
                    .Select(x => new { Count = x.Count(), Last = x.Last() })
                    .Join(source.Select(x => x.Count()), x => x.Count, x => x, (x1, x2) => x1.Last + x2);

            var result = ObservableRewriter.ChangeParameters(expr);

            result.ShouldEqualExpression<Func<IObservable<IObservable<string>>, IObservable<IObservable<string>>>>(
                source => ObservableMaze.Join(
                          source.Select(x => new { Count = x.Count(), Last = x.LastAsync() }),
                          source.Select(x => x.Count()),
                          x => x.Count,
                          x => x, 
                          (x1, x2) => Observable.CombineLatest(x1.Count, x1.Last, x2, (__x1_Count, __x1_Last, __x2) => __x1_Last + __x2)));
        }

        [Fact]
        public void rewrite_a_group_join_statement()
        {
            Expression<Func<IEnumerable<int>, IEnumerable<Source>, IEnumerable<int>>> expr =
                (items, source) => items.GroupJoin(source, x => x, x => x.Id, (i, s) => i);

            var result = ObservableRewriter.ChangeParameters(expr);

            result.ShouldEqualExpression<Func<IObservable<int>, IObservable<Source>, IObservable<int>>>(
                (items, source) => items.GroupJoin(source, x => x, x => x.Id, (i, s) => i));
        }

        [Fact]
        public void rewrite_a_group_join_statement_2()
        {
            Expression<Func<IEnumerable<int>, IEnumerable<Source>, IEnumerable<int>>> expr =
                (items, source) => items
                    .GroupJoin(source, x => x, s => s.Id, (x, decomposed) => new { decomposed })
                    .Select(record => record.decomposed.Count());

            var result = ObservableRewriter.ChangeParameters(expr);

            result.ShouldEqualExpression<Func<IObservable<int>, IObservable<Source>, IObservable<IObservable<int>>>>(
                (items, source) => items
                    .GroupJoin(source, x => x, s => s.Id, (x, decomposed) => new { decomposed })
                    .Select(record => record.decomposed.Count()));
        }

        [Fact]
        public void rewrite_a_constant_list()
        {
            Expression<Func<IEnumerable<string>>> expr = () => new[] { "Test1", "Test2" };

            var result = ObservableRewriter.ChangeParameters(expr);

            result.ShouldEqualExpression<Func<IEnumerable<string>>>(() => new[] { "Test1", "Test2" });
        }

        [Fact]
        public void rewrite_a_chain_condition()
        {
            Expression<Func<IEnumerable<string>, IEnumerable<string>>> expr =
                source => source.Where(x => new[] { "Test1", "Test2" }.Contains(x));

            var result = ObservableRewriter.ChangeParameters(expr);

            result.ShouldEqualExpression<Func<IObservable<string>, IObservable<string>>>(
                source => source.Where(x => new[] { "Test1", "Test2" }.Contains(x)));
        }
        
        [Fact]
        public void rewrite_a_chain_condition_2()
        {
            Expression<Func<IEnumerable<IEnumerable<string>>, IEnumerable<int>>> expr =
                source => source.Select(x => x.Count());

            var result = ObservableRewriter.ChangeParameters(expr);

            result.ShouldEqualExpression<Func<IObservable<IObservable<string>>, IObservable<IObservable<int>>>>(
                source => source.Select(x => x.Count()));
        }

        [Fact]
        public void rewrite_on_a_changed_collection()
        {
            Expression<Func<IEnumerable<IEnumerable<string>>, IEnumerable<bool>>> expr =
                source => source.Select(x => x.Count()).Select(x => x > 5);

            var result = ObservableRewriter.ChangeParameters(expr);

            result.ShouldEqualExpression<Func<IObservable<IObservable<string>>, IObservable<IObservable<bool>>>>(
                source => source.Select(x => x.Count()).Select(x => x.Select(__x => __x > 5)));
        }

        [Fact]
        public void rewrite_on_a_changed_collection2()
        {
            Expression<Func<IEnumerable<IEnumerable<bool>>, IEnumerable<bool>>> expr =
                source => source.Select(x => x.Max()).Select(x => x);

            var result = ObservableRewriter.ChangeParameters(expr);

            result.ShouldEqualExpression<Func<IObservable<IObservable<bool>>, IObservable<IObservable<bool>>>>(
                source => source.Select(x => x.Max()).Select(x => x));
        }

        [Fact]
        public void rewrite_on_a_changed_item_single_members()
        {
            Expression<Func<IEnumerable<IEnumerable<string>>, IEnumerable<bool>>> expr =
                source => source.Select(x => new
                {
                    Last = x.Last(),
                    Magic = "string"
                })
                .Select(x => x.Last == x.Magic);

            var result = ObservableRewriter.ChangeParameters(expr);

            result.ShouldEqualExpression<Func<IObservable<IObservable<string>>, IObservable<IObservable<bool>>>>(
                source => source.Select(x => new
                {
                    Last = x.LastAsync(),
                    Magic = "string"
                })
                .Select(x => x.Last.Select(__x_Last => __x_Last == x.Magic)));
        }

        [Fact]
        public void rewrite_on_a_changed_item_multiple_members()
        {
            Expression<Func<IEnumerable<IEnumerable<string>>, IEnumerable<bool>>> expr =
                source => source.Select(x => new 
                { 
                    Count = x.Count(), 
                    Last = x.Last(), 
                    First = x.First(),
                    Magic = "string"
                })
                .Select(x => x.Last == x.First || x.Last == x.Magic);

            var result = ObservableRewriter.ChangeParameters(expr);

            result.ShouldEqualExpression<Func<IObservable<IObservable<string>>, IObservable<IObservable<bool>>>>(
                source => source.Select(x => new
                {
                    Count = x.Count(),
                    Last = x.LastAsync(),
                    First = x.FirstAsync(),
                    Magic = "string"
                })
                .Select(x => Observable.CombineLatest(x.Count, x.Last, x.First, (__x_Count, __x_Last, __x_First) => __x_Last == __x_First || __x_Last == x.Magic)));
        }

        [Fact]
        public void rewrite_join_with_changed_collection()
        {
            Expression<Func<IEnumerable<IEnumerable<string>>, IEnumerable<string>>> expr =
                source => source.Select(x => new
                {
                    Count = x.Count(),
                    Last = x.Last(),
                    First = x.First(),
                    Magic = "string"
                })
                .Join(source.Select(x => x.Count()), x => x.Count, x => x, (x1, x2) => x1.Last + x2);

            var result = ObservableRewriter.ChangeParameters(expr);
        }

        [Fact]
        public void rewrite_concat_queryable()
        {
            Expression<Func<IQueryable<string>, IQueryable<string>, IQueryable<string>>> expr =
                (source1, source2) => source1.Concat(source2);

            var result = ObservableRewriter.ChangeParameters(expr);

            result.ShouldEqualExpression<Func<IObservable<string>, IObservable<string>, IObservable<string>>>(
                (source1, source2) => source1.Merge(source2));
        }

        public class Source
        {
            public int Id { get; set; }

            public string Txt { get; set; }
        }

        public class SourceWithCildCollection
        {
            public IEnumerable<int> Items { get; set; }
        }

        public class Result
        {
            public int Value { get; set; }
        }
    }
}
