using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Maze.Facts
{
    public class TypeFactoryFacts
    {
        [Fact]
        public void create_a_type()
        {
            var factory = new TypeFactory();

            var type = factory.BuildType(new Dictionary<string, Type>
            {
                ["Text"] = typeof(string)
            });

            type.GetProperties().Single().Name.ShouldEqual("Text");
            type.GetProperties().Single().PropertyType.ShouldEqual(typeof(string));
        }

        [Fact]
        public void create_multiple_classes()
        {
            var factory = new TypeFactory();

            var type1 = factory.BuildType(new Dictionary<string, Type>
            {
                ["Text"] = typeof(string)
            });

            var type2 = factory.BuildType(new Dictionary<string, Type>
            {
                ["Number"] = typeof(int)
            });
        }

        [Fact]
        public void create_multiple_factories()
        {
            var factory1 = new TypeFactory();

            var type1 = factory1.BuildType(new Dictionary<string, Type>
            {
                ["Text"] = typeof(string)
            });

            var factory2 = new TypeFactory();

            var type2 = factory2.BuildType(new Dictionary<string, Type>
            {
                ["Number"] = typeof(int)
            });
        }

        [Fact]
        public void create_instance_dynamic_type()
        {
            var factory = new TypeFactory();

            var type = factory.BuildType(new Dictionary<string, Type>
            {
                ["Text"] = typeof(string),
                ["Number"] = typeof(int)
            });

            var result = Activator.CreateInstance(type, new object[] { "txt", 1 });

            ((string)((dynamic)result).Text).ShouldEqual("txt");
            ((int)((dynamic)result).Number).ShouldEqual(1);
        }

        [Fact]
        public void create_new_expression()
        {
            var factory = new TypeFactory();

            var param = Expression.Parameter(typeof(string), "src");

            var result = factory.CreateProxy(ImmutableList.Create<Expression>(param), ImmutableList.Create("Text"));

            result.Members.Single().Name.ShouldEqual("Text");
            result.Arguments.Single().ShouldBe(param);
        }

        [Fact]
        public void execute_new_expression()
        {
            var factory = new TypeFactory();

            var param = Expression.Parameter(typeof(string), "src");

            var expression = factory.CreateProxy(ImmutableList.Create<Expression>(param, Expression.Constant(1)), ImmutableList.Create("Text", "Number"));

            var lambda = Expression.Lambda<Func<string, object>>(expression, param).Compile();

            var result = lambda.Invoke("txt");

            ((string)((dynamic)result).Text).ShouldEqual("txt");
            ((int)((dynamic)result).Number).ShouldEqual(1);
        }
    }
}
