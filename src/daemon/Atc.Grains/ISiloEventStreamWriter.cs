namespace Atc.Grains;

public interface ISiloEventStreamWriter
{
    Task WriteGrainEvent(ISilo targetSilo, IGrain targetGrain, ulong sequenceNo, IGrainEvent @event);
}
