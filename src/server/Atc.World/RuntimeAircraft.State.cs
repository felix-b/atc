using System;
using System.Runtime.CompilerServices;
using Atc.Data;
using Atc.Data.Primitives;
using Atc.Data.Traffic;
using Atc.Math;
using Atc.World.Redux;
using Microsoft.AspNetCore.Http;
using ProtoBuf;
using ProtoBuf.WellKnownTypes;
using Zero.Serialization.Buffers;

namespace Atc.World
{
    public partial class RuntimeAircraft : IHaveRuntimeState<RuntimeAircraft.RuntimeState>
    {
        public RuntimeState GetState()
        {
            return _state;
        }

        void IHaveRuntimeState<RuntimeState>.SetState(RuntimeState newState)
        {
            _state = newState;
        }

        RuntimeState IHaveRuntimeState<RuntimeState>.Reduce(RuntimeState currentState, IRuntimeStateEvent stateEvent)
        {
            switch (stateEvent)
            {
                case MovedEvent moved:
                    return currentState with {
                        Version = currentState.Version + 1,
                        Location = moved.NewLocation
                    };
                default:
                    return currentState;
            }
        }

        public record RuntimeState(
            int Version,
            GeoPoint Location,
            Altitude Altitude, 
            Angle Pitch, 
            Angle Roll, 
            Bearing Heading, 
            Bearing Track, 
            Speed GroundSpeed 
        );

        public record MovedEvent(
            GeoPoint NewLocation
        ) : IRuntimeStateEvent;
    }
}
