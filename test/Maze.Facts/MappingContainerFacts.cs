using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using Maze.Mappings;
using Moq;
using Xunit;

namespace Maze.Facts
{
    public class MappingContainerFacts
    {
        [Fact]
        public void create_an_empty_container()
        {
            var container = MappingContainer.Create();

            container.ShouldNotBeNull();
        }

        [Fact]
        public void add_mapping()
        {
            var mapping = new Mock<IMapping<object>>();

            mapping.SetupGet(x => x.SourceMappings).Returns(ImmutableDictionary<ParameterExpression, IMapping>.Empty);

            var container = MappingContainer
                .Create()
                .Add(mapping.Object);

            container.Mappings.ShouldEqual(mapping.Object);
            container.ExecutionQueue.ShouldEqual(mapping.Object);
        }

        [Fact]
        public void add_the_same_mapping()
        {
            var mapping = new Mock<IMapping<object>>();

            mapping.SetupGet(x => x.SourceMappings).Returns(ImmutableDictionary<ParameterExpression, IMapping>.Empty);

            var container = MappingContainer
                .Create()
                .Add(mapping.Object)
                .Add(mapping.Object);

            container.Mappings.ShouldEqual(mapping.Object);
            container.ExecutionQueue.ShouldEqual(mapping.Object);
        }

        [Fact]
        public void add_detached_mapping()
        {
            var anonymousMapping = new Mock<IMapping<object>>();

            anonymousMapping.SetupGet(x => x.SourceMappings).Returns(ImmutableDictionary<ParameterExpression, IMapping>
                .Empty.Add(Expression.Parameter(typeof(int)), new AnonymousMapping<int>()));

            var container = MappingContainer
                .Create()
                .Add(anonymousMapping.Object);

            container.Mappings.ShouldEqual(anonymousMapping.Object);
            container.ExecutionQueue.ShouldBeEmpty();
        }

        [Fact]
        public void add_dependent_mapping()
        {
            var mapping = new Mock<IMapping<object>>();
            mapping.SetupGet(x => x.SourceMappings).Returns(ImmutableDictionary<ParameterExpression, IMapping>.Empty);

            var dependentMapping = new Mock<IMapping<object>>();
            dependentMapping.SetupGet(x => x.SourceMappings).Returns(ImmutableDictionary<ParameterExpression, IMapping>
                .Empty.Add(Expression.Parameter(typeof(object)), mapping.Object));

            var container = MappingContainer
                .Create()
                .Add(mapping.Object)
                .Add(dependentMapping.Object);

            container.Mappings.ShouldEqual(mapping.Object, dependentMapping.Object);
            container.ExecutionQueue.ShouldEqual(mapping.Object, dependentMapping.Object);
        }

        [Fact]
        public void add_dependent_mapping_to_detached_mapping()
        {
            var anonymousMapping = new Mock<IMapping<object>>();
            anonymousMapping.SetupGet(x => x.SourceMappings).Returns(ImmutableDictionary<ParameterExpression, IMapping>
                .Empty.Add(Expression.Parameter(typeof(int)), new AnonymousMapping<int>()));

            var dependentMapping = new Mock<IMapping<object>>();
            dependentMapping.SetupGet(x => x.SourceMappings).Returns(ImmutableDictionary<ParameterExpression, IMapping>
                .Empty.Add(Expression.Parameter(typeof(object)), anonymousMapping.Object));

            var container = MappingContainer
                .Create()
                .Add(anonymousMapping.Object)
                .Add(dependentMapping.Object);

            container.Mappings.ShouldEqual(anonymousMapping.Object, dependentMapping.Object);
            container.ExecutionQueue.ShouldBeEmpty();
        }

        [Fact]
        public void add_anonymous_mapping()
        {
            var mapping = new Mock<IMapping<int>>();
            mapping.SetupGet(x => x.SourceMappings).Returns(ImmutableDictionary<ParameterExpression, IMapping>.Empty);

            var anonymousMapping = new Mock<IMapping<object>>();
            anonymousMapping.SetupGet(x => x.SourceMappings).Returns(ImmutableDictionary<ParameterExpression, IMapping>
                .Empty.Add(Expression.Parameter(typeof(int)), new AnonymousMapping<int>()));

            var container = MappingContainer
                .Create()
                .Add(mapping.Object)
                .Add(anonymousMapping.Object);

            container.Mappings.ShouldEqual(anonymousMapping.Object, mapping.Object);

            container.ExecutionQueue.Count().ShouldEqual(2);

            container.ExecutionQueue.ElementAt(0).ShouldBe(mapping.Object);
            var proxy = container.ExecutionQueue.ElementAt(1).ShouldBeType<ProxyMapping<object>>();

            proxy.Original.ShouldBe(anonymousMapping.Object);
            proxy.SourceMappings.Values.Single().ShouldBe(mapping.Object);
        }

        [Fact]
        public void add_ambiguous_anonymous_mapping()
        {
            var firstMapping = new Mock<IMapping<int>>();
            firstMapping.SetupGet(x => x.SourceMappings).Returns(ImmutableDictionary<ParameterExpression, IMapping>.Empty);

            var secondMapping = new Mock<IMapping<int>>();
            secondMapping.SetupGet(x => x.SourceMappings).Returns(ImmutableDictionary<ParameterExpression, IMapping>.Empty);

            var anonymousMapping = new Mock<IMapping<object>>();
            anonymousMapping.SetupGet(x => x.SourceMappings).Returns(ImmutableDictionary<ParameterExpression, IMapping>
                .Empty.Add(Expression.Parameter(typeof(int)), new AnonymousMapping<int>()));

            var container = MappingContainer
                .Create()
                .Add(firstMapping.Object)
                .Add(secondMapping.Object);

            var ex = Assert.Throws<InvalidOperationException>(() => container.Add(anonymousMapping.Object));

            ex.Message.ShouldContain("ambiguous source mapping", StringComparison.InvariantCultureIgnoreCase);
        }

        [Fact]
        public void add_missing_mapping()
        {
            var anonymousMapping = new Mock<IMapping<object>>();
            anonymousMapping.SetupGet(x => x.SourceMappings).Returns(ImmutableDictionary<ParameterExpression, IMapping>
                .Empty.Add(Expression.Parameter(typeof(int)), new AnonymousMapping<int>()));

            var mapping = new Mock<IMapping<int>>();
            mapping.SetupGet(x => x.SourceMappings).Returns(ImmutableDictionary<ParameterExpression, IMapping>.Empty);

            var container = MappingContainer
                .Create()
                .Add(anonymousMapping.Object)
                .Add(mapping.Object);

            container.Mappings.ShouldEqual(anonymousMapping.Object, mapping.Object);

            container.ExecutionQueue.Count().ShouldEqual(2);

            container.ExecutionQueue.ElementAt(0).ShouldBe(mapping.Object);
            var proxy = container.ExecutionQueue.ElementAt(1).ShouldBeType<ProxyMapping<object>>();

            proxy.Original.ShouldBe(anonymousMapping.Object);
            proxy.SourceMappings.Values.Single().ShouldBe(mapping.Object);
        }

        [Fact]
        public void add_missing_mapping_to_detached_mappings()
        {
            var anonymousMapping = new Mock<IMapping<object>>();
            anonymousMapping.SetupGet(x => x.SourceMappings).Returns(ImmutableDictionary<ParameterExpression, IMapping>
                .Empty.Add(Expression.Parameter(typeof(int)), new AnonymousMapping<int>()));

            var dependentMapping = new Mock<IMapping<object>>();
            dependentMapping.SetupGet(x => x.SourceMappings).Returns(ImmutableDictionary<ParameterExpression, IMapping>
                .Empty.Add(Expression.Parameter(typeof(object)), anonymousMapping.Object));

            var missingMapping = new Mock<IMapping<int>>();
            missingMapping.SetupGet(x => x.SourceMappings).Returns(ImmutableDictionary<ParameterExpression, IMapping>.Empty);

            var container = MappingContainer
                .Create()
                .Add(anonymousMapping.Object)
                .Add(dependentMapping.Object)
                .Add(missingMapping.Object);

            container.Mappings.ShouldEqual(anonymousMapping.Object, dependentMapping.Object, missingMapping.Object);

            container.ExecutionQueue.Count().ShouldEqual(3);

            container.ExecutionQueue.ElementAt(0).ShouldBe(missingMapping.Object);

            var proxy = container.ExecutionQueue.ElementAt(1).ShouldBeType<ProxyMapping<object>>();
            proxy.Original.ShouldBe(anonymousMapping.Object);
            proxy.SourceMappings.Values.Single().ShouldBe(missingMapping.Object);

            container.ExecutionQueue.ElementAt(2).ShouldBe(dependentMapping.Object);
        }

        [Fact]
        public void add_component_mapping()
        {
            var mapping = new Mock<IComponentMapping<object>>();

            var firstMapping = new Mock<IMapping<object>>();
            firstMapping.SetupGet(x => x.SourceMappings).Returns(ImmutableDictionary<ParameterExpression, IMapping>.Empty);

            var secondMapping = new Mock<IMapping<object>>();
            secondMapping.SetupGet(x => x.SourceMappings).Returns(ImmutableDictionary<ParameterExpression, IMapping>
                .Empty.Add(Expression.Parameter(typeof(object)), firstMapping.Object));

            mapping.SetupGet(x => x.Mappings).Returns(new Dictionary<string, IMapping>
            {
                { "First", firstMapping.Object },
                { "Second", secondMapping.Object },
            }.ToImmutableDictionary());

            var container = MappingContainer
                .Create()
                .Add(mapping.Object);

            container.Mappings.ShouldEqual(firstMapping.Object, secondMapping.Object);
            container.Components.ShouldEqual(mapping.Object);
            container.ExecutionQueue.ShouldEqual(firstMapping.Object, secondMapping.Object);
        }

        [Fact]
        public void add_the_same_component_mapping()
        {
            var mapping = new Mock<IComponentMapping<object>>();

            var firstMapping = new Mock<IMapping<object>>();
            firstMapping.SetupGet(x => x.SourceMappings).Returns(ImmutableDictionary<ParameterExpression, IMapping>.Empty);

            var secondMapping = new Mock<IMapping<object>>();
            secondMapping.SetupGet(x => x.SourceMappings).Returns(ImmutableDictionary<ParameterExpression, IMapping>
                .Empty.Add(Expression.Parameter(typeof(object)), firstMapping.Object));

            mapping.SetupGet(x => x.Mappings).Returns(new Dictionary<string, IMapping>
            {
                { "First", firstMapping.Object },
                { "Second", secondMapping.Object },
            }.ToImmutableDictionary());

            var container = MappingContainer
                .Create()
                .Add(mapping.Object)
                .Add(mapping.Object);

            container.Mappings.ShouldEqual(firstMapping.Object, secondMapping.Object);
            container.Components.ShouldEqual(mapping.Object);
            container.ExecutionQueue.ShouldEqual(firstMapping.Object, secondMapping.Object);
        }

        [Fact]
        public void merge_containers()
        {
            var firstMapping = new Mock<IMapping<int>>();
            firstMapping.SetupGet(x => x.SourceMappings).Returns(ImmutableDictionary<ParameterExpression, IMapping>.Empty);

            var secondMapping = new Mock<IMapping<object>>();
            secondMapping.SetupGet(x => x.SourceMappings).Returns(ImmutableDictionary<ParameterExpression, IMapping>
                .Empty.Add(Expression.Parameter(typeof(int)), new AnonymousMapping<int>()));

            var firstContainer = MappingContainer
                .Create()
                .Add(firstMapping.Object);

            var secondContainer = MappingContainer
                .Create()
                .Add(secondMapping.Object);

            var container = MappingContainer.Merge(firstContainer, secondContainer);

            container.Mappings.ShouldEqual(firstMapping.Object, secondMapping.Object);

            container.ExecutionQueue.Count().ShouldEqual(2);

            container.ExecutionQueue.ElementAt(0).ShouldBe(firstMapping.Object);
            var proxy = container.ExecutionQueue.ElementAt(1).ShouldBeType<ProxyMapping<object>>();

            proxy.Original.ShouldBe(secondMapping.Object);
            proxy.SourceMappings.Values.Single().ShouldBe(firstMapping.Object);
        }

        [Fact]
        public void merge_detached_container()
        {
            var secondMapping = new Mock<IMapping<object>>();
            secondMapping.SetupGet(x => x.SourceMappings).Returns(ImmutableDictionary<ParameterExpression, IMapping>
                .Empty.Add(Expression.Parameter(typeof(int)), new AnonymousMapping<int>()));

            var firstContainer = MappingContainer
                .Create();

            var secondContainer = MappingContainer
                .Create()
                .Add(secondMapping.Object);

            var container = MappingContainer.Merge(firstContainer, secondContainer);

            container.Mappings.ShouldEqual(secondMapping.Object);
            container.ExecutionQueue.ShouldBeEmpty();
        }

        [Fact]
        public void merge_the_same_containers()
        {
            var firstMapping = new Mock<IMapping<int>>();
            firstMapping.SetupGet(x => x.SourceMappings).Returns(ImmutableDictionary<ParameterExpression, IMapping>.Empty);

            var firstContainer = MappingContainer
                .Create()
                .Add(firstMapping.Object);

            var container = MappingContainer.Merge(firstContainer, firstContainer);

            container.Mappings.ShouldEqual(firstMapping.Object);
            container.ExecutionQueue.ShouldEqual(firstMapping.Object);
        }
    }
}
