using Atc.Data.Primitives;
using NUnit.Framework;

namespace Atc.Data.Tests.Primitives
{
    [TestFixture]
    public class WindTests
    {
        [Test]
        public void CanUseWindConstructors()
        {
            var wind1 = new Wind(
                direction: Bearing.FromTrueDegrees(210),
                speed: Speed.FromKnots(5),
                gust: null);

            var wind2 = new Wind(
                direction: (from: Bearing.FromTrueDegrees(210), to: Bearing.FromTrueDegrees(260)),
                speed: (from: Speed.FromKnots(5), to: Speed.FromKnots(10)),
                gust: Speed.FromKnots(20));
        }
    }
}
