using System;
using Atc.Data.Primitives;
using FluentAssertions;
using NUnit.Framework;

namespace Atc.Data.Tests.Primitives
{
    [TestFixture]
    public class DistanceTests
    {
        [Test]
        public void InitFromFeet()
        {
            var distance = Distance.FromFeet(123.45f);

            distance.Value.Should().Be(123.45f);
            distance.Unit.Should().Be(LengthUnit.Feet);
        }

        [Test]
        public void CanConvertUnits()
        {
            var distance = Distance.FromFeet(1000);

            distance.GetValueInUnit(LengthUnit.Feet).Should().Be(1000);
            distance.GetValueInUnit(LengthUnit.Mile).Should().BeApproximately(0.189394f, Distance.Precision);
            distance.GetValueInUnit(LengthUnit.Meter).Should().BeApproximately(304.8f, Distance.Precision);
            distance.GetValueInUnit(LengthUnit.Kilometer).Should().BeApproximately(0.3048f, Distance.Precision);
            distance.GetValueInUnit(LengthUnit.NauticalMile).Should().BeApproximately(0.164579f, Distance.Precision);
        }

        [Test]
        public void CanConvertUnits_ShortcutProps()
        {
            var distance = Distance.FromFeet(1000);

            distance.Feet.Should().Be(1000);
            distance.Miles.Should().BeApproximately(0.189394f, Distance.Precision);
            distance.Meters.Should().BeApproximately(304.8f, Distance.Precision);
            distance.Kilometers.Should().BeApproximately(0.3048f, Distance.Precision);
            distance.NauticalMiles.Should().BeApproximately(0.164579f, Distance.Precision);
        }

        [Test]
        public void Equality_SameValueAndUnit_Equal()
        {
            var distance = Distance.FromNauticalMiles(12.34f);
            var distance2 = Distance.FromNauticalMiles(12.34f);

            distance.Equals(distance2).Should().BeTrue();
            (distance == distance2).Should().BeTrue();
            (distance != distance2).Should().BeFalse();
        }

        [Test]
        public void Equality_SameUnitButDifferentValue_NotEqual()
        {
            var distance1 = Distance.FromNauticalMiles(12.34f);
            var distance2 = Distance.FromNauticalMiles(56.78f);

            distance1.Equals(distance2).Should().BeFalse();
            (distance1 == distance2).Should().BeFalse();
            (distance1 != distance2).Should().BeTrue();
        }

        [Test]
        public void Equality_SameValueButDifferentUnit_NotEqual()
        {
            var distance1 = Distance.FromMiles(12.34f);
            var distance2 = Distance.FromNauticalMiles(12.34f);

            distance1.Equals(distance2).Should().BeFalse();
            (distance1 == distance2).Should().BeFalse();
            (distance1 != distance2).Should().BeTrue();
        }

        [Test]
        public void Equality_EquivalentValueInDifferentUnit_Equal()
        {
            var distance1 = Distance.FromMeters(15000);
            var distance2 = Distance.FromKilometers(15);

            distance1.Equals(distance2).Should().BeTrue();
            (distance1 == distance2).Should().BeTrue();
            (distance1 != distance2).Should().BeFalse();
        }

        [Test]
        public void Equality_DifferentValueInDifferentUnit_NotEqual()
        {
            var distance1 = Distance.FromMiles(12.34f);
            var distance2 = Distance.FromNauticalMiles(56.78f);

            distance1.Equals(distance2).Should().BeFalse();
            (distance1 == distance2).Should().BeFalse();
            (distance1 != distance2).Should().BeTrue();
        }
    }
}
