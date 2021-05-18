using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;

namespace Atc.Data.Sources.Tests
{
    [TestFixture]
    public class DictionaryExtensionsTests
    {
        [Test]
        public void MakeMinimalUniqueStringKey_ReturnAsIsIfUnique()
        {
            var dictionary = new Dictionary<string, int>() {
                { "abc", 123 }
            };

            var key = dictionary.MakeMinimalUniqueStringKey("def");

            key.Should().Be("def");
        }

        [Test]
        public void MakeMinimalUniqueStringKey_Append2ToExistingKey()
        {
            var dictionary = new Dictionary<string, int>() {
                { "abc", 123 }
            };

            var key = dictionary.MakeMinimalUniqueStringKey("abc");

            key.Should().Be("abc2");
        }

        [Test]
        public void MakeMinimalUniqueStringKey_FindUniqueSuffix()
        {
            var dictionary = new Dictionary<string, int>() {
                { "abc", 12300 },
                { "abc2", 12302 },
            };

            var key = dictionary.MakeMinimalUniqueStringKey("abc");
            key.Should().Be("abc3");

            for (int suffix = 3; suffix < 100; suffix++)
            {
                dictionary.Add($"abc{suffix}", 12300 + suffix);

                var nextUniqueKey = dictionary.MakeMinimalUniqueStringKey("abc");
                nextUniqueKey.Should().Be($"abc{suffix + 1}");
            }
        }
    }
}