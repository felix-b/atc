namespace Atc.Grains.Impl;

public class EventDispatch : ISiloEventDispatch
{
    private readonly Silo _silo;
    private readonly ISiloEventStreamWriter _eventWriter;
    private readonly ISiloTelemetry _telemetry;
    private readonly ISiloEnvironment _environment;
    private ulong _nextSequenceNo = 1;
    private bool _dispatching = false;

    public EventDispatch(
        Silo silo, 
        ISiloEventStreamWriter eventWriter, 
        ISiloTelemetry telemetry, 
        ISiloEnvironment environment)
    {
        _silo = silo;
        _eventWriter = eventWriter;
        _telemetry = telemetry;
        _environment = environment;
    }

    public void Dispatch(IGrain target, IGrainEvent @event)
    {
        if (_dispatching)
        {
            throw new InvalidOperationException("Reentrant dispatch is not allowed. Use TaskQueue.Defer() instead.");
        }

        var sequenceNo = _nextSequenceNo++;
        var envelope = CreateEventEnvelope(target, @event, sequenceNo);
        _eventWriter.FireGrainEvent(envelope);

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

    public ISilo Silo => _silo;

    private GrainEventEnvelope CreateEventEnvelope(IGrain grain, IGrainEvent @event, ulong sequenceNo)
    {
        return new GrainEventEnvelope(_silo.SiloId, grain.GrainId, sequenceNo, _environment.UtcNow, @event);
    }
}
