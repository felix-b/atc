using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Atc.Data;
using Atc.Data.Primitives;
using Atc.Data.Traffic;
using Atc.World.Redux;
using Zero.Latency.Servers;
using Zero.Serialization.Buffers;
using Zero.Serialization.Buffers.Impl;

namespace Atc.World
{
    public partial class RuntimeWorld : IHaveRuntimeState<RuntimeWorld.RuntimeState>
    {
        RuntimeState IHaveRuntimeState<RuntimeState>.Reduce(RuntimeState currentState, IRuntimeStateEvent stateEvent)
        {
            switch (stateEvent)
            {
                case AircraftAddedEvent aircraftAdded:
                    var newAircraft = new RuntimeAircraft(this, _store, aircraftAdded);
                    
                    // here we're sinful of mutating a HashSet<T> rather than transforming an ImmutableHashSet<T>
                    // this is because we might be dealing with a very large number of items 
                    currentState.AircraftById.Add(newAircraft.Id, newAircraft); 
                    
                    return currentState with {
                        Version = currentState.Version + 1,
                        NextAircraftId = currentState.NextAircraftId + 1
                    };
                default:
                    return currentState;
            }
        }

        RuntimeState IHaveRuntimeState<RuntimeState>.GetState() => _state;

        void IHaveRuntimeState<RuntimeState>.SetState(RuntimeState newState) => _state = newState;

        
        public record RuntimeState(
            ulong Version,
            uint NextAircraftId,
            Dictionary<uint, RuntimeAircraft> AircraftById
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
            uint Id,
            string TypeIcao,
            string TailNo,
            string Callsign,
            uint? ModeS, 
            AircraftCategories Category,
            OperationTypes Operations,
            string? AirlineIcao,
            string LiveryId,
            GeoPoint Location,
            Altitude Altitude,
            Angle Pitch,
            Angle Roll,
            Bearing Heading,
            Bearing Track,
            Speed GroundSpeed
        ) : IRuntimeStateEvent;
    }
}