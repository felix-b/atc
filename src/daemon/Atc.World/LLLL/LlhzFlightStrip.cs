using Atc.World.Contracts.Communications;
using Atc.World.Contracts.Data;
using Atc.World.Contracts.Traffic;

namespace Atc.World.LLLL;

public record LlhzFlightStrip(
    Callsign Callsign,
    FlightType DepartureType,
    LlhzFlightStripLane Lane,
    DateTime LaneSinceUtc,
    int? RemainingCircuitCount = null
);

public enum LlhzFlightStripLane
{
    Parked,
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
