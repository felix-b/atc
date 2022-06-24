namespace Atc.Grains.Impl;

public class EventDispatch : ISiloEventDispatch
{
    private readonly ISilo _silo;
    private readonly ISiloEventStreamWriter _eventWriter;
    private ulong _nextSequenceNo = 1;
    private bool _dispatching = false;
    
    public EventDispatch(ISilo silo, ISiloEventStreamWriter eventWriter, ISiloTelemetry telemetry)
    {
        _silo = silo;
        _eventWriter = eventWriter;
    }

    public async Task Dispatch(IGrain target, IGrainEvent @event)
    {
        if (_dispatching)
        {
            throw new InvalidOperationException("Reentrant dispatch is not allowed. Use TaskQueue.Defer() instead.");
        }

        var sequenceNo = _nextSequenceNo++;
        await _eventWriter.WriteGrainEvent(_silo, target, sequenceNo, @event);

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
        catch (Exception e)
        {
            //logSpan.Fail(e);
        }
        finally
        {
            _dispatching = false;
        }
    }

    public ulong NextSequenceNo => _nextSequenceNo;
}
