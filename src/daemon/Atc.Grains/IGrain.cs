namespace Atc.Grains;

public interface IGrainId
{
    string GrainId { get; }
    string GrainType { get; }
}

public interface IGrain : IGrainId
{
    object GetState();
    void SetState(object state);
    object Reduce(object state, IGrainEvent @event);
    void ObserveChanges(object oldState, object newState);
    bool ShouldExecuteWorkItem(IGrainWorkItem workItem);
    bool ExecuteWorkItem(IGrainWorkItem workItem, bool timedOut);
}

public interface IStartableGrain : IGrain
{
    void Start();
}
