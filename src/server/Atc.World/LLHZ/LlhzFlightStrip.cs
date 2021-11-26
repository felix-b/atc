using Atc.World.Comms;
using Zero.Loss.Actors;

namespace Atc.World.LLHZ
{
    public record LlhzFlightStrip(
        DepartureIntentType DepartureType,
        ActorRef<AircraftActor> Aircraft,
        LlhzFlightStripLane Lane
    );

    public enum LlhzFlightStripLane
    {
        StartupDeclined = 0,
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
