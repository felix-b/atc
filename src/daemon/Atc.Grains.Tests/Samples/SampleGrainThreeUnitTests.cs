using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace Atc.Grains.Tests.Samples;

[TestFixture]
public class SampleGrainThreeUnitTests
{
    [Test]
    public void CanCreateByActivationWithMocks()
    {
        var silo = SiloTestDoubles.CreateSilo("test1", SampleSilo.Configure);

        var oneMock = new Mock<IGrain>().As<ISampleGrainOne>();
        var twoMock = new Mock<IGrain>().As<ISampleGrainTwo>();

        oneMock.SetupGet(x => x.Num).Returns(987);
        oneMock.SetupGet(x => x.Str).Returns("ZYX");
        twoMock.SetupGet(x => x.Value).Returns(987.65m);
        
        var grain = silo.Grains.CreateGrain<SampleGrainThree>(grainId => 
            new SampleGrainThree.GrainActivationEvent(
                grainId, 
                One: SiloTestDoubles.MockGrainRef(oneMock.Object), 
                Two: SiloTestDoubles.MockGrainRef(twoMock.Object))); 
        
        grain.Get().Num.Should().Be(987);
        grain.Get().Str.Should().Be("ZYX");
        grain.Get().Value.Should().Be(987.65m);
    }
}