using System;
using System.Runtime.CompilerServices;
using Atc.Data;
using Atc.Data.Primitives;
using Atc.Data.Traffic;
using Atc.Math;
using Atc.World.Comms;
using Atc.World.Redux;
using Geo.Gps;
using Microsoft.AspNetCore.Http;
using ProtoBuf;
using ProtoBuf.WellKnownTypes;
using Zero.Serialization.Buffers;
using Zero.Serialization.Buffers.Impl;

namespace Atc.World
{
    public partial class RuntimeAircraft
    {
        private readonly IRuntimeStateStore _store;
        private readonly RuntimeRadioEther _ether;
        private readonly uint _id;
        private readonly string _tailNo;
        private readonly AircraftData _data;
        private RuntimeState _state; 

        public RuntimeAircraft(
            IRuntimeStateStore store, 
            RuntimeRadioEther ether,
            ZRef<AircraftData> dataRef,
            GeoPoint location,
            Altitude altitude,
            Bearing heading,
            Bearing? track = null,
            Speed? groundSpeed = null,
            Angle? pitch = null,
            Angle? roll = null)
        {
            ref var data = ref dataRef.Get();

            _id = data.Id;
            _tailNo = data.TailNo.Value;

            _store = store;
            _ether = ether;
            _state = CreateInitialStateFromData(
                ref data, 
                location, 
                altitude, 
                heading, 
                track, 
                groundSpeed, 
                pitch, 
                roll);
        }

        public RuntimeAircraft(
            IRuntimeStateStore store, 
            RuntimeRadioEther ether,
            RuntimeWorld.AircraftAddedEvent addedEvent)
        {
            _store = store;
            _ether = ether;
            _state = CreateInitialStateFromEvent(addedEvent, out _data);

            _id = addedEvent.Id;
            _tailNo = addedEvent.TailNo;
        }

        public void ProgressBy(TimeSpan delta)
        {
            var groundDistance = Distance.FromNauticalMiles(_state.GroundSpeed.Knots * delta.TotalHours);
            GeoMath.CalculateGreatCircleDestination(
                _state.Location,
                _state.Track,
                groundDistance,
                out var newLocation);

            _store.Dispatch(this, new MovedEvent(newLocation));
        }

        public void PowerAvionicsOn(Frequency com1Frequency)
        {
            _store.Dispatch(new AvionicsPoweredOnEvent(com1Frequency));            
        }

        public void PowerAvionicsOff()
        {
            _store.Dispatch(new AvionicsPoweredOffEvent());            
        }

        public uint Id => _id;
        public string TailNo => _tailNo;
        public string TypeIcao => _data.Type.Get().Icao;
        public ZRef<AirlineData>? AirlineData { get; init; }
        public ZRef<AircraftTypeData>? TypeData { get; init; }

        private static RuntimeState CreateInitialStateFromData(
            ref AircraftData data,
            GeoPoint location,
            Altitude altitude,
            Bearing heading,
            Bearing? track,
            Speed? groundSpeed,
            Angle? pitch,
            Angle? roll)
        {
            var context = BufferContext.Current;
            ref var worldData = ref context.GetWorldData();

            return new(
                Version: 1,
                Location: location, 
                Altitude: altitude,
                Pitch: pitch ?? Angle.FromDegrees(0),
                Roll: roll ?? Angle.FromDegrees(0), 
                Heading: heading, 
                Track: track ?? heading, 
                GroundSpeed: groundSpeed ?? Speed.FromKnots(0),
                AvionicsPoweredOn: false,
                Com1Frequency: Frequency.FromKhz(120500), 
                Com1Station: null
            );
        }

        private static RuntimeState CreateInitialStateFromEvent(RuntimeWorld.AircraftAddedEvent e, out AircraftData data)
        {
            var context = BufferContext.Current;
            ref var worldData = ref context.GetWorldData();

            ZRef<AircraftData>? dataRef = e.ModeS.HasValue
                ? worldData.AircraftByModeS[(int)e.ModeS.Value]
                : null;

            context.TryGetString(e.LiveryId, out var liveryIdRef);
            
            data = dataRef.HasValue
                ? dataRef.Value.Get()
                : new AircraftData {
                    Id = e.Id,
                    Type = worldData.TypeByIcao[e.TypeIcao],
                    ModeS = e.ModeS, 
                    Category = e.Category,
                    Operations = e.Operations,
                    Airline = e.AirlineIcao != null
                        ? worldData.AirlineByIcao[e.AirlineIcao] 
                        : null,
                    LiveryId = liveryIdRef
                };
                
            return new(
                Version: 1,
                Location: e.Location,
                Altitude: e.Altitude,
                Pitch: e.Pitch,
                Roll: e.Roll,
                Heading: e.Heading,
                Track: e.Track,
                GroundSpeed: e.GroundSpeed,
                AvionicsPoweredOn: false,
                Com1Frequency: Frequency.FromKhz(120500), 
                Com1Station: null
            );
        }
    }
}
