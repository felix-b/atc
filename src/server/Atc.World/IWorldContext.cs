using System;
using Atc.Data;
using Atc.Data.Primitives;
using Atc.World.Comms;
using Zero.Loss.Actors;

namespace Atc.World
{
    public interface IWorldContext
    {
        ActorRef<GroundRadioStationAetherActor> AddGroundStation(ActorRef<RadioStationActor> station);

        ActorRef<GroundRadioStationAetherActor>? TryFindRadioAether(
            ActorRef<RadioStationActor> fromStation, 
            Frequency? newFrequency);

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
        
        IDeferHandle Defer(string description, Action action);
        IDeferHandle DeferBy(string description, TimeSpan time, Action action);
        IDeferHandle DeferUntil(string description, DateTime utc, Action action);
        IDeferHandle DeferUntil(string description, Func<bool> predicate, DateTime deadlineUtc, Action onPredicateTrue, Action onTimeout);
        
        DateTime UtcNow();
    }

    public interface IDeferHandle
    {
        void UpdateDeadline(DateTime newDeadlineUtc);
        void Cancel();
        static IDeferHandle Noop => NoopDeferHandle.Instance;
    }

    internal class NoopDeferHandle : IDeferHandle
    {
        public static readonly NoopDeferHandle Instance = new NoopDeferHandle();

        private NoopDeferHandle()
        {
        }
        
        public void UpdateDeadline(DateTime newDeadlineUtc)
        {
        }

        public void Cancel()
        {
        }
    }
}
