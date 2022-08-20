using Atc.Grains;

namespace Atc.Daemon;

public class AtcdOfflineSiloEventStream : ISiloEventStreamWriter
{
    private readonly List<GrainEventEnvelope> _events = new(capacity: 16384); 
    
    void ISiloEventStreamWriter.FireGrainEvent(GrainEventEnvelope envelope)
    {
        _events.Add(envelope);
    }
}
