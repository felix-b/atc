namespace Atc.World.Redux
{
    public interface IHaveRuntimeState<TState>
        where TState : class
    {
        TState Reduce(TState currentState, IRuntimeStateEvent stateEvent);
        TState GetState();
        void SetState(TState newState);
    }

    public interface IRuntimeStateEvent
    {
    }
}
