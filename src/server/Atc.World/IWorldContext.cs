using System;
using Atc.Data;
using Atc.Data.Primitives;
using Atc.World.Comms;
using Zero.Loss.Actors;

namespace Atc.World
{
    public interface IWorldContext
    {
        ActorRef<GroundRadioStationAetherActor>? TryFindRadioAether(ActorRef<RadioStationActor> fromStation);

        ActorRef<AircraftActor> SpawnNewAircraft(
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
            Angle? roll = null);
        
        IDeferHandle DeferBy(TimeSpan time, Action action);
        IDeferHandle DeferUntil(DateTime utc, Action action);
        IDeferHandle DeferUntil(Func<bool> predicate, DateTime utc, Action onPredicateTrue, Action onTimeout);
        
        DateTime UtcNow();
    }

    public interface IDeferHandle
    {
        void Cancel();
    }
}
