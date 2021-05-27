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
    public partial class RuntimeAircraft
    {
        private readonly IRuntimeStateStore _store;
        private RuntimeState _state; 

        public RuntimeAircraft(IRuntimeStateStore store, ZRef<AircraftData> data)
        {
            throw new NotImplementedException();
        }

        public RuntimeAircraft(IRuntimeStateStore store, RuntimeState initialState)
        {
            _store = store;
            _state = initialState;
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

        public int Id => _state.Id;
        public ZRef<AirlineData>? AirlineData { get; init; }
        public ZRef<AircraftTypeData>? TypeData { get; init; }
    }
}