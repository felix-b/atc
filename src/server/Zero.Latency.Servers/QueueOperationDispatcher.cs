
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Zero.Latency.Servers
{
    public class QueueOperationDispatcher<TEnvelopeIn, TEnvelopeOut> : IOperationDispatcher, IServiceTaskSynchronizer
        where TEnvelopeIn : class 
        where TEnvelopeOut : class 
    {
        private readonly IDeferredOperationDispatcher<TEnvelopeIn, TEnvelopeOut> _next;
        private readonly IEndpointLogger _logger;
        private readonly Thread _inputThread;
        private readonly BlockingCollection<WorkItem> _inputQueue = new(boundedCapacity: 500);
        private readonly Thread[] _outputThreads;
        private readonly BlockingCollection<OperationOutputsWorkItem>[] _outputQueues;
        private readonly CancellationTokenSource _cancellation = new();
        private bool _disposed = false;

        public QueueOperationDispatcher(int outputThreadCount, IDeferredOperationDispatcher<TEnvelopeIn, TEnvelopeOut> next, IEndpointLogger logger)
        {
            _next = next;
            _logger = logger;

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
                    Console.WriteLine($"WARNING! OUTPUT THREAD [{i}] CANNOT BE STOPPED");
                }
            }

            if (!_inputThread.Join(5000))
            {
                Console.WriteLine($"WARNING! INPUT THREAD CANNOT BE STOPPED");
            }

            await nextStopTask;
        }
        
        public void DispatchOperation(IConnectionContext connection, object envelope)
        {
            //TODO: optimize to minimize heap allocations!

            var workItem = new IncomingMessageWorkItem(
                Context: new DeferredConnectionContext<TEnvelopeIn, TEnvelopeOut>(connection), 
                Envelope: (TEnvelopeIn)envelope);
            
            _inputQueue.Add(workItem);
        }

        public void SubmitTask(Action callback)
        {
            var workItem = new ArbitraryCallbackWorkItem(callback);
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
            
            // when implementing observers, the context can be kept referenced beyond the lifetime of the operation
            // an observer might write to the context again at a later moment
            // the observer must then call RequestFlush() which invokes the delegate below 
            // and so we ensure that the context enters the output queue once again
            workItem.Context.OnFlushRequested += () => {
                _outputQueues[queueIndex].Add(new OperationOutputsWorkItem(
                    Context: workItem.Context
                ));
            };
            
            _outputQueues[queueIndex].Add(new OperationOutputsWorkItem(
                Context: workItem.Context
            ));
        }
        
        private void RunInputThread()
        {
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
                            Console.WriteLine($"OPERATION DISPATCH FAILED! {e}");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("OPERATION INPUT QUEUE THREAD - CANCELING");
            }
            catch (Exception e)
            {
                Console.WriteLine($"OPERATION INPUT QUEUE THREAD CRASHED! {e}");
            }

            Console.WriteLine("OPERATION INPUT QUEUE THREAD - EXIT");
        }

        private void RunOutputThread(int queueIndex)
        {
            try
            {
                while (!_cancellation.IsCancellationRequested)
                {
                    if (_outputQueues[queueIndex].TryTake(out var item, 10000, _cancellation.Token))
                    {
                        try
                        {
                            var result = item.Context.PerformOutputRequests();
                            if (!result.IsCompleted)
                            {
                                if (!result.AsTask().Wait(5000, _cancellation.Token))
                                {
                                    Console.WriteLine($"OPERATION OUTPUTS TIMED OUT!");
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"OPERATION OUTPUTS FAILED! {e}");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"OPERATION INPUT QUEUE THREAD - EXITING");
            }
            catch (Exception e)
            {
                Console.WriteLine($"OPERATION INPUT QUEUE THREAD CRASHED! {e}");
            }

            Console.WriteLine($"OPERATION OUTPUT QUEUE THREAD[{queueIndex}] - EXIT");
        }

        private void ExecuteWorkItemOnCurrentThread(WorkItem workItem)
        {
            switch (workItem)
            {
                case IncomingMessageWorkItem incomingMessage:
                    _next.DispatchOperation(incomingMessage.Context, incomingMessage.Envelope!);
                    EnqueueOutputRequests(incomingMessage);
                    break;
                case ArbitraryCallbackWorkItem arbitraryCallback:
                    arbitraryCallback.Callback();
                    break;
                default:
                    throw new ArgumentException($"Work item type not recognized: {workItem.GetType()}");
            }
        }

        private void ExecuteWorkItemOnCurrentThread(ArbitraryCallbackWorkItem workItem)
        {
            workItem.Callback();
        }

        private abstract record WorkItem;

        private record IncomingMessageWorkItem(
            DeferredConnectionContext<TEnvelopeIn, TEnvelopeOut> Context,
            TEnvelopeIn Envelope
        ) : WorkItem;

        private record OperationOutputsWorkItem(
            DeferredConnectionContext<TEnvelopeIn, TEnvelopeOut> Context
        ) : WorkItem;
        
        private record ArbitraryCallbackWorkItem(
            Action Callback
        ) : WorkItem;
    }
}
