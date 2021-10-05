using System;
using Atc.Data;
using Atc.Data.Primitives;
using Atc.World.Comms;
using Zero.Loss.Actors;

namespace Atc.World.Tests
{
    public class TestWorldContext : IWorldContext
    {
        public ActorRef<GroundRadioStationAetherActor>? TryFindRadioAether(ActorRef<RadioStationActor> fromStation)
        {
            return OnTryFindRadioAether(fromStation);
        }

        public ActorRef<AircraftActor> SpawnNewAircraft(
            string typeIcao, 
            string tailNo, 
            string? callsign, 
            string? airlineIcao, 
            AircraftCategories category,
            OperationTypes operations, 
            GeoPoint location, 
            Altitude altitude, 
            Bearing heading, 
            Bearing? track = null,
            Speed? groundSpeed = null, 
            Angle? pitch = null, 
            Angle? roll = null)
        {
            throw new NotImplementedException();
        }

        public IDeferHandle DeferBy(TimeSpan time, Action action)
        {
            throw new NotImplementedException();
        }

        public IDeferHandle DeferUntil(DateTime utc, Action action)
        {
            throw new NotImplementedException();
        }

        public IDeferHandle DeferUntil(Func<bool> predicate, DateTime utc, Action onPredicateTrue, Action onTimeout)
        {
            throw new NotImplementedException();
        }

        public DateTime UtcNow()
        {
            return PresetUtcNow;
        }

        public DateTime PresetUtcNow { get; set; } = DateTime.UtcNow;

        public Func<ActorRef<RadioStationActor>, ActorRef<GroundRadioStationAetherActor>?> OnTryFindRadioAether { get; set; } = 
            (station) => null;
    }
}