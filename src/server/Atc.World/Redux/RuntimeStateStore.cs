using System;

namespace Atc.World.Redux
{
    public class RuntimeStateStore : IRuntimeStateStore
    {
        public void Dispatch(IRuntimeStateEvent stateEvent) 
        {
            // note: all reducers subscribe to "find me" data structure
            // this is required for event log replication, in order to dispatch state updates on a follower side
            // 1. lookup the "find me" data structure and locate the target reducer
            // 2. invoke the target reducer with the second overload of Dispatch
            throw new System.NotImplementedException();
        }

        public void Dispatch<TState>(IHaveRuntimeState<TState> target, IRuntimeStateEvent stateEvent) 
            where TState : class
        {
            var state0 = target.GetState();
            var state1 = target.Reduce(state0, stateEvent);
            
            if (!object.ReferenceEquals(state0, state1))
            {
                target.SetState(state1);
                StateEventDispatched?.Invoke(stateEvent);
            }
        }

        // This event is useful for replication of state events to followers
        // and also for recording state events with the purpose of replaying them later
        public event Action<IRuntimeStateEvent>? StateEventDispatched;
    }
}
