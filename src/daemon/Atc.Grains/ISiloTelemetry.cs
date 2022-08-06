using Atc.Telemetry;

namespace Atc.Grains;

[TelemetryName("Silo")]
public interface ISiloTelemetry : ITelemetry
{
    void DebugDispatchEvent(string grainId, IGrainEvent @event);
    ITraceSpan SpanExecuteReadyWorkItems();
    ITraceSpan SpanExecuteWorkItem(string grainId, string workItemType, ulong workItemId, bool timedOut);
    void DebugInsertWorkItem(ulong id, string type, DateTime notEarlierThanUtc, DateTime notLaterThanUtc, DateTime firstWorkItemUtc);
    void DebugRemoveWorkItem(ulong id, DateTime firstWorkItemUtc);
    EventFailedException ExceptionDispatchEventFailed(ulong sequenceNo, string targetGrainId, string eventType, Exception exception);
    InvalidOperationException ExceptionInvalidInteractionThread(string siloId, int ownerThreadId, int currentThreadId);
    void DebugPostedAsyncAction(ulong key);
    void ErrorFailedToPostAsyncActionQueueFull(ulong key);
    ITraceSpan SpanRunAsyncAction(ulong key);
    void ErrorFailedToExecuteAsyncAction(ulong key, Exception exception);
    void ErrorExecuteReadyWorkItemsFailed(Exception exception);
}
