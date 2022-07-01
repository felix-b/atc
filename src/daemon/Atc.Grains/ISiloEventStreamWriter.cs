namespace Atc.Grains;

public interface ISiloEventStreamWriter
{
    void FireGrainEvent(GrainEventEnvelope envelope);
}

public record GrainEventEnvelope(
    string TargetSiloId,
    string TargetGrainId,
    ulong SequenceNo,
    DateTime Utc,
    IGrainEvent Event
);
