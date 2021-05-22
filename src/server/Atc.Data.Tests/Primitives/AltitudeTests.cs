using System;
using Atc.Data.Primitives;
using FluentAssertions;
using NUnit.Framework;

namespace Atc.Data.Tests.Primitives
{
    [TestFixture]
    public class AltitudeTests
    {
        [Test]
        public void InitFromFeetMsl()
        {
            var alt1 = Altitude.FromFeetMsl(123.45f);

            alt1.Value.Should().Be(123.45f);
            alt1.Unit.Should().Be(AltitudeUnit.Feet);
            alt1.Type.Should().Be(AltitudeType.Msl);
        }

        [Test]
        public void CanConvertUnits()
        {
            var alt1 = Altitude.FromFeetMsl(19000);

            alt1.GetValueInUnit(AltitudeUnit.Feet).Should().Be(19000f);
            alt1.GetValueInUnit(AltitudeUnit.Meter).Should().Be(5791.2f);
            alt1.GetValueInUnit(AltitudeUnit.Kilometer).Should().BeApproximately(5.7912f, 0.0001f);
            alt1.GetValueInUnit(AltitudeUnit.FlightLevel).Should().Be(190);
        }

        [Test]
        public void CanConvertUnits_ShortcutProps()
        {
            var alt1 = Altitude.FromFeetMsl(19000);

            alt1.Feet.Should().Be(19000f);
            alt1.Meters.Should().Be(5791.2f);
            alt1.Kilometers.Should().BeApproximately(5.7912f, 0.0001f);
            alt1.FlightLevel.Should().Be(190);
        }

        [Test]
        public void Equality_SameTypeUnitAndValue_Equal()
        {
            var alt1 = Altitude.FromFeetMsl(123.45f);
            var alt2 = Altitude.FromFeetMsl(123.45f);

            alt1.Equals(alt2).Should().BeTrue();
            (alt1 == alt2).Should().BeTrue();
            (alt1 != alt2).Should().BeFalse();
        }

        [Test]
        public void Equality_SameTypeAndUnitButDifferenValue_NotEqual()
        {
            var alt1 = Altitude.FromFeetMsl(123.45f);
            var alt2 = Altitude.FromFeetMsl(678.9f);

            alt1.Equals(alt2).Should().BeFalse();
            (alt1 == alt2).Should().BeFalse();
            (alt1 != alt2).Should().BeTrue();
        }

        [Test]
        public void Equality_SameTypeEquivalentValueInDifferentUnit_Equal()
        {
            var alt1 = Altitude.FromFeetMsl(19000);
            var alt2 = Altitude.FromFlightLevel(190);

            alt1.Equals(alt2).Should().BeTrue();
            (alt1 == alt2).Should().BeTrue();
            (alt1 != alt2).Should().BeFalse();
        }

        [Test]
        public void Equality_SameUnitAndValueButDifferentType_NotEqual()
        {
            var alt1 = Altitude.FromFeetMsl(1000);
            var alt2 = Altitude.FromFeetAgl(1000);

            alt1.Equals(alt2).Should().BeFalse();
            (alt1 == alt2).Should().BeFalse();
            (alt1 != alt2).Should().BeTrue();
        }

        [Test]
        public void FlightLevel_GetInAgl_Throws()
        {
            var alt = Altitude.FromFeetAgl(19000);
            
            Assert.Throws<InvalidOperationException>(() => {
                var value = alt.FlightLevel;
            });

            Assert.Throws<InvalidOperationException>(() => {
                var value = alt.GetValueInUnit(AltitudeUnit.FlightLevel);
            });
        }
    }
}