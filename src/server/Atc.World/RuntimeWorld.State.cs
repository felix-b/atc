using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Atc.Data;
using Atc.Data.Primitives;
using Atc.World.Redux;
using Zero.Latency.Servers;

namespace Atc.World
{
    public partial class RuntimeWorld : IHaveRuntimeState<RuntimeWorld.RuntimeState>
    {
        RuntimeState IHaveRuntimeState<RuntimeState>.Reduce(RuntimeState currentState, IRuntimeStateEvent stateEvent)
        {
            switch (stateEvent)
            {
                case AircraftAddedEvent aircraftAdded:
                    var newAircraft = new RuntimeAircraft(_store, aircraftAdded.ToAircraftInitialState());
                    
                    // here we're sinful of mutating a HashSet<T> rather than transforming an ImmutableHashSet<T>
                    // this is because we might be dealing with a very large number of items 
                    currentState.AllAircraft.Add(newAircraft); 
                    
                    return currentState with {
                        Version = currentState.Version + 1                      
                    };
                default:
                    return currentState;
            }
        }

        RuntimeState IHaveRuntimeState<RuntimeState>.GetState() => _state;

        void IHaveRuntimeState<RuntimeState>.SetState(RuntimeState newState) => _state = newState;

        public record RuntimeState(
            ulong Version,
            int NextAircraftId,
            HashSet<RuntimeAircraft> AllAircraft
        );

        // only used for replication to followers, sent once per every N ticks
        // not handled by the leader
        // the leader updates these values internally without the event
        // (otherwise this event would have to occur tens times per second)
        public record TimeUpdateEvent(
            ulong TickCount,
            TimeSpan Timestamp
        ) : IRuntimeStateEvent;
        
        public partial record AircraftAddedEvent(
            int Id,
            string TypeIcao,
            string TailNo,
            string LiveryId,
            AircraftCategories Category,
            OperationTypes Operations,
            GeoPoint Location,
            Altitude Altitude,
            Angle Pitch,
            Angle Roll,
            Bearing Heading,
            Bearing Track,
            Speed GroundSpeed
        ) : IRuntimeStateEvent;

        public partial record AircraftAddedEvent
        {
            public RuntimeAircraft.RuntimeState ToAircraftInitialState()
            {
                return new(
                    Version: 1,
                    Id,
                    TypeIcao,
                    TailNo,
                    LiveryId,
                    Category,
                    Operations,
                    Location,
                    Altitude,
                    Pitch,
                    Roll,
                    Heading,
                    Track,
                    GroundSpeed                    
                );
            }
        }
    }
}