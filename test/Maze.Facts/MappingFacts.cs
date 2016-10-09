using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Maze;
using Maze.Facts;
using Maze.Mappings;

using Xunit;

namespace Maze.Facts
{
    public class MappingFacts
    {
        [Fact]
        public void create_enumerable_mapping()
        {
            var source = new int[0];

            var mapping = Engine.Source(source);

            var enumMapping = mapping.Instance.ShouldBeType<EnumerableMapping<int>>();

            mapping.Mappings.ShouldEqual(enumMapping);
            mapping.Components.ShouldBeEmpty();

            enumMapping.Enumerable.ShouldEqual(source);
            enumMapping.SourceMappings.ShouldBeEmpty();
        }

        [Fact]
        public void create_expression_mapping()
        {
            var mapping = Engine.Map((IQueryable<Source> src) => from x in src select new Dest { Value = x.Value * 10 });

            var exprMapping = mapping.Instance.ShouldBeType<ExpressionMapping<Dest>>();

            mapping.Mappings.ShouldEqual(exprMapping);
            mapping.Components.ShouldBeEmpty();

            exprMapping.Expression.ShouldEqualExpression<Func<IQueryable<Source>, IQueryable<Dest>>>(src => from x in src select new Dest { Value = x.Value * 10 });
            exprMapping.SourceMappings.Values.ShouldEqual(new AnonymousMapping<Source>());
        }

        [Fact]
        public void create_expression_mapping_from_component()
        {
            var mapping = Engine.Map((IQueryable<Source> src) => new TestMapping(src).Result);

            var exprMapping = mapping.Instance.ShouldBeType<ExpressionMapping<Dest>>();

            mapping.Mappings.ShouldEqual(exprMapping);
            mapping.Components.ShouldBeEmpty();

            exprMapping.Expression.ShouldEqualExpression<Func<IQueryable<Source>, IQueryable<Dest>>>(src => from x in src select new Dest { Value = x.Value });
            exprMapping.SourceMappings.Values.ShouldEqual(new AnonymousMapping<Source>());
        }

        [Fact]
        public void create_expression_mapping_from_source_singaleton_mapping()
        {
            var sourceMapping = Engine.Map((IQueryable<Source> src) => from x in src select new Dest { Value = x.Value * 10 });

            var mapping = sourceMapping.Map(src => from x in src select x.Value);

            var exprMapping = mapping.Instance.ShouldBeType<ExpressionMapping<int>>();

            mapping.Mappings.ShouldEqual(sourceMapping.Instance, exprMapping);
            mapping.Components.ShouldBeEmpty();

            exprMapping.Expression.ShouldEqualExpression<Func<IQueryable<Dest>, IQueryable<int>>>(src => from x in src select x.Value);
            exprMapping.SourceMappings.Values.ShouldEqual(sourceMapping.Instance);
        }

        [Fact]
        public void create_anonymous_component_with_enumerable_mapping()
        {
            var source = new int[0];

            var mapping = Engine.CreateComponent(() => new
            {
                Items = source.AsQueryable()
            });

            var enumMapping = mapping.Instance.Mappings["Items"].ShouldBeType<EnumerableMapping<int>>();

            mapping.Mappings.ShouldEqual(enumMapping);
            mapping.Components.ShouldEqual(mapping.Instance);

            enumMapping.Enumerable.ShouldBe(source);
            enumMapping.SourceMappings.ShouldBeEmpty();
        }

        [Fact]
        public void create_anonymous_component_with_expression_mapping()
        {
            var mapping = Engine.MapComponent((IQueryable<int> src) => new
            {
                Items = src
            });

            var exprMapping = mapping.Instance.Mappings["Items"].ShouldBeType<ExpressionMapping<int>>();

            mapping.Mappings.ShouldEqual(exprMapping);
            mapping.Components.ShouldEqual(mapping.Instance);

            exprMapping.Expression.ShouldEqualExpression<Func<IQueryable<int>, IQueryable<int>>>(src => src);
            exprMapping.SourceMappings.Values.ShouldEqual(new AnonymousMapping<int>());
        }

        [Fact]
        public void create_anonymous_component_from_source_singaleton_mapping()
        {
            var sourceMapping = Engine.Map((IQueryable<Source> src) => from x in src select new Dest { Value = x.Value * 10 });

            var mapping = sourceMapping.MapComponent(src => new
            {
                Items = from x in src select x.Value
            });

            mapping.Instance.Mappings.Count.ShouldEqual(1);
            var exprMapping = mapping.Instance.Mappings["Items"].ShouldBeType<ExpressionMapping<int>>();

            mapping.Mappings.ShouldEqual(sourceMapping.Instance, exprMapping);
            mapping.Components.ShouldEqual(mapping.Instance);

            exprMapping.Expression.ShouldEqualExpression<Func<IQueryable<Dest>, IQueryable<int>>>(src => from x in src select x.Value);
            exprMapping.SourceMappings.Values.ShouldEqual(sourceMapping.Instance);
        }

        [Fact]
        public void create_anonymous_component_from_source_component()
        {
            var sourceMapping = Engine.CreateComponent<TestMapping>();

            var mapping = sourceMapping.MapComponent(src => new
            {
                Items = from x in src.Result select x.Value
            });

            mapping.Instance.Mappings.Count.ShouldEqual(1);
            var exprMapping = mapping.Instance.Mappings["Items"].ShouldBeType<ExpressionMapping<int>>();
            
            mapping.Mappings.ShouldEqual(sourceMapping.Instance.Mappings["Result"], exprMapping);
            mapping.Components.ShouldEqual(sourceMapping.Instance, mapping.Instance);

            exprMapping.Expression.ShouldEqualExpression<Func<IQueryable<Dest>, IQueryable<int>>>(src_Result => src_Result.Select(x => x.Value));
            exprMapping.SourceMappings.Values.ShouldEqual(sourceMapping.Instance.Mappings["Result"]);
        }

        [Fact]
        public void create_concrete_component()
        {
            var mapping = Engine.MapComponent((IQueryable<Source> src) => new TestMapping(src));

            mapping.Instance.Mappings.Count.ShouldEqual(1);
            var exprMapping = mapping.Instance.Mappings["Result"].ShouldBeType<ExpressionMapping<Dest>>();

            mapping.Mappings.ShouldEqual(mapping.Instance.Mappings.Values);
            mapping.Components.ShouldEqual(mapping.Instance);

            exprMapping.Expression.ShouldEqualExpression<Func<IQueryable<Source>, IQueryable<Dest>>>(src => from x in src select new Dest { Value = x.Value });
            exprMapping.SourceMappings.Values.ShouldEqual(new AnonymousMapping<Source>());
        }

        [Fact]
        public void create_concrete_component_from_source_singaleton_mapping()
        {
            var sourceMapping = Engine.Map((IQueryable<Source> src) => src);

            var mapping = sourceMapping.MapComponent(src => new TestMapping(src));

            mapping.Instance.Mappings.Count.ShouldEqual(1);
            var expressionMapping = mapping.Instance.Mappings["Result"].ShouldBeType<ExpressionMapping<Dest>>();

            mapping.Mappings.ShouldEqual(sourceMapping.Instance, mapping.Instance.Mappings.Values.Single());
            mapping.Components.ShouldEqual(mapping.Instance);

            expressionMapping.Expression.ShouldEqualExpression<Func<IQueryable<Source>, IQueryable<Dest>>>(src => from x in src select new Dest { Value = x.Value });
            expressionMapping.SourceMappings.Values.ShouldEqual(sourceMapping.Instance);
        }

        [Fact]
        public void create_concrete_component_dynamically()
        {
            var mapping = Engine.CreateComponent<TestMapping>();

            mapping.Instance.Mappings.Count.ShouldEqual(1);
            var exprMapping = mapping.Instance.Mappings["Result"].ShouldBeType<ExpressionMapping<Dest>>();
            
            mapping.Mappings.ShouldEqual(exprMapping);
            mapping.Components.ShouldEqual(mapping.Instance);

            exprMapping.Expression.ShouldEqualExpression<Func<IQueryable<Source>, IQueryable<Dest>>>(input => from x in input select new Dest { Value = x.Value });
            exprMapping.SourceMappings.Values.ShouldEqual(new AnonymousMapping<Source>());
        }

        [Fact]
        public void create_concrete_component_dynamically_with_multiple_sources()
        {
            var mapping = Engine.CreateComponent<MultipleSourceTestMapping>();

            mapping.Instance.Mappings.Count.ShouldEqual(1);
            var exprMapping = mapping.Instance.Mappings["Result"].ShouldBeType<ExpressionMapping<int>>();

            mapping.Mappings.ShouldEqual(mapping.Instance.Mappings.Values.ToArray());
            mapping.Components.ShouldEqual(mapping.Instance);

            exprMapping.Expression.ShouldEqualExpression<Func<IQueryable<Source>, IQueryable<AnotherSource>, IQueryable<int>>>(
                (input, anotherSourceInput) => input.Select(x => x.Value).Concat(anotherSourceInput.Select(x => x.Key)));
            exprMapping.SourceMappings.ShouldEqual(new Dictionary<ParameterExpression, IMapping>
            {
                [exprMapping.Expression.Parameters[0]] = new AnonymousMapping<Source>(),
                [exprMapping.Expression.Parameters[1]] = new AnonymousMapping<AnotherSource>()
            });
        }

        [Fact]
        public void create_derived_concrete_component_dynamically()
        {
            var mapping = Engine.CreateComponent<DerivedTestMapping>();

            mapping.Instance.Mappings.Count.ShouldEqual(2);
            mapping.Instance.Mappings["Result"].ShouldBeType<ExpressionMapping<Dest>>().Expression
                .ShouldEqualExpression<Func<IQueryable<Source>, IQueryable<Dest>>>(input => from x in input select new Dest { Value = x.Value });
            
            mapping.Mappings.ShouldEqual(mapping.Instance.Mappings.Values.ToArray());
            mapping.Components.ShouldEqual(mapping.Instance);

            var expressionMapping = mapping.Instance.Mappings["Values"].ShouldBeType<ExpressionMapping<int>>();

            expressionMapping.Expression.ShouldEqualExpression<Func<IQueryable<Dest>, IQueryable<int>>>(this_Result => from x in this_Result select x.Value);
            expressionMapping.SourceMappings.Values.ShouldEqual(mapping.Instance.Mappings["Result"]);
        }

        [Fact]
        public void create_complex_derived_concrete_component_dynamically()
        {
            var mapping = Engine.CreateComponent<ComplexDerivedTestMapping>();

            mapping.Instance.Mappings.Count.ShouldEqual(4);

            mapping.Instance.Mappings["Result"].ShouldBeType<ExpressionMapping<Dest>>().Expression
                .ShouldEqualExpression<Func<IQueryable<Source>, IQueryable<Dest>>>(sourceInput => from x in sourceInput select new Dest { Value = x.Value });

            mapping.Instance.Mappings["Values"].ShouldBeType<ExpressionMapping<int>>().Expression
                .ShouldEqualExpression<Func<IQueryable<Dest>, IQueryable<int>>>(this_Result => from x in this_Result select x.Value);

            mapping.Instance.Mappings["Source"].ShouldBeType<ExpressionMapping<AnotherSource>>().Expression
                .ShouldEqualExpression<Func<IQueryable<AnotherSource>, IQueryable<AnotherSource>>>(anotherSourceInput => anotherSourceInput);

            mapping.Mappings.ShouldEqual(
                mapping.Instance.Mappings["Result"],
                mapping.Instance.Mappings["Values"],
                mapping.Instance.Mappings["Source"],
                mapping.Instance.Mappings["Total"]);
            mapping.Components.ShouldEqual(mapping.Instance);

            var exprMapping = mapping.Instance.Mappings["Total"].ShouldBeType<ExpressionMapping<string>>();

            exprMapping.Expression.ShouldEqualExpression<Func<IQueryable<AnotherSource>, IQueryable<int>, IQueryable<string>, IQueryable<string>>>(
                (this_Source, this_Values, thirdSourceInput) => from x in this_Values
                                                                join r1 in this_Source on x equals r1.Key
                                                                join r2 in thirdSourceInput on r1.Key.ToString() equals r2
                                                                where !string.IsNullOrEmpty(r2)
                                                                select r2);

            exprMapping.SourceMappings.ShouldEqual(new Dictionary<ParameterExpression, IMapping>
            {
                [exprMapping.Expression.Parameters[0]] = mapping.Instance.Mappings["Source"],
                [exprMapping.Expression.Parameters[1]] = mapping.Instance.Mappings["Values"],
                [exprMapping.Expression.Parameters[2]] = new AnonymousMapping<string>(),
            });
        }

        [Fact]
        public void create_concrete_component_from_source_singleton_mapping()
        {
            var sourceMapping = Engine.Map((IQueryable<int> src) => from x in src select new Source { Value = x * 10 });

            var mapping = sourceMapping.MapComponent(src => new TestMapping(src));

            mapping.Instance.Mappings.Count.ShouldEqual(1);
            var expressionMapping = mapping.Instance.Mappings["Result"].ShouldBeType<ExpressionMapping<Dest>>();
            
            mapping.Mappings.ShouldEqual(sourceMapping.Instance, mapping.Instance.Mappings.Values.Single());
            mapping.Components.ShouldEqual(mapping.Instance);

            expressionMapping.Expression.ShouldEqualExpression<Func<IQueryable<Source>, IQueryable<Dest>>>(src => from x in src select new Dest { Value = x.Value });
            expressionMapping.SourceMappings.Values.ShouldEqual(sourceMapping.Instance);
        }

        [Fact]
        public void create_nested_concrete_component_dynamically()
        {
            var mapping = Engine.CreateComponent<NestedMapping>();

            mapping.Instance.Mappings.Count.ShouldEqual(1);
            var exprMapping = mapping.Instance.Mappings["Child.Result"].ShouldBeType<ExpressionMapping<Dest>>();

            mapping.Mappings.ShouldEqual(exprMapping);
            mapping.Components.ShouldEqual(mapping.Instance);

            exprMapping.Expression.ShouldEqualExpression<Func<IQueryable<Source>, IQueryable<Dest>>>(input => from x in input select new Dest { Value = x.Value });
            exprMapping.SourceMappings.Values.ShouldEqual(new AnonymousMapping<Source>());
        }

        [Fact]
        public void create_combined_mapping()
        {
            var first = Engine.MapComponent((IQueryable<int> src) => new { Src = src });            
            var second = Engine.MapComponent((IQueryable<string> src) => new { Src = src });

            var mapping = Engine.Combine(first, second);
            
            mapping.Mappings.ShouldEqual(first.Instance.Mappings["Src"], second.Instance.Mappings["Src"]);
            mapping.Components.ShouldEqual(first.Instance, second.Instance, mapping.Instance);

            mapping.Instance.Mappings["First.Src"].ShouldBe(first.Instance.Mappings["Src"]);
            mapping.Instance.Mappings["Second.Src"].ShouldBe(second.Instance.Mappings["Src"]);
        }

        [Fact]
        public void create_anonymous_component_from_combined_mapping()
        {
            var first = Engine.MapComponent((IQueryable<int> src) => new { Src = src });
            var second = Engine.MapComponent((IQueryable<string> src) => new { Src = src });
            var combined = Engine.Combine(first, second);

            var mapping = Engine.MapComponent(combined, src => new { Result = src.First.Src });

            var exprMapping = mapping.Instance.Mappings["Result"].ShouldBeType<ExpressionMapping<int>>();

            mapping.Mappings.ShouldEqual(first.Instance.Mappings["Src"], second.Instance.Mappings["Src"], exprMapping);
            mapping.Components.ShouldEqual(first.Instance, second.Instance, combined.Instance, mapping.Instance);

            exprMapping.Expression.ShouldEqualExpression<Func<IQueryable<int>, IQueryable<int>>>(src_First_Src => src_First_Src);
            exprMapping.SourceMappings.Values.ShouldEqual(first.Instance.Mappings["Src"]);
        }

        [Fact]
        public void get_singleton_mapping()
        {
            var first = Engine
                .Map((IQueryable<int> src) => from x in src select new Source { Value = x * 10 });

            var second = first.Map(src => from x in src select x.ToString());

            var result = second.Get<Source>();

            result.Instance.ShouldBe(first.Instance);

            result.Mappings.ShouldBe(second.Mappings);
            result.Components.ShouldBe(second.Components);
        }

        [Fact]
        public void get_same_singleton_mapping()
        {
            var first = Engine
                .Map((IQueryable<int> src) => from x in src select new Source { Value = x * 10 });

            var second = first.Map(src => from x in src select x.ToString());

            var result = second.Get<string>();

            result.ShouldBe(second);
        }

        private class Source
        {
            public int Value { get; set; }
        }

        private class AnotherSource
        {
            public int Key { get; set; }

            public decimal Total { get; set; }
        }

        private class Dest
        {
            public int Value { get; set; }
        }

        private class TestMapping
        {
            private readonly IQueryable<Source> input;

            public TestMapping(IQueryable<Source> input)
            {
                this.input = input;
            }

            public IQueryable<Dest> Result
            {
                get { return from x in this.input select new Dest { Value = x.Value }; }
            }
        }

        private class MultipleSourceTestMapping
        {
            private readonly IQueryable<Source> input;
            private readonly IQueryable<AnotherSource> anotherSourceInput;

            public MultipleSourceTestMapping(IQueryable<Source> input, IQueryable<AnotherSource> anotherSourceInput)
            {
                this.input = input;
                this.anotherSourceInput = anotherSourceInput;
            }

            public IQueryable<int> Result
            {
                get { return this.input.Select(x => x.Value).Concat(this.anotherSourceInput.Select(x => x.Key)); }
            }
        }

        private class DerivedTestMapping : TestMapping
        {
            public DerivedTestMapping(IQueryable<Source> input) : base(input)
            {
            }

            public IQueryable<int> Values
            {
                get { return from x in this.Result select x.Value; }
            }
        }

        private class ComplexDerivedTestMapping : DerivedTestMapping
        {
            private readonly IQueryable<AnotherSource> anotherSourceInput;
            private readonly IQueryable<string> thirdSourceInput;

            public ComplexDerivedTestMapping(IQueryable<Source> sourceInput, IQueryable<AnotherSource> anotherSourceInput, IQueryable<string> thirdSourceInput)
                : base(sourceInput)
            {
                this.anotherSourceInput = anotherSourceInput;
                this.thirdSourceInput = thirdSourceInput;
            }

            public IQueryable<AnotherSource> Source
            {
                get { return this.anotherSourceInput; }
            }

            public IQueryable<string> Total
            {
                get { return from x in this.Values
                             join r1 in this.anotherSourceInput on x equals r1.Key
                             // this.anotherSourceInput would be replaced with this.Source as they got the same expression
                             join r2 in this.thirdSourceInput on r1.Key.ToString() equals r2
                             where !string.IsNullOrEmpty(r2)
                             select r2; }
            }
        }

        private class NestedMapping
        {
            private readonly IQueryable<Source> input;

            public NestedMapping(IQueryable<Source> input)
            {
                this.input = input;
            }

            public TestMapping Child
            {
                get { return new TestMapping(this.input); }
            }
        }
    }
}
