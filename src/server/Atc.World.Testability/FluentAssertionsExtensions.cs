using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentAssertions.Collections;
using FluentAssertions.Equivalency;

namespace Atc.World.Tests
{
    public static class FluentAssertionsExtensions
    {
        public static AndConstraint<StringCollectionAssertions> BeStrictlyEquivalentTo(
            this StringCollectionAssertions assertions, 
            params string[] expectation)
        {
            return assertions.BeEquivalentTo(expectation, options => options.WithStrictOrdering());
        }

        public static AndConstraint<GenericCollectionAssertions<T>> BeStrictlyEquivalentTo<T>(
            this GenericCollectionAssertions<T> assertions, 
            params T[] expectation)
        {
            return assertions.BeEquivalentTo(expectation, options => options.WithStrictOrdering());
        }
    }
}