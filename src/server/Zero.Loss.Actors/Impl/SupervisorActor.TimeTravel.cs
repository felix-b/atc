using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Zero.Loss.Actors.Impl
{
    public partial class SupervisorActor : ISupervisorActorTimeTravel
    {
        void ISupervisorActorTimeTravel.RestoreSnapshot(ActorStateSnapshot snapshot)
        {
            SnapshotData data = (SnapshotData) snapshot.Opaque;
            
            _stateStore.ResetNextSequenceNo(snapshot.NextSequenceNo);
            var rebuiltState = RebuildSupervisorState();
            ((IStatefulActor) this).SetState(rebuiltState);

            SupervisorState RebuildSupervisorState()
            {
                var actorByIdBuilder = ImmutableDictionary.CreateBuilder<string, ActorEntry>();
                actorByIdBuilder.Add(UniqueId, State.ActorByUniqueId[UniqueId]);
                
                foreach (var snapshotEntry in data.ActorEntries)
                {
                    var actorEntry = RebuildActorEntry(snapshotEntry);
                    actorByIdBuilder.Add(snapshotEntry.UniqueId, actorEntry);
                    
                    var currentState = actorEntry.Actor.GetState();
                    actorEntry.Actor.SetState(snapshotEntry.State);
                    actorEntry.Actor.ObserveChanges(currentState, snapshotEntry.State);
                }

                return new SupervisorState(
                    ActorByUniqueId: actorByIdBuilder.ToImmutable(),
                    LastInstanceIdPerTypeString: data.LastInstanceIdPerTypeString,
                    LastCreatedActor: null);
            }
            
            ActorEntry RebuildActorEntry(SnapshotDataActorEntry snapshotEntry)
            {
                if (State.ActorByUniqueId.TryGetValue(snapshotEntry.UniqueId, out var existingActorEntry))
                {
                    return existingActorEntry;
                }
                    
                if (_registrationByActivationEventType.TryGetValue(snapshotEntry.ActivationEvent.GetType(), out var registration))
                {
                    var resurrectedActor = registration.Factory(snapshotEntry.ActivationEvent);
                    return new ActorEntry(resurrectedActor, snapshotEntry.ActivationEvent);
                }

                throw new ActorTypeNotFoundException();
            }
        }

        void ISupervisorActorTimeTravel.ReplayEvents(IEnumerable<StateEventEnvelope> events)
        {
            foreach (var envelope in events)
            {
                if (envelope.SequenceNo != _stateStore.NextSequenceNo)
                {
                    throw new EventOutOfSequenceException(
                        $"Expected event sequence no. to be {_stateStore.NextSequenceNo}, but got #{envelope.SequenceNo}");
                }
                
                if (!State.ActorByUniqueId.TryGetValue(envelope.TargetUniqueId, out var targetEntry))
                {
                    throw new ActorNotFoundException(
                        $"Cannot replay event #{envelope.SequenceNo} because target actor '{envelope.TargetUniqueId}' not found");
                }
                
                _stateStore.Dispatch(targetEntry.Actor, envelope.Event);
            }
        }

        ActorStateSnapshot ISupervisorActorTimeTravel.TakeSnapshot()
        {
            var actorEntries = State.ActorByUniqueId
                .Values
                .Where(entry => entry.Actor != this)
                .Select(entry => {
                    var actor = entry.Actor;
                    return new SnapshotDataActorEntry(
                        actor.UniqueId,
                        entry.ActivationEvent,
                        actor.GetState());
                })
                .ToImmutableArray();

            var opaqueData = new SnapshotData(
                State.LastInstanceIdPerTypeString, 
                actorEntries);

            return new ActorStateSnapshot(                
                _stateStore.NextSequenceNo, 
                opaqueData);
        }

        public ISupervisorActorTimeTravel TimeTravel => this;

        private record SnapshotData(
            ImmutableDictionary<string, ulong> LastInstanceIdPerTypeString,
            ImmutableArray<SnapshotDataActorEntry> ActorEntries);
        
        private record SnapshotDataActorEntry(
            string UniqueId, 
            IActivationStateEvent ActivationEvent, 
            object State);
        
    }
}
