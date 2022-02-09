using System;
using Atc.Data;
using Atc.Data.Primitives;
using Atc.World.Abstractions;
using Atc.World.Comms;
using Atc.World.Traffic;
using Zero.Loss.Actors;

namespace Atc.World
{
    public interface IWorldContext
    {
        ActorRef<GroundRadioStationAetherActor> AddGroundStation(ActorRef<RadioStationActor> station);

        ActorRef<GroundRadioStationAetherActor>? TryFindRadioAether(
            ActorRef<RadioStationActor> fromStation, 
            Frequency? newFrequency);

        ActorRef<AircraftActor> GetAircraftByActorId(string uniqueId);

        ActorRef<Traffic.AircraftActor> SpawnNewAircraft(
            string typeIcao,
            string tailNo,
            string? callsign,
            string? airlineIcao,
            AircraftCategories category,
            OperationTypes operations,
            Maneuver maneuver);
        
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
