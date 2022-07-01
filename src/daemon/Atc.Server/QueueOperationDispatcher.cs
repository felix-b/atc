using System.Collections.Concurrent;

namespace Atc.Server;

public class QueueOperationDispatcher<TEnvelopeIn, TEnvelopeOut> : IOperationDispatcher, IServiceTaskSynchronizer
    where TEnvelopeIn : class 
    where TEnvelopeOut : class 
{
    private readonly IDeferredOperationDispatcher<TEnvelopeIn, TEnvelopeOut> _next;
    private readonly IEndpointTelemetry _telemetry;
    private readonly Thread _inputThread;
    private readonly BlockingCollection<WorkItem> _inputQueue = new(boundedCapacity: 500);
    private readonly Thread[] _outputThreads;
    private readonly BlockingCollection<OperationOutputsWorkItem>[] _outputQueues;
    private readonly CancellationTokenSource _cancellation = new();
    private ulong _nextWorkItemId = 1;
    private bool _disposed = false;

    public QueueOperationDispatcher(int outputThreadCount, IDeferredOperationDispatcher<TEnvelopeIn, TEnvelopeOut> next, IEndpointTelemetry telemetry)
    {
        _next = next;
        _telemetry = telemetry;

        _outputQueues = Enumerable.Range(0, outputThreadCount)
            .Select(index => new BlockingCollection<OperationOutputsWorkItem>(boundedCapacity: 500))
            .ToArray();
        _outputThreads = new Thread[_outputQueues.Length];

        SpinUpOutputThreads();

        _inputThread = new Thread(RunInputThread);
        _inputThread.Start();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cancellation.Cancel();

        var nextStopTask = _next.DisposeAsync();

        for (int i = 0 ; i < _outputThreads.Length ; i++)
        {
            if (!_outputThreads[i].Join(5000))
            {
                _telemetry.WarningQueueOpDispatcherOutputThreadStoppingTimedOut(threadIndex: i);
            }
        }

        if (!_inputThread.Join(5000))
        {
            _telemetry.WarningQueueOpDispatcherInputThreadStoppingTimedOut();
        }

        await nextStopTask;
    }
        
    public void DispatchOperation(IConnectionContext connection, object envelope)
    {
        //TODO: optimize to minimize heap allocations!

        var workItem = new IncomingMessageWorkItem(
            Id: TakeNextWorkItemId(),
            Context: new DeferredConnectionContext<TEnvelopeIn, TEnvelopeOut>(connection, _telemetry), 
            Envelope: (TEnvelopeIn)envelope);
            
        _telemetry.DebugQueueOpDispatcherAcceptingWorkItem(connection.Id, workItem.Id, workItem.GetType().Name);
        _inputQueue.Add(workItem);
    }

    public void SubmitTask(Action callback)
    {
        var workItem = new ArbitraryCallbackWorkItem(
            Id: TakeNextWorkItemId(), 
            callback);
        
        _inputQueue.Add(workItem);
    }

    private void SpinUpOutputThreads()
    {
        for (int i = 0; i < _outputThreads.Length; i++)
        {
            var queueIndex = i;
            _outputThreads[queueIndex] = new Thread(() => RunOutputThread(queueIndex));
            _outputThreads[queueIndex].Start();
        }
    }

    private void EnqueueOutputRequests(IncomingMessageWorkItem workItem)
    {
        var queueIndex = workItem.Context.Id % _outputQueues.Length;
        _telemetry.DebugQueueOpDispatcherEnqueueOutputRequests(count: workItem.Context.OutputRequests?.Count ?? 0, queueIndex);
            
        // when implementing observers, the context can be kept referenced beyond the lifetime of the operation
        // an observer might write to the context again at a later moment
        // the observer must then call RequestFlush() which invokes the delegate below 
        // and so we ensure that the context enters the output queue once again
        workItem.Context.OnFlushRequested += () => {
            _outputQueues[queueIndex].Add(new OperationOutputsWorkItem(
                Id: workItem.Id,
                Context: workItem.Context
            ));
        };
            
        _outputQueues[queueIndex].Add(new OperationOutputsWorkItem(
            Id: workItem.Id,
            Context: workItem.Context
        ));
    }
        
    private void RunInputThread()
    {
        using var traceSpan = _telemetry.SpanQueueOpDispatcherRunInputThread();
            
        try
        {
            while (!_cancellation.IsCancellationRequested)
            {
                if (_inputQueue.TryTake(out var item, 10000, _cancellation.Token) && !_cancellation.IsCancellationRequested)
                {
                    try
                    {
                        ExecuteWorkItemOnCurrentThread(item);                            
                    }
                    catch (Exception e)
                    {
                        _telemetry.ErrorDispatchOperationFailed(requestType: item.GetType().Name, exception: e);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _telemetry.VerboseQueueOpDispatcherInputThreadExiting();
        }
        catch (Exception e)
        {
            _telemetry.CriticalQueueOpDispatcherInputThreadCrashed(exception: e);
        }

        _telemetry.InfoQueueOpDispatcherInputThreadExited();
    }

    private void RunOutputThread(int queueIndex)
    {
        using var traceSpan = _telemetry.SpanQueueOpDispatcherRunOutputThread(queueIndex);

        try
        {
            while (!_cancellation.IsCancellationRequested)
            {
                if (_outputQueues[queueIndex].TryTake(out var item, 10000, _cancellation.Token))
                {
                    using var itemTraceSpan = _telemetry.SpanQueueOpDispatcherPerformOutputRequests(
                        workItemId: item.Id, 
                        count: item.Context.OutputRequests?.Count ?? 0);
                    
                    try
                    {
                        var result = item.Context.PerformOutputRequests();
                        if (!result.IsCompleted)
                        {
                            if (!result.AsTask().Wait(5000, _cancellation.Token))
                            {
                                _telemetry.ErrorQueueOpDispatcherOutputsTimedOut(queueIndex, connectionId: item.Context.Id);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        _telemetry.ErrorQueueOpDispatcherFailedToSendOutputs(queueIndex, connectionId: item.Context.Id, exception: e);
                        itemTraceSpan.Fail(e);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _telemetry.VerboseQueueOpDispatcherOutputThreadExiting(queueIndex);
        }
        catch (Exception e)
        {
            _telemetry.CriticalQueueOpDispatcherOutputThreadCrashed(queueIndex, exception: e);
        }

        _telemetry.InfoQueueOpDispatcherOutputThreadExited(queueIndex);
    }

    private void ExecuteWorkItemOnCurrentThread(WorkItem workItem)
    {
        using var traceSpan = _telemetry.SpanQueueOpDispatcherExecuteWorkItem(workItem.Id, workItem.GetType().Name);
        
        switch (workItem)
        {
            case IncomingMessageWorkItem incomingMessage:
                _next.DispatchOperation(incomingMessage.Context, incomingMessage.Envelope!);
                EnqueueOutputRequests(incomingMessage);
                break;
            case ArbitraryCallbackWorkItem arbitraryCallback:
                ExecuteArbitraryCallback(arbitraryCallback);
                break;
            default:
                throw new ArgumentException($"Work item type not recognized: {workItem.GetType()}");
        }
    }

    private void ExecuteArbitraryCallback(ArbitraryCallbackWorkItem arbitraryCallback)
    {
        using (var callbackTraceSpan = _telemetry.SpanQueueOpDispatcherInvokeArbitrary())
        {
            try
            {
                arbitraryCallback.Callback();
            }
            catch (Exception e)
            {
                callbackTraceSpan.Fail(e);
                throw;
            }
        }
    }

    private ulong TakeNextWorkItemId()
    {
        return Interlocked.Increment(ref _nextWorkItemId);
    }
    
    private abstract record WorkItem(ulong Id);

    private record IncomingMessageWorkItem(
        ulong Id,
        DeferredConnectionContext<TEnvelopeIn, TEnvelopeOut> Context,
        TEnvelopeIn Envelope
    ) : WorkItem(Id);

    private record OperationOutputsWorkItem(
        ulong Id,
        DeferredConnectionContext<TEnvelopeIn, TEnvelopeOut> Context
    ) : WorkItem(Id);
        
    private record ArbitraryCallbackWorkItem(
        ulong Id,
        Action Callback
    ) : WorkItem(Id);
}