namespace Atc.World.Redux
{
    public interface IRuntimeStateStore
    {
        void Dispatch(IRuntimeStateEvent stateEvent);
        void Dispatch<TState>(IHaveRuntimeState<TState> target, IRuntimeStateEvent stateEvent)
            where TState : class;
    }
}
