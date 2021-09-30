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
        private readonly ICommsLogger _logger;

        public RuntimeRadioStationFactory(IWorldContext world, IRuntimeStateStore store, ICommsLogger logger)
        {
            _world = world;
            _store = store;
            _logger = logger;
        }

        public RuntimeRadioStation CreateStation(
            Func<GeoPoint> getLocation,
            Func<Altitude> getElevation,
            Frequency frequency, 
            string name,
            string callsign)
        {
            var nextStationId = _state.LastStationId + 1;
            var uniqueStationId = _world.CreateUniqueId(nextStationId);
            var station = new RuntimeRadioStation(
                _world, _store, _logger, uniqueStationId, getLocation, getElevation, frequency, name, callsign);
            
            _store.Dispatch(new UpdateLastStationId(nextStationId));
            return station;
        }

        public RuntimeRadioStation CreateGroundStation(
            GeoPoint location,
            Altitude elevation,
            Frequency frequency, 
            string name,
            string callsign,
            out GroundRadioStationAether aether)
        {
            var station = CreateStation(() => location, () => elevation, frequency, name, callsign);
            aether = new GroundRadioStationAether(_world, _store, _logger, station);
            return station;
        }
    }
}
