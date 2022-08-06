using System.Net.WebSockets;
using Atc.Telemetry;

namespace Atc.Server.TestDoubles;

public class TestEndpointTelemetry : TelemetryTestDoubleBase, IEndpointTelemetry
{
    void IEndpointTelemetry.VerboseRegisteringObserver(long connectionId, Type type, string? registrationKey)
    {
        ReportVerbose($"RegisteringObserver(connectionId={connectionId},type={type.Name},registrationKey=[{registrationKey}]");
    }

    void IEndpointTelemetry.VerboseDisposingObserver(long connectionId, string? registrationKey)
    {
        ReportVerbose($"DisposingObserver(connectionId={connectionId},registrationKey=[{registrationKey}]");
    }

    void IEndpointTelemetry.VerboseEndpointDisposingAsync(int step)
    {
        ReportVerbose($"EndpointDisposingAsync(step={step})");
    }

    void IEndpointTelemetry.InfoHostRunStarting()
    {
        ReportInfo($"HostRunStarting");
    }

    void IEndpointTelemetry.InfoHostRunFinished()
    {
        ReportInfo($"HostRunFinished");
    }

    void IEndpointTelemetry.InfoHostStoppingAsync(int step)
    {
        ReportInfo($"HostStoppingAsync(step={step})");
    }

    void IEndpointTelemetry.ErrorBadRequest()
    {
        ReportError(nameof(IEndpointTelemetry.ErrorBadRequest));
    }

    ITraceSpan IEndpointTelemetry.SpanAspNetCoreIncoming()
    {
        return new TestSpan(this, "AspNetCoreIncoming");
    }

    ITraceSpan IEndpointTelemetry.SpanAcceptingSocket(long connectionId)
    {
        return new TestSpan(this, $"AcceptingSocket(connectionId={connectionId})");
    }

    void IEndpointTelemetry.InfoConnectionSocketOpened(long connectionId)
    {
        ReportInfo($"ConnectionSocketOpened(connectionId={connectionId})");
    }

    void IEndpointTelemetry.InfoConnectionSocketClosed(long connectionId)
    {
        ReportInfo($"ConnectionSocketClosed(connectionId={connectionId})");
    }

    void IEndpointTelemetry.VerboseConnectionAlreadyDisposedIgnoring(long connectionId)
    {
        ReportVerbose($"ConnectionAlreadyDisposedIgnoring(connectionId={connectionId})");
    }

    void IEndpointTelemetry.ErrorSocketReceiveLoopFailure(long connectionId, WebSocketError? socketErrorCode, Exception exception)
    {
        ReportError($"ErrorSocketReceiveLoopFailure({connectionId},{socketErrorCode},{exception.GetType().Name}:{exception.Message})");
    }

    void IEndpointTelemetry.DebugConnectionDisposingObservers(long connectionId)
    {
        ReportDebug($"ConnectionDisposingObservers(connectionId={connectionId})");
    }

    void IEndpointTelemetry.DebugConnectionInitiatingSocketCloseHandshake(long connectionId)
    {
        ReportDebug($"ConnectionInitiatingSocketCloseHandshake(connectionId={connectionId})");
    }

    void IEndpointTelemetry.DebugConnectionJoiningReceiveLoop(long connectionId)
    {
        ReportDebug($"ConnectionJoiningReceiveLoop(connectionId={connectionId})");
    }

    ITraceSpan IEndpointTelemetry.SpanConnectionDispose(long connectionId, WebSocketState socketState)
    {
        return new TestSpan(this, $"ConnectionDispose(connectionId={connectionId})");
    }

    void IEndpointTelemetry.ErrorDispatchOperationFailed(string requestType, Exception exception)
    {
        ReportError($"ErrorDispatchOperationFailed({requestType},{exception.GetType().Name}:{exception.Message})");
    }

    OperationMethodNotFoundException IEndpointTelemetry.ExceptionOperationMethodNotFound(int discriminatorValue)
    {
        var message = $"ExceptionOperationMethodNotFound(discriminatorValue={discriminatorValue})"; 
        ReportError(message);
        return new OperationMethodNotFoundException(message);
    }

    ITraceSpan IEndpointTelemetry.SpanQueueOpDispatcherRunOutputThread(int queueIndex)
    {
        return new TestSpan(this, $"SpanQueueOpDispatcherRunOutputThread(queueIndex={queueIndex})");
    }

    ITraceSpan IEndpointTelemetry.SpanQueueOpDispatcherRunInputThread()
    {
        return new TestSpan(this, "SpanQueueOpDispatcherRunInputThread");
    }

    ITraceSpan IEndpointTelemetry.SpanConnectionRunReceiveLoop(long connectionId)
    {
        return new TestSpan(this, $"SpanConnectionRunReceiveLoop(connectionId={connectionId})");
    }

    void IEndpointTelemetry.WarningQueueOpDispatcherOutputThreadStoppingTimedOut(int threadIndex)
    {
        ReportWarning($"QueueOpDispatcherOutputThreadStoppingTimedOut(threadIndex={threadIndex})");
    }

    void IEndpointTelemetry.WarningQueueOpDispatcherInputThreadStoppingTimedOut()
    {
        ReportWarning($"QueueOpDispatcherInputThreadStoppingTimedOut");
    }

    void IEndpointTelemetry.VerboseQueueOpDispatcherInputThreadExiting()
    {
        ReportVerbose($"QueueOpDispatcherInputThreadExiting");
    }

    void IEndpointTelemetry.CriticalQueueOpDispatcherInputThreadCrashed(Exception exception)
    {
        ReportError($"CriticalQueueOpDispatcherInputThreadCrashed({exception.GetType().Name}:{exception.Message})");
    }

    void IEndpointTelemetry.InfoQueueOpDispatcherInputThreadExited()
    {
        ReportInfo($"QueueOpDispatcherInputThreadExited");
    }

    void IEndpointTelemetry.ErrorQueueOpDispatcherOutputsTimedOut(int queueIndex, long connectionId)
    {
        ReportError($"CriticalQueueOpDispatcherInputThreadCrashed(queueIndex={queueIndex},connectionId={connectionId})");
    }

    void IEndpointTelemetry.ErrorQueueOpDispatcherFailedToSendOutputs(int queueIndex, long connectionId, Exception exception)
    {
        ReportError($"CriticalQueueOpDispatcherInputThreadCrashed(queueIndex={queueIndex},connectionId={connectionId},{exception.GetType().Name}:{exception.Message})");
    }

    void IEndpointTelemetry.VerboseQueueOpDispatcherOutputThreadExiting(int queueIndex)
    {
        ReportVerbose($"QueueOpDispatcherOutputThreadExiting(queueIndex={queueIndex})");
    }

    void IEndpointTelemetry.CriticalQueueOpDispatcherOutputThreadCrashed(int queueIndex, Exception exception)
    {
        ReportError($"CriticalQueueOpDispatcherOutputThreadCrashed(queueIndex={queueIndex},{exception.GetType().Name}:{exception.Message})");
    }

    void IEndpointTelemetry.InfoQueueOpDispatcherOutputThreadExited(int queueIndex)
    {
        ReportInfo($"QueueOpDispatcherOutputThreadExited(queueIndex={queueIndex})");
    }

    void IEndpointTelemetry.DebugConnectionReceiveLoopCompleted(long connectionId, SocketReceiveStatus receiveStatus)
    {
        ReportDebug($"ConnectionReceiveLoopCompleted(connectionId={connectionId},receiveStatus={receiveStatus})");
    }

    void IEndpointTelemetry.DebugConnectionSocketReplyingCloseHandshake(long connectionId, WebSocketState socketState)
    {
        ReportDebug($"ConnectionReceiveLoopCompleted(connectionId={connectionId},socketState={socketState})");
    }

    void IEndpointTelemetry.DebugAcceptSocketExited(WebSocketState socketState)
    {
        ReportDebug($"AcceptSocketExited(socketState={socketState})");
    }

    void IEndpointTelemetry.DebugConnectionSocketDisposing(long connectionId, WebSocketState socketState)
    {
        ReportDebug($"ConnectionSocketDisposing(connectionId={connectionId},socketState={socketState})");
    }

    void IEndpointTelemetry.DebugConnectionMessageReceived(long connectionId, int sizeBytes)
    {
        ReportDebug($"ConnectionMessageReceived(connectionId={connectionId},sizeBytes={sizeBytes})");
    }

    void IEndpointTelemetry.DebugConnectionReceiveLoopCanceled(long connectionId)
    {
        ReportDebug($"ConnectionReceiveLoopCanceled(connectionId={connectionId})");
    }

    void IEndpointTelemetry.DebugQueueOpDispatcherAcceptingWorkItem(long connectionId, ulong workItemId, string workItemType)
    {
        ReportDebug($"QueueOpDispatcherAcceptingWorkItem(connectionId={connectionId},workItemId={workItemId},workItemType={workItemType})");
    }

    ITraceSpan IEndpointTelemetry.SpanQueueOpDispatcherExecuteWorkItem(ulong workItemId, string workItemType)
    {
        return new TestSpan(this, $"QueueOpDispatcherExecuteWorkItem(workItemId={workItemId},workItemType={workItemType})");
    }

    void IEndpointTelemetry.DebugQueueOpDispatcherEnqueueOutputRequests(int count, long queueIndex)
    {
        ReportDebug($"QueueOpDispatcherEnqueueOutputRequests(count={count},queueIndex={queueIndex})");
    }

    ITraceSpan IEndpointTelemetry.SpanMethodDispatcherInvoke(int payloadCase, string methodName)
    {
        return new TestSpan(this, $"MethodDispatcherInvoke(payloadCase={payloadCase},methodName={methodName})");
    }

    ITraceSpan IEndpointTelemetry.SpanQueueOpDispatcherInvokeArbitrary()
    {
        return new TestSpan(this, $"QueueOpDispatcherInvokeArbitrary");
    }

    ITraceSpan IEndpointTelemetry.SpanQueueOpDispatcherPerformOutputRequests(ulong workItemId, int count)
    {
        return new TestSpan(this, $"QueueOpDispatcherPerformOutputRequests(workItemId={workItemId},count={count})");
    }

    ITraceSpan IEndpointTelemetry.SpanConnectionPerformOutputRequest(int index, string type)
    {
        return new TestSpan(this, $"ConnectionPerformOutputRequest(index={index},type={type})");
    }
}
