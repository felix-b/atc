using System;
using Atc.World.Comms;
using Zero.Loss.Actors;

namespace Atc.World.LLHZ
{
    public record LlhzFlightStrip(
        DepartureIntentType DepartureType,
        ActorRef<Traffic.AircraftActor> Aircraft,
        LlhzFlightStripLane Lane,
        DateTime LaneSinceUtc,
        int? RemainingCircuitCount = null,
        ActorRef<LlhzControllerActor>? HandedOffBy = null)
    {
        public string Callsign => Aircraft.Get().Callsign;
    }

    public enum LlhzFlightStripLane
    {
        Unspecified = 0,
        Parked = 1,
        StartupDeclined,
        StartupApproved,
        DepartureTaxiDeclined,
        DepartureTaxiApproved,
        HoldingShortReadyForDeparture,
        ApprovedLineUpAndWait,
        ClearedForTakeoff,
        ReportedDownwind,
        ReportedFinal,
        ClearedForTouchAndGo,
        ClearedToLand,
        GoingAround,
    }
}
