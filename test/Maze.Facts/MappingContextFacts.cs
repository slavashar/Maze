using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Maze.Facts
{
    public class MappingContextFacts
    {
        [Fact]
        public void create_a_source()
        {
            var context = new MappingContext();

            var query = context.CreateSource<Source>("source");

            var expression = context.GetExpression(query);

            expression.ShouldEqualExpression<Func<IQueryable<Source>, IQueryable<Source>>>(source => source);
        }

        [Fact]
        public void create_a_expression_source()
        {
            var context = new MappingContext();

            Expression<Func<IQueryable<int>, IQueryable<int>>> sample = src => src.Select(x => x * 10);

            var query = context.CreateSource<int>(sample);
            
            var expression = context.GetExpression(query);

            expression.ShouldEqualExpression<Func<IQueryable<int>, IQueryable<int>>>(src => src.Select(x => x * 10));
        }

        [Fact]
        public void create_a_transformation()
        {
            var context = new MappingContext();

            var query = from x in context.CreateSource<Source>("source")
                        select new Dest { Value = x.Value * 10 };

            var expression = context.GetExpression(query);

            expression.ShouldEqualExpression<Func<IQueryable<Source>, IQueryable<Dest>>>(source => from x in source select new Dest { Value = x.Value * 10 });
        }

        [Fact]
        public void create_a_transformation_with_multiple_sources()
        {
            var context = new MappingContext();

            var query = from s in context.CreateSource<Source>("source")
                        join d in context.CreateSource<Dest>("dest") on s.Value equals d.Value
                        select s.Value + d.Value;
            
            var expression = context.GetExpression(query);

            expression.ShouldEqualExpression<Func<IQueryable<Source>, IQueryable<Dest>, IQueryable<int>>>(
                (source, dest) => from s in source
                                  join d in dest on s.Value equals d.Value
                                  select s.Value + d.Value);
        }

        [Fact]
        public void create_a_transformation_with_expression_source()
        {
            var context = new MappingContext();

            Expression<Func<IQueryable<int>, IQueryable<int>>> sample = src => src.Select(x => x * 10);

            var query = from x in context.CreateSource<int>(sample)
                        select new Dest { Value = x * 10 };

            var expression = context.GetExpression(query);

            expression.ShouldEqualExpression<Func<IQueryable<int>, IQueryable<Dest>>>(
                src => from x in src.Select(x => x * 10)
                       select new Dest { Value = x * 10 });
        }

        [Fact]
        public void create_multiple_transformations()
        {
            var context = new MappingContext();

            var query = context
                .CreateSource<Source>("src")
                .Select(source => new Dest { Value = source.Value * 10 })
                .Select(dest => new Dest { Value = dest.Value * 10 });

            var expression = context.GetExpression(query);

            expression.ShouldEqualExpression<Func<IQueryable<Source>, IQueryable<Dest>>>(
                src => src.Select(source => new Dest { Value = source.Value * 10 })
                          .Select(dest => new Dest { Value = dest.Value * 10 }));
        }

        [Fact]
        public void check_dependency()
        {
            var context = new MappingContext();

            var first = context
                .CreateSource<Source>("src")
                .Select(source => new Dest { Value = source.Value * 10 });

            var second = first
                .Select(dest => new Dest { Value = dest.Value * 10 });

            context.IsSubset(first, second).ShouldBeFalse();
            context.IsSubset(second, first).ShouldBeTrue();
        }

        [Fact]
        public void check_multiple_dependency()
        {
            var context = new MappingContext();

            var first = context
                .CreateSource<Source>("src1")
                .Select(source => new Dest { Value = source.Value * 10 });

            var second = context
                .CreateSource<Source>("src2")
                .Select(dest => new Dest { Value = dest.Value * 10 });

            var third = first
                .Join(second, x => x, x => x, (x1, x2) => new { });

            context.IsSubset(third, first).ShouldBeTrue();
            context.IsSubset(third, second).ShouldBeTrue();
        }

        [Fact]
        public void check_nasted_dependency()
        {
            var context = new MappingContext();

            var first = context
                .CreateSource<Source>("src")
                .Select(src => new Dest { Value = src.Value * 1 });

            var second = first
                .Select(src => new Dest { Value = src.Value * 10 });

            var third = second
                .Select(src => new Dest { Value = src.Value * 100 });
            
            context.IsSubset(third, first).ShouldBeTrue();
            context.IsSubset(third, second).ShouldBeTrue();

            context.IsSubset(third, first, second).ShouldBeFalse();
            context.IsSubset(third, second, first).ShouldBeTrue();
        }

        [Fact]
        public void get_subset_with_alia_parameter()
        {
            var context = new MappingContext();

            var first = context
                .CreateSource<Source>("src")
                .Select(source => new Dest { Value = source.Value * 10 });

            var second = first
                .Select(dest => new Dest { Value = dest.Value * 5 });

            var expression = context.GetExpression(second, new Dictionary<IQueryable, string> { { first, "__first" } });

            expression.ShouldEqualExpression<Func<IQueryable<Dest>, IQueryable<Dest>>>(
                __first => __first.Select(dest => new Dest { Value = dest.Value * 5 }));
        }

        [Fact]
        public void get_subset_with_multiple_alia_parameters()
        {
            var context = new MappingContext();

            var first = context
                .CreateSource<Source>("src1")
                .Select(source => new Dest { Value = source.Value * 10 });

            var second = context
                .CreateSource<Source>("src2")
                .Select(source => new Dest { Value = source.Value * 10 });

            var third = first.Join(second, x => x, x => x, (x1, x2) => x1);

            var expression = context.GetExpression(third, new Dictionary<IQueryable, string> { { first, "__first" }, { second, "__second" } });

            expression.ShouldEqualExpression<Func<IQueryable<Dest>, IQueryable<Dest>, IQueryable<Dest>>>(
                (__first, __second) => __first.Join(__second, x => x, x => x, (x1, x2) => x1));
        }

        [Fact]
        public void get_subset_with_alia_parameter_abd_source()
        {
            var context = new MappingContext();

            var first = context
                .CreateSource<Source>("src1")
                .Select(source => new Dest { Value = source.Value * 10 });

            var second = context
                .CreateSource<Source>("src2")
                .Select(source => new Dest { Value = source.Value * 10 });

            var third = first.Join(second, x => x, x => x, (x1, x2) => x1);

            var expression = context.GetExpression(third, new Dictionary<IQueryable, string> { { first, "__first" } });

            expression.ShouldEqualExpression<Func<IQueryable<Dest>, IQueryable<Source>, IQueryable<Dest>>>(
                (__first, src2) => __first.Join(src2.Select(source => new Dest { Value = source.Value * 10 }), x => x, x => x, (x1, x2) => x1));
        }

        [Fact]
        public void get_subset_with_constant()
        {
            var context = new MappingContext();

            var first = context
                .CreateSource<Source>("src")
                .Select(source => new Dest { Value = source.Value * 10 });

            var second = first
                .Select(dest => new Dest { Value = dest.Value * 5 });

            var expression = context.GetExpression(second, first);

            expression.Parameters.ShouldBeEmpty();
            ((ConstantExpression)((MethodCallExpression)expression.Body).Arguments[0]).Value.ShouldBe(first);
        }

        [Fact]
        public void same_expression_should_reuse_quary()
        {
            var context = new MappingContext();

            var source = context
                .CreateSource<Source>("src");

            var first = source.Select(x => new Dest { Value = x.Value * 10 });

            var second = source.Select(x => new Dest { Value = x.Value * 10 });

            first.ShouldBe(second);
        }

        [Fact]
        public void check_subset_from_reuse_quary()
        {
            var context = new MappingContext();

            var source = context
                .CreateSource<Source>("src");

            var first = source.Select(x => new Dest { Value = x.Value * 10 });

            var second = source
                .Select(x => new Dest { Value = x.Value * 10 })
                .Select(x => x.Value / 10);

            context.IsSubset(second, first).ShouldBeTrue();
        }

        private class Source
        {
            public int Value { get; set; }
        }

        private class Dest
        {
            public int Value { get; set; }
        }
    }
}
