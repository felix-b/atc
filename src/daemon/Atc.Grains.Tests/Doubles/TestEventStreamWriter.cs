using System.Collections.Immutable;

namespace Atc.Grains.Tests.Doubles;

public class TestEventStreamWriter : ISiloEventStreamWriter
{
    public ImmutableList<GrainEventEnvelope> Events { get; private set; } = ImmutableList<GrainEventEnvelope>.Empty;

    public void FireGrainEvent(GrainEventEnvelope envelope)
    {
        Events = Events.Add(envelope);
    }
}
