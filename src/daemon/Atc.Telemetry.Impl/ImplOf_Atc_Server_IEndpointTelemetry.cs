using System.Net.WebSockets;
using Atc.Server;

namespace Atc.Telemetry.Impl;

public static class ImplOf_Atc_Server_IEndpointTelemetry 
{
    public class Noop : IEndpointTelemetry
    {
        public void VerboseRegisteringObserver(long connectionId, Type type, string? registrationKey)
        {
        }

        public void VerboseDisposingObserver(long connectionId, string? registrationKey)
        {
        }

        public void VerboseEndpointDisposingAsync(int step)
        {
        }

        public void InfoHostRunStarting()
        {
        }

        public void InfoHostRunFinished()
        {
        }

        public void InfoHostStoppingAsync(int step)
        {
        }

        public void ErrorBadRequest()
        {
        }

        public ITraceSpan SpanAspNetCoreIncoming()
        {
            return NoopTraceSpan.Instance;
        }

        public ITraceSpan SpanAcceptingSocket(long connectionId)
        {
            return NoopTraceSpan.Instance;
        }

        public void InfoConnectionSocketOpened(long connectionId)
        {
        }

        public void InfoConnectionSocketClosed(long connectionId)
        {
        }

        public void VerboseConnectionAlreadyDisposedIgnoring(long connectionId)
        {
        }

        public void ErrorSocketReceiveLoopFailure(long connectionId, WebSocketError? socketErrorCode,
            Exception exception)
        {
        }

        public void DebugConnectionDisposingObservers(long connectionId)
        {
            throw new NotImplementedException();
        }

        public void DebugConnectionInitiatingSocketCloseHandshake(long connectionId)
        {
            throw new NotImplementedException();
        }

        public void DebugConnectionJoiningReceiveLoop(long connectionId)
        {
            throw new NotImplementedException();
        }

        public ITraceSpan SpanConnectionDispose(long connectionId, WebSocketState socketState)
        {
            return NoopTraceSpan.Instance;
        }

        public void ErrorDispatchOperationFailed(string requestType, Exception exception)
        {
            throw new NotImplementedException();
        }

        public OperationMethodNotFoundException ExceptionOperationMethodNotFound<TPayloadCaseIn>(
            TPayloadCaseIn discriminatorValue) where TPayloadCaseIn : Enum
        {
            return new OperationMethodNotFoundException();
        }

        public ITraceSpan SpanQueueOpDispatcherRunOutputThread(int queueIndex)
        {
            return NoopTraceSpan.Instance;
        }

        public ITraceSpan SpanQueueOpDispatcherRunInputThread()
        {
            return NoopTraceSpan.Instance;
        }

        public ITraceSpan SpanConnectionRunReceiveLoop(long connectionId)
        {
            return NoopTraceSpan.Instance;
        }

        public void WarningQueueOpDispatcherOutputThreadStoppingTimedOut(int threadIndex)
        {
        }

        public void WarningQueueOpDispatcherInputThreadStoppingTimedOut()
        {
        }

        public void VerboseQueueOpDispatcherInputThreadExiting()
        {
        }

        public void CriticalQueueOpDispatcherInputThreadCrashed(Exception exception)
        {
        }

        public void InfoQueueOpDispatcherInputThreadExited()
        {
        }

        public void ErrorQueueOpDispatcherOutputsTimedOut(int queueIndex, long connectionId)
        {
        }

        public void ErrorQueueOpDispatcherFailedToSendOutputs(int queueIndex, long connectionId, Exception exception)
        {
        }

        public void VerboseQueueOpDispatcherOutputThreadExiting(int queueIndex)
        {
        }

        public void CriticalQueueOpDispatcherOutputThreadCrashed(int queueIndex, Exception exception)
        {
        }

        public void InfoQueueOpDispatcherOutputThreadExited(int queueIndex)
        {
        }

        public void DebugConnectionReceiveLoopCompleted(long connectionId, SocketReceiveStatus receiveStatus)
        {
        }

        public void DebugConnectionSocketReplyingCloseHandshake(long connectionId, WebSocketState socketState)
        {
        }

        public void DebugAcceptSocketExited(WebSocketState socketState)
        {
        }

        public void DebugConnectionSocketDisposing(long connectionId, WebSocketState socketState)
        {
        }

        public void DebugConnectionMessageReceived(long connectionId, int sizeBytes)
        {
        }

        public void DebugConnectionReceiveLoopCanceled(long connectionId)
        {
        }

        public void DebugQueueOpDispatcherAcceptingWorkItem(long connectionId, ulong workItemId, string workItemType)
        {
        }

        public ITraceSpan SpanQueueOpDispatcherExecuteWorkItem(ulong workItemId, string workItemType)
        {
            return NoopTraceSpan.Instance;
        }

        public void DebugQueueOpDispatcherEnqueueOutputRequests(int count, long queueIndex)
        {
        }

        public ITraceSpan SpanMethodDispatcherInvoke<TPayloadCaseIn>(TPayloadCaseIn payloadCase, string methodName)
            where TPayloadCaseIn : Enum
        {
            return NoopTraceSpan.Instance;
        }

        public ITraceSpan SpanQueueOpDispatcherInvokeArbitrary()
        {
            return NoopTraceSpan.Instance;
        }

        public ITraceSpan SpanQueueOpDispatcherPerformOutputRequests(ulong workItemId, int count)
        {
            return NoopTraceSpan.Instance;
        }

        public ITraceSpan SpanConnectionPerformOutputRequest(int index, string type)
        {
            return NoopTraceSpan.Instance;
        }
    }
}