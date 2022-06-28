namespace Atc.Grains;

public interface IGrain
{
    string GrainId { get; }
    string GrainType { get; }
    object GetState();
    void SetState(object state);
    object Reduce(object state, IGrainEvent @event);
    void ObserveChanges(object oldState, object newState);
    Task<bool> ShouldExecuteWorkItem(IGrainWorkItem workItem);
    Task<bool> ExecuteWorkItem(IGrainWorkItem workItem, bool timedOut);
}

public interface IStartableGrain : IGrain
{
    void Start();
}
