using System;

namespace Zero.Loss.Actors
{
    public interface IStateStore
    {
        void Dispatch(IStatefulActor target, IStateEvent @event);
    }

    public interface IStateStoreInit
    {
        void AddEventListener(StateEventListener listener, out int listenerId);
        void RemoveEventListener(int listenerId);
    }

    public delegate void StateEventListener(ulong sequenceNo, IStateEvent @event);
}
