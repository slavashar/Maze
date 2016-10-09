using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Maze.Facts
{
    public class ExpressionComparerFacts
    {
        [Fact]
        private void get_hash_from_null()
        {
            ExpressionComparer.Default.GetHashCode(null).ShouldEqual(0);
        }

        [Fact]
        public void compare_parameters()
        {
            ExpressionComparer.Default.Compare(Expression.Parameter(typeof(string), "str"), Expression.Parameter(typeof(string), "str")).ShouldEqual(0);

            ExpressionComparer.Default.Compare(Expression.Parameter(typeof(string), "str"), Expression.Parameter(typeof(string), "value")).ShouldEqual(-1);

            ExpressionComparer.Default.Compare(Expression.Parameter(typeof(string), "str"), Expression.Parameter(typeof(int), "str")).ShouldEqual(1);
        }

        [Fact]
        public void equal_parameters()
        {
            ExpressionComparer.Default.Equals(Expression.Parameter(typeof(string), "str"), Expression.Parameter(typeof(string), "str")).ShouldBeTrue();

            ExpressionComparer.Default.Equals(Expression.Parameter(typeof(string), "str"), Expression.Parameter(typeof(string), "value")).ShouldBeFalse();

            ExpressionComparer.Default.Equals(Expression.Parameter(typeof(string), "str"), Expression.Parameter(typeof(int), "str")).ShouldBeFalse();
        }

        [Fact]
        public void get_hashcode_from_parameters()
        {
            var result = ExpressionComparer.Default.GetHashCode(Expression.Parameter(typeof(string), "str"));

            ExpressionComparer.Default.GetHashCode(Expression.Parameter(typeof(string), "str")).ShouldEqual(result);

            ExpressionComparer.Default.GetHashCode(Expression.Parameter(typeof(string), "value")).ShouldNotEqual(result);

            ExpressionComparer.Default.GetHashCode(Expression.Parameter(typeof(int), "str")).ShouldNotEqual(result);
        }

        [Fact]
        public void get_hashcode_from_lambda()
        {
            var param = Expression.Parameter(typeof(IQueryable<int>), "src");

            var method = new Func<IQueryable<int>, double>(Queryable.Average).Method;

            var lambda = Expression.Lambda(Expression.Call(method, param), param);

            var result = ExpressionComparer.Default.GetHashCode(lambda);

            result.ShouldNotEqual(0);
        }

        [Fact]
        public void compare_member_access()
        {
            var comparer = ExpressionComparer.Default;

            var expression = Expression.Property(Expression.Parameter(typeof(DateTime)), "Day");

            comparer.Compare(expression, Expression.Property(Expression.Parameter(typeof(DateTime)), "Day")).ShouldEqual(0);

            comparer.Compare(expression, Expression.Property(Expression.Parameter(typeof(DateTime)), "Hour")).ShouldNotEqual(0);

            comparer.GetHashCode(expression).ShouldNotEqual(comparer.GetHashCode(Expression.Property(Expression.Parameter(typeof(DateTime)), "Hour")));
        }
    }
}
