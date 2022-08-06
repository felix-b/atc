using Atc.Telemetry;

namespace Atc.Grains;

[TelemetryName("SomeCustomV2")]
public interface ISiloTelemetry : ITelemetry
{
    void DebugDispatchEvent(string grainId, IGrainEvent @event);
    ITraceSpan SpanExecuteReadyWorkItems();
    ITraceSpan SpanExecuteWorkItem(string grainId, IGrainWorkItem workItem, bool timedOut);
}
