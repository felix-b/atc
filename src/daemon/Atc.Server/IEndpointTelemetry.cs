using System.Net.WebSockets;
using Atc.Telemetry;

namespace Atc.Server;

public interface IEndpointTelemetry : ITelemetry
{
    void VerboseRegisteringObserver(long connectionId, Type type, string? registrationKey);
    void VerboseDisposingObserver(long connectionId, string? registrationKey);
    void VerboseEndpointDisposingAsync(int step);
    void InfoHostRunStarting();
    void InfoHostRunFinished();
    void InfoHostStoppingAsync(int step);
    void ErrorBadRequest();
    ITraceSpan SpanAspNetCoreIncoming();
    ITraceSpan SpanAcceptingSocket(long connectionId);
    void InfoConnectionSocketOpened(long connectionId);
    void InfoConnectionSocketClosed(long connectionId);
    void VerboseConnectionAlreadyDisposedIgnoring(long connectionId);
    void ErrorSocketReceiveLoopFailure(long connectionId, WebSocketError? socketErrorCode, Exception exception);
    void DebugConnectionDisposingObservers(long connectionId);
    void DebugConnectionInitiatingSocketCloseHandshake(long connectionId);
    void DebugConnectionJoiningReceiveLoop(long connectionId);
    ITraceSpan SpanConnectionDispose(long connectionId, WebSocketState socketState);
    void ErrorDispatchOperationFailed(string requestType, Exception exception);
    OperationMethodNotFoundException ExceptionOperationMethodNotFound(int discriminatorValue);
    ITraceSpan SpanQueueOpDispatcherRunOutputThread(int queueIndex);
    ITraceSpan SpanQueueOpDispatcherRunInputThread();
    ITraceSpan SpanConnectionRunReceiveLoop(long connectionId);
    void WarningQueueOpDispatcherOutputThreadStoppingTimedOut(int threadIndex);
    void WarningQueueOpDispatcherInputThreadStoppingTimedOut();
    void VerboseQueueOpDispatcherInputThreadExiting();
    void CriticalQueueOpDispatcherInputThreadCrashed(Exception exception);
    void InfoQueueOpDispatcherInputThreadExited();
    void ErrorQueueOpDispatcherOutputsTimedOut(int queueIndex, long connectionId);
    void ErrorQueueOpDispatcherFailedToSendOutputs(int queueIndex, long connectionId, Exception exception);
    void VerboseQueueOpDispatcherOutputThreadExiting(int queueIndex);
    void CriticalQueueOpDispatcherOutputThreadCrashed(int queueIndex, Exception exception);
    void InfoQueueOpDispatcherOutputThreadExited(int queueIndex);
    void DebugConnectionReceiveLoopCompleted(long connectionId, SocketReceiveStatus receiveStatus);
    void DebugConnectionSocketReplyingCloseHandshake(long connectionId, WebSocketState socketState);
    void DebugAcceptSocketExited(WebSocketState socketState);
    void DebugConnectionSocketDisposing(long connectionId, WebSocketState socketState);
    void DebugConnectionMessageReceived(long connectionId, int sizeBytes);
    void DebugConnectionReceiveLoopCanceled(long connectionId);
    void DebugQueueOpDispatcherAcceptingWorkItem(long connectionId, ulong workItemId, string workItemType);
    ITraceSpan SpanQueueOpDispatcherExecuteWorkItem(ulong workItemId, string workItemType);
    void DebugQueueOpDispatcherEnqueueOutputRequests(int count, long queueIndex);
    ITraceSpan SpanMethodDispatcherInvoke(int payloadCase, string methodName);
    ITraceSpan SpanQueueOpDispatcherInvokeArbitrary();
    ITraceSpan SpanQueueOpDispatcherPerformOutputRequests(ulong workItemId, int count);
    ITraceSpan SpanConnectionPerformOutputRequest(int index, string type);
    void DebugConnectionFireMessageRequest(long connectionId);
    void DebugConnectionRegisterObserverRequest(long connectionId, string? registrationKey);
    void DebugConnectionDisposeObserverRequest(long connectionId, string registrationKey);
    void DebugConnectionCloseRequest(long connectionId);
    void DebugConnectionFlushRequest(long connectionId);
    Exception ExceptionDisposeObserverRegistrationKeyNull();
}
