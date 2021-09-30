namespace Zero.Loss.Actors
{
    public interface IStatefulActor
    {
        object GetState();
        object Reduce(object state, IStateEvent @event);
        void SetState(object newState);
        void ObserveChanges(object oldState, object newState);
        string UniqueId { get; }
    }

    public interface IStateEvent
    {
    }

    public interface IActivationStateEvent : IStateEvent
    {
        string UniqueId { get; }
    }

    public interface IActivationStateEvent<TActor> : IActivationStateEvent 
        where TActor : class, IStatefulActor
    {
    }

    public interface IStatefulActor<TState> : IStatefulActor
        where TState : class
    {
    }
}