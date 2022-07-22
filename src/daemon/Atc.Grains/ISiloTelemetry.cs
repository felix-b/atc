using Atc.Telemetry;

namespace Atc.Grains;

public interface ISiloTelemetry : ITelemetry
{
    void DebugDispatchEvent(string grainId, IGrainEvent @event);
    ITraceSpan SpanExecuteReadyWorkItems();
    ITraceSpan SpanExecuteWorkItem(string grainId, IGrainWorkItem workItem, bool timedOut);
}
