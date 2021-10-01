using System;

namespace Zero.Loss.Actors
{
    public interface IStateStore
    {
        void Dispatch(IStatefulActor target, IStateEvent @event);
        void Dispatch<TTarget>(in ActorRef<TTarget> targetRef, IStateEvent @event) where TTarget : class, IStatefulActor
        {
            Dispatch(targetRef.Get(), @event);
        }
    }

    public interface IStateStoreInit
    {
        void AddEventListener(StateEventListener listener, out int listenerId);
        void RemoveEventListener(int listenerId);
    }

    public readonly struct StateEventEnvelope
    {
        public StateEventEnvelope(ulong sequenceNo, string targetUniqueId, IStateEvent @event)
        {
            SequenceNo = sequenceNo;
            TargetUniqueId = targetUniqueId;
            Event = @event;
        }

        public readonly ulong SequenceNo;
        public readonly string TargetUniqueId;
        public readonly IStateEvent Event;
    }
    
    public delegate void StateEventListener(in StateEventEnvelope envelope);
}
