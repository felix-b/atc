using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Zero.Loss.Actors.Impl
{
    public partial class SupervisorActor : StatefulActor<SupervisorActor.SupervisorState>, IInternalSupervisorActor
    {
        public record SupervisorState(
            ImmutableDictionary<string, ActorEntry> ActorByUniqueId,
            ImmutableDictionary<string, ulong> LastInstanceIdPerTypeString,
            IStatefulActor? LastCreatedActor
        ) : IStateEvent;

        public record ActorEntry(IStatefulActor Actor, IActivationStateEvent ActivationEvent);

        private record DummySelfActivationEvent(string UniqueId) : IActivationStateEvent;

        public record DeactivateActorEvent(
            string UniqueId
        ) : IStateEvent;

        private record ClearLastCreatedActorEvent : IStateEvent;
        
        public bool TryGetActorObjectById<TActor>(string uniqueId, out TActor? actor) where TActor : class, IStatefulActor 
        {
            var result = State.ActorByUniqueId.TryGetValue(uniqueId, out var entry);
            if (result && entry!.Actor is TActor typedActor)
            {
                actor = typedActor;
                return true;
            }

            actor = null;
            return false;
        }

        public bool TryGetActorById<TActor>(string uniqueId, out ActorRef<TActor>? actorRef) where TActor : class, IStatefulActor
        {
            if (TryGetActorObjectById<TActor>(uniqueId, out var actorObj))
            {
                actorRef = new ActorRef<TActor>(this, actorObj!.UniqueId);
                return true;
            }

            actorRef = null;
            return false;
        }

        public IEnumerable<ActorRef<TActor>> GetAllActorsOfType<TActor>() where TActor : class, IStatefulActor
        {
            return State.ActorByUniqueId.Values
                .Select(entry => entry.Actor)
                .OfType<TActor>()
                .Select(actor => new ActorRef<TActor>(this, actor.UniqueId));
        }
        
        protected override SupervisorState Reduce(SupervisorState state, IStateEvent @event)
        {
            switch (@event)
            {
                case IActivationStateEvent activation:
                    var actor = ActivateActor(activation, out var typeString);
                    if (!state.LastInstanceIdPerTypeString.TryGetValue(typeString, out var lastInstanceId))
                    {
                        lastInstanceId = 0;
                    }
                    return state with {
                        ActorByUniqueId = state.ActorByUniqueId.Add(activation.UniqueId, new ActorEntry(actor, activation)),
                        LastInstanceIdPerTypeString = state.LastInstanceIdPerTypeString.SetItem(typeString, lastInstanceId + 1),
                        LastCreatedActor = actor
                    };
                case DeactivateActorEvent deactivation:
                    if (!state.ActorByUniqueId.TryGetValue(deactivation.UniqueId, out var entryToDeactivate))
                    {
                        throw new ActorNotFoundException($"Failed to deactivate actor '{deactivation.UniqueId}': no such actor");
                    }
                    // ReSharper disable once SuspiciousTypeConversion.Global
                    if (entryToDeactivate.Actor is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    return state with {
                        ActorByUniqueId = state.ActorByUniqueId.Remove(deactivation.UniqueId)
                    };
                case ClearLastCreatedActorEvent:
                    return state with {
                        LastCreatedActor = null
                    };
                default:
                    return state;
            }
        }

        private IStatefulActor ActivateActor(IActivationStateEvent @event, out string typeString)
        {
            if (!_registrationByActivationEventType.TryGetValue(@event.GetType(), out var registration))
            {
                throw new ActorTypeNotFoundException($"Activation event of type '{@event.GetType().Name}' was not registered");
            }

            typeString = registration.TypeString;
            return registration.Factory(@event);
        }

        private static SupervisorState CreateInitialState()
        {
            return new SupervisorState(
                ActorByUniqueId: ImmutableDictionary<string, ActorEntry>.Empty,
                LastInstanceIdPerTypeString: ImmutableDictionary<string, ulong>.Empty,
                LastCreatedActor: null
            );
        }
    }
}
