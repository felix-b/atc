using System;
using System.Runtime.CompilerServices;
using Atc.Data;
using Atc.Data.Primitives;
using Atc.Data.Traffic;
using Atc.Math;
using Atc.World.Comms;
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
                case AvionicsPoweredOnEvent avionicsOn:
                    return currentState with {
                        Version = currentState.Version + 1,
                        AvionicsPoweredOn = true,
                        Com1Frequency = avionicsOn.Com1Frequency,
                        Com1Station = _ether.CreateAircraftStation(this, avionicsOn.Com1Frequency)
                    };
                case AvionicsPoweredOffEvent:
                    if (currentState.Com1Station != null)
                    {
                        _ether.RemoveStation(currentState.Com1Station.Id);
                    }
                    return currentState with {
                        Version = currentState.Version + 1,
                        AvionicsPoweredOn = false,
                        Com1Station = null
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
            Speed GroundSpeed,
            bool AvionicsPoweredOn,
            Frequency Com1Frequency,
            RuntimeRadioStation? Com1Station
        );
        
        public record MovedEvent(
            GeoPoint NewLocation
        ) : IRuntimeStateEvent;

        public record AvionicsPoweredOnEvent(
            Frequency Com1Frequency
        ) : IRuntimeStateEvent;

        public record AvionicsPoweredOffEvent : IRuntimeStateEvent;
    }
}
