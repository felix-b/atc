using Atc.World.Comms;
using Zero.Loss.Actors;

namespace Atc.World.LLHZ
{
    public record LlhzFlightStrip(
        DepartureIntentType DepartureType,
        ActorRef<AircraftActor> Aircraft,
        LlhzFlightStripLane Lane)
    {
        public string Callsign => Aircraft.Get().Callsign;
    }

    public enum LlhzFlightStripLane
    {
        Unspecified = 0,
        Parked = 1,
        StartupDeclined,
        StartupApproved,
        TaxiDeclined,
        TaxiApproved,
        HoldingShortReadyForDeparture,
        ApprovedLineUpAndWait,
        ClearedForTakeoff,
        ReportedDownwind,
        ReportedFinal,
        ClearedToLand,
        GoingAround,
    }
}
