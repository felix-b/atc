namespace Atc.Grains;

public abstract class AbstractGrain<TStateRecord> : IGrain 
    where TStateRecord : class // use C# record types
{
    private readonly ISiloEventDispatch _dispatch;
    private TStateRecord _state;

    protected AbstractGrain(string grainId, string grainType, ISiloEventDispatch dispatch, TStateRecord initialState)
    {
        GrainId = grainId;
        GrainType = grainType;

        _dispatch = dispatch;
        _state = initialState;
    }

    public string GrainId { get; }
    public string GrainType { get; }
    
    protected abstract TStateRecord Reduce(TStateRecord stateBefore, IGrainEvent @event);

    protected virtual Task<bool> ExecuteWorkItem(IGrainWorkItem workItem)
    {
        return Task.FromResult(false);
    }

    protected virtual void ObserveChanges(TStateRecord oldState, TStateRecord newState)
    {
        // nothing
    }

    protected Task Dispatch(IGrainEvent @event)
    {
        return _dispatch.Dispatch(this, @event);
    }

    protected TStateRecord State => _state;
    
    object IGrain.GetState()
    {
        return _state!;
    }

    void IGrain.SetState(object state)
    {
        _state = (TStateRecord)state;
    }

    object IGrain.Reduce(object state, IGrainEvent @event)
    {
        return Reduce((TStateRecord) state, @event)!;
    }

    void IGrain.ObserveChanges(object oldState, object newState)
    {
        ObserveChanges((TStateRecord)oldState, (TStateRecord)newState);
    }

    Task<bool> IGrain.ExecuteWorkItem(IGrainWorkItem workItem)
    {
        return ExecuteWorkItem(workItem);
    }
}
