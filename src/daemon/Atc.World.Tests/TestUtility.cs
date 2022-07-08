using Atc.Grains;
using Moq;

namespace Atc.World.Tests;

public record MockedGrain<T>(
    GrainRef<T> Grain,
    Mock<T> Mock
) where T : class, IGrainId;

public static class TestUtility
{
    private static int __nextMockId = 1;
    
    public static MockedGrain<T> MockGrain<T>(string? grainId = null, ISilo? injectTo = null) where T : class, IGrainId
    {
        var mock = new Mock<T>();
        var grainType = $"MockOf{typeof(T).Name}";

        mock.SetupGet(x => x.GrainType).Returns(grainType);
        mock.SetupGet(x => x.GrainId).Returns(grainId ?? MakeNewGrainId());

        if (injectTo != null)
        {
            SiloTestDoubles.InjectGrainMockToSilo(mock.As<IGrain>().Object, injectTo);
        }
        
        var grainRef = SiloTestDoubles.MockGrainRef(mock.Object);
        return new MockedGrain<T>(grainRef, mock);

        string MakeNewGrainId()
        {
            var mockId = TakeNextMockId();
            return $"{grainType}/#{mockId}";
        }
    }

    public static MockedGrain<IWorldGrain> MockWorldGrain(ISilo silo)
    {
        return MockGrain<IWorldGrain>(grainId: SiloExtensions.WorldGrainId, injectTo: silo);
    }

    private static int TakeNextMockId()
    {
        return Interlocked.Increment(ref __nextMockId);
    }
}