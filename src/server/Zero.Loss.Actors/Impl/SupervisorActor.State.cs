using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Zero.Loss.Actors.Impl
{
    public partial class SupervisorActor : StatefulActor<SupervisorActor.SupervisorState>
    {
        private IStatefulActor? _lastCreatedActorTemp = null;
        
        public record SupervisorState(
            ImmutableDictionary<string, IStatefulActor> ActorByUniqueId,
            ImmutableDictionary<string, ulong> LastInstanceIdPerTypeString
        ) : IStateEvent;

        public record DeactivateActorEvent(
            string UniqueId
        ) : IStateEvent;
        
        public bool TryGetActorById<TActor>(string uniqueId, out TActor? actor) where TActor : class, IStatefulActor
        {
            var result = State.ActorByUniqueId.TryGetValue(uniqueId, out var untypedActor);
            if (result && untypedActor is TActor typedActor)
            {
                actor = typedActor;
                return true;
            }

            actor = null;
            return false;
        }

        public IEnumerable<TActor> GetAllActorsOfType<TActor>() where TActor : class, IStatefulActor
        {
            return State.ActorByUniqueId.Values.OfType<TActor>();
        }
        
        protected override SupervisorState Reduce(SupervisorState state, IStateEvent @event)
        {
            switch (@event)
            {
                case IActivationStateEvent activation:
                    _lastCreatedActorTemp = ActivateActor(@event, out var typeString);
                    if (!state.LastInstanceIdPerTypeString.TryGetValue(typeString, out var lastInstanceId))
                    {
                        lastInstanceId = 0;
                    }
                    return state with {
                        ActorByUniqueId = state.ActorByUniqueId.Add(activation.UniqueId, _lastCreatedActorTemp),
                        LastInstanceIdPerTypeString = state.LastInstanceIdPerTypeString.SetItem(typeString, lastInstanceId + 1)
                    };
                case DeactivateActorEvent deactivation:
                    if (!state.ActorByUniqueId.TryGetValue(deactivation.UniqueId, out var actorToDeactivate))
                    {
                        throw new KeyNotFoundException($"Failed to deactivate actor '{deactivation.UniqueId}': no such actor");
                    }
                    if (actorToDeactivate is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    return state with {
                        ActorByUniqueId = state.ActorByUniqueId.Remove(deactivation.UniqueId)
                    };
                default:
                    return state;
            }
        }

        private IStatefulActor ActivateActor(IStateEvent @event, out string typeString)
        {
            throw new NotImplementedException();
        }

        private static SupervisorState CreateInitialState()
        {
            return new SupervisorState(
                ActorByUniqueId: ImmutableDictionary<string, IStatefulActor>.Empty,
                LastInstanceIdPerTypeString: ImmutableDictionary<string, ulong>.Empty
            );
        }
    }
}
