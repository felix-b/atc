namespace Atc.Grains.Impl;

public class EventDispatch : ISiloEventDispatch
{
    private readonly string _siloId;
    private readonly ISiloEventStreamWriter _eventWriter;
    private readonly ISiloTelemetry _telemetry;
    private readonly ISiloEnvironment _environment;
    private readonly Func<ISiloTaskQueue> _getTaskQueue;
    private ulong _nextSequenceNo = 1;
    private bool _dispatching = false;

    public EventDispatch(
        string siloId, 
        ISiloEventStreamWriter eventWriter, 
        ISiloTelemetry telemetry, 
        ISiloEnvironment environment,
        Func<ISiloTaskQueue> getTaskQueue)
    {
        _siloId = siloId;
        _eventWriter = eventWriter;
        _telemetry = telemetry;
        _environment = environment;
        _getTaskQueue = getTaskQueue;
    }

    public async Task Dispatch(IGrain target, IGrainEvent @event)
    {
        if (_dispatching)
        {
            throw new InvalidOperationException("Reentrant dispatch is not allowed. Use TaskQueue.Defer() instead.");
        }

        var sequenceNo = _nextSequenceNo++;
        var envelope = CreateEventEnvelope(target, @event, sequenceNo);
        await _eventWriter.WriteGrainEvent(envelope);

        //using var logSpan = _logger.Dispatch(sequenceNo, target.UniqueId, @event.GetType().Name, @event.ToString());
        _dispatching = true;

        try
        {
            var state0 = target.GetState();
            var state1 = target.Reduce(state0, @event);

            if (!object.ReferenceEquals(state0, state1))
            {
                //InvokeEventListeners(new StateEventEnvelope(sequenceNo, target.UniqueId, @event));
                    
                target.SetState(state1);
                target.ObserveChanges(state0, state1);
            }
        }
        catch //(Exception e)
        {
            //logSpan.Fail(e);
        }
        finally
        {
            _dispatching = false;
        }
    }

    public ulong NextSequenceNo => _nextSequenceNo;

    public ISiloTaskQueue TaskQueue => _getTaskQueue();
    
    private GrainEventEnvelope CreateEventEnvelope(IGrain grain, IGrainEvent @event, ulong sequenceNo)
    {
        return new GrainEventEnvelope(_siloId, grain.GrainId, sequenceNo, _environment.UtcNow, @event);
    }
}
