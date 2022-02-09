using Atc.Data.Primitives;
using NUnit.Framework;
using FluentAssertions;

namespace Atc.Data.Tests.Primitives
{
    [TestFixture]
    public class BearingTests
    {
        [Test]
        public void CanNormalizeDegrees()
        {
            Bearing.NormalizeDegrees(0.0f).Should().Be(0.0f);
            Bearing.NormalizeDegrees(30.0f).Should().Be(30.0f);
            Bearing.NormalizeDegrees(359.99f).Should().BeApproximately(359.99f, 0.01f);
            Bearing.NormalizeDegrees(360.0f).Should().Be(0.0f);

            Bearing.NormalizeDegrees(480.1f).Should().BeApproximately(120.1f, 0.01f);
            Bearing.NormalizeDegrees(-120.1f).Should().BeApproximately(239.9f, 0.01f);
            Bearing.NormalizeDegrees(-480.1f).Should().BeApproximately(239.9f, 0.01f);

            Bearing.NormalizeDegrees(4080.1f).Should().BeApproximately(120.1f, 0.01f);
            Bearing.NormalizeDegrees(-4080.1f).Should().BeApproximately(239.9f, 0.01f);
        }
    }
}
