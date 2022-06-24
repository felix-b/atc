using System.Collections.Immutable;

namespace Atc.Grains.Tests.Samples;

public class TestEventStreamWriter : ISiloEventStreamWriter
{
    public ImmutableList<IGrainEvent> Events { get; private set; } = ImmutableList<IGrainEvent>.Empty;

    public Task WriteGrainEvent(ISilo targetSilo, IGrain targetGrain, ulong sequenceNo, IGrainEvent @event)
    {
        Events = Events.Add(@event);
        return Task.CompletedTask;
    }
}
