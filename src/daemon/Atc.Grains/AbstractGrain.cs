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

    protected virtual bool ShouldExecuteWorkItem(IGrainWorkItem workItem)
    {
        return false;
    }

    protected virtual bool ExecuteWorkItem(IGrainWorkItem workItem, bool timedOut)
    {
        return false;
    }

    protected virtual void ObserveChanges(TStateRecord oldState, TStateRecord newState)
    {
        // nothing
    }

    protected void Dispatch(IGrainEvent @event)
    {
        _dispatch.Dispatch(this, @event);
    }

    protected GrainWorkItemHandle Defer(
        IGrainWorkItem workItem,
        DateTime? notEarlierThanUtc = null,
        DateTime? notLaterThanUtc = null,
        bool withPredicate = false)
    {
        return _dispatch.TaskQueue.Defer(
            this, 
            workItem, 
            notEarlierThanUtc: notEarlierThanUtc,
            notLaterThanUtc: notLaterThanUtc,
            withPredicate: withPredicate);
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

    bool IGrain.ShouldExecuteWorkItem(IGrainWorkItem workItem)
    {
        return ShouldExecuteWorkItem(workItem);
    }

    bool IGrain.ExecuteWorkItem(IGrainWorkItem workItem, bool timedOut)
    {
        return ExecuteWorkItem(workItem, timedOut);
    }
}
