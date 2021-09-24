using System;
using Atc.Data.Primitives;
using Atc.Speech.Abstractions;
using Atc.World.Redux;

namespace Atc.World.Comms
{
    public partial class RuntimeRadioStationFactory
    {
        private readonly IWorldContext _world;
        private readonly IRuntimeStateStore _store;

        public RuntimeRadioStationFactory(IWorldContext world, IRuntimeStateStore store)
        {
            _world = world;
            _store = store;
        }

        public RuntimeRadioStation CreateStation(
            RuntimeRadioEther ether,
            Func<GeoPoint> getLocation,
            Func<Altitude> getElevation,
            Frequency frequency)
        {
            var stationId = _world.CreateUniqueId(_state.LastStationId);
            var station = new RuntimeRadioStation(_world, _store, ether, stationId, getLocation, getElevation, frequency);
            
            _store.Dispatch(new UpdateLastStationId(stationId));
            return station;
        }
    }
}
