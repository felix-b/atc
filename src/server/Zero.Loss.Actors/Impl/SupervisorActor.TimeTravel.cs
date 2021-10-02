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

        #if false
        public bool MatchSnapshot(ActorStateSnapshot snapshot, List<string> mismatches)
        {
            mismatches.Clear();

            if (_stateStore.NextSequenceNo != snapshot.NextSequenceNo)
            {
                mismatches.Add($"NextSequenceNo: store has {_stateStore.NextSequenceNo}, snapshot has {snapshot.NextSequenceNo}");
            }

            SnapshotData data = (SnapshotData) snapshot.Opaque;

            MatchLastInstanceIdPerTypeString();
            MatchActorsAndState();

            return (mismatches.Count == 0);

            void MatchActorsAndState()
            {
                var snapshotActorIds = new HashSet<string>();
                
                foreach (var snapshotEntry in data.ActorEntries)
                {
                    snapshotActorIds.Add(snapshotEntry.UniqueId);
                    
                    if (State.ActorByUniqueId.TryGetValue(snapshotEntry.UniqueId, out var actorEntry))
                    {
                        MatchImmutableObjects(
                            $"Actor '{snapshotEntry.UniqueId}' state", 
                            snapshotEntry.State,
                            actorEntry.Actor.GetState());
                        MatchImmutableObjects(
                            $"Actor '{snapshotEntry.UniqueId}' activation event", 
                            snapshotEntry.ActivationEvent,
                            actorEntry.ActivationEvent);
                    }
                    else
                    {
                        mismatches.Add($"Actor '{snapshotEntry.UniqueId}' missing in supervisor");
                    }
                }

                foreach (var unexpectedId in State.ActorByUniqueId.Keys.Where(id => !snapshotActorIds.Contains(id)))
                {
                    mismatches.Add($"Unexpected actor in supervisor: '{unexpectedId}'");
                }
            }
            
            void MatchLastInstanceIdPerTypeString()
            {
                foreach (var typeIdPair in data.LastInstanceIdPerTypeString)
                {
                    if (State.LastInstanceIdPerTypeString.TryGetValue(typeIdPair.Key, out var value))
                    {
                        if (value != typeIdPair.Value)
                        {
                            mismatches.Add(
                                $"LastInstanceIdPerTypeString[{typeIdPair.Key}]: supervisor has {value}, snapshot has {typeIdPair.Value}");
                        }
                    }
                    else
                    {
                        mismatches.Add($"LastInstanceIdPerTypeString[{typeIdPair.Key}]: entry missing in supervisor");
                    }
                }

                foreach (var unexpectedKey in State.LastInstanceIdPerTypeString.Keys.Where(
                    key => !data.LastInstanceIdPerTypeString.ContainsKey(key)))
                {
                    mismatches.Add($"LastInstanceIdPerTypeString[{unexpectedKey}]: unexpected entry in supervisor");
                }
            }

            void MatchImmutableObjects(string name, string path, object? expected, object? actual)
            {
                if (expected == null || actual == null)
                {
                    if ((actual == null) != (expected == null))
                    {
                        mismatches.Add(
                            $"{name} at {path}: expected {(expected == null ? "null" : "not-null")}, actual {(actual == null ? "null" : "not-null")}");
                    }
                    return;
                }

                if (actual.GetType() != expected.GetType())
                {
                    mismatches.Add(
                        $"{name}: expected '', found ''");
                }
            }
        }
        #endif

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
