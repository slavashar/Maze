using System;
using System.Collections;
using System.Collections.Generic;
using Xunit.Sdk;

namespace Xunit
{
    [System.Diagnostics.DebuggerStepThrough]
    internal static class ShouldExtension
    {
        public static void ShouldBe<T>(this T actual, T expected)
        {
            Assert.Same(expected, actual);
        }

        public static void ShouldEqual<T>(this IEnumerable<T> actual, IEnumerable<T> expected)
        {
            Assert.Equal(expected, actual);
        }

        public static void ShouldEqual<TKey, TValue>(this IDictionary<TKey, TValue> actual, IDictionary<TKey, TValue> expected)
        {
            if (expected.Count != actual.Count)
            {
                throw new CollectionException(actual, expected.Count, actual.Count);
            }

            var index = -1;
            foreach (var key in expected.Keys)
            {
                TValue actualValue;
                index++;

                if (!actual.TryGetValue(key, out actualValue))
                {
                    throw new ContainsException(key, actual.Keys);
                }

                try
                {
                    Assert.Equal(expected[key], actualValue);
                }
                catch (Exception ex)
                {
                    throw new CollectionException(actual, expected.Count, actual.Count, index, ex);
                }
            }
        }

        public static void ShouldEqual<T>(this IEnumerable<T> actual, params T[] expected)
        {
            Assert.Equal(expected, actual);
        }

        public static void ShouldEqual<T>(this ISet<T> actual, params T[] expected)
        {
            if (expected.Length != actual.Count)
            {
                throw new CollectionException(actual, expected.Length, actual.Count);
            }

            var index = -1;
            foreach (var item in expected)
            {
                index++;

                if (!actual.Contains(item))
                {
                    throw new ContainsException(item, item);
                }
            }
        }

        public static void ShouldEqual<T>(this T actual, T expected)
        {
            Assert.Equal(expected, actual);
        }

        public static void ShouldNotEqual<T>(this T actual, T notExpected)
        {
            Assert.NotEqual(notExpected, actual);
        }

        public static void ShouldStartsWith(this string actual, string expectedStart)
        {
            Assert.StartsWith(expectedStart, actual);
        }

        public static void ShouldBeIgnoringWhitespace(this string actual, string expected)
        {
            Assert.Equal(
                System.Text.RegularExpressions.Regex.Replace(expected, @"\s+", " ").Trim(), 
                System.Text.RegularExpressions.Regex.Replace(actual, @"\s+", " ").Trim());
        }

        public static void ShouldContain(this string actualString, string expectedSubstring)
        {
            Assert.Contains(expectedSubstring, actualString);
        }

        public static void ShouldContain(this string actualString, string expectedSubstring, StringComparison comparisonType)
        {
            Assert.Contains(expectedSubstring, actualString, comparisonType);
        }

        public static void ShouldNotBe<T>(this T actual, T expected)
        {
            Assert.NotEqual(expected, actual);
        }

        public static void ShouldBeNull(this object @object)
        {
            Assert.Null(@object);
        }

        public static void ShouldNotBeNull(this object @object)
        {
            Assert.NotNull(@object);
        }

        public static void ShouldBeTrue(this bool condition)
        {
            Assert.True(condition);
        }

        public static void ShouldBeFalse(this bool condition)
        {
            Assert.False(condition);
        }

        public static T ShouldBeType<T>(this object @object)
        {
            Assert.IsType<T>(@object);

            return (T)@object;
        }

        public static void ShouldBeEmpty(this IEnumerable collection)
        {
            Assert.Empty(collection);
        }
    }
}
