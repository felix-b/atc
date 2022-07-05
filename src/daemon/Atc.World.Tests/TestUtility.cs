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
    
    public static MockedGrain<T> MockGrain<T>() where T : class, IGrainId
    {
        var mockId = TakeNextMockId();
        var mock = new Mock<T>();

        var grainType = $"MockOf{typeof(T).Name}";
        mock.SetupGet(x => x.GrainType).Returns(grainType);
        mock.SetupGet(x => x.GrainId).Returns($"{grainType}/#{mockId}");

        var grainRef = SiloTestDoubles.MockGrainRef(mock.Object);
        return new MockedGrain<T>(grainRef, mock);
    }

    private static int TakeNextMockId()
    {
        return Interlocked.Increment(ref __nextMockId);
    }
}