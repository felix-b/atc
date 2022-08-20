using Atc.Maths;
using Atc.World.Contracts.Data;
using Atc.World.Contracts.Traffic;

namespace Atc.World.Contracts.Communications;

public enum DepartureRejectReason
{
    NoFlightPlan,
    PatternBusy,
    TrainingAreasBusy,
    //TODO: add more
}

public enum LandingClearanceType
{
    FullStop,
    TouchAndGo
}

public enum GoAroundReason
{
    RunwayOccupied,
    ApproachUnstable
}

public record IntentCondition(
    // Time relation to subject/action (before/after/when)    
    IntentConditionWhen When,
    // Type of subject/action the condition relates to
    IntentConditionWhat What,
    // Callsign of operator specified in What
    Callsign? Who = null,
    // TimeInterval and PointInTime are mutually exclusive
    // What == TimeInterval: a time interval that defines the time relation
    TimeSpan? TimeInterval = null,
    // What == PointInTime: a specific time that defines the time relation
    DateTime? PointInTime = null
);

public enum IntentConditionWhen
{
    Before,
    After,
    AtMoment,
}

public enum IntentConditionWhat
{
    // before/after startup
    Startup,
    // when ready to taxi 
    ReadyToTaxi,
    // before/after takeoff
    Takeoff,
    // before/after landing 
    Land,
    // before/after vacating runway
    Vacate,
    // before/after parking
    Park,
    // before/after another aircraft
    Aircraft,
    // before/after a vehicle
    Vehicle,
    // such as "X minutes ago" (before) or "in X minutes" (after)
    TimeInterval,
    // such as "no later than X" (before) or "not earlier than X" (after)
    PointInTime
}

public record TrafficAdvisory(
    TrafficAdvisoryLocation Location,
    TrafficAdvisoryDestinationType? DestinationType = null,
    string? DestinationWaypointName = null,
    string? AircraftType = null,
    Bearing? Heading = null,
    Speed? Speed = null,
    Altitude? Altitude = null,
    bool RequestTrafficInSightReport = false
);

public record TrafficAdvisoryLocation(
    Bearing? RelativeHeading = null,
    TrafficAdvisoryLocationOrdering? RelativeOrdering = null,
    TrafficAdvisoryLocationPattern? Pattern = null,
    TrafficAdvisoryLocationRefinement? Refinement = null
);
    
public enum TrafficAdvisoryLocationOrdering
{
    InFront,   
    Behind    
}

public enum TrafficAdvisoryLocationPattern
{
    Upwind,
    Departure,
    Crosswind,
    Downwind,
    Base,
    Final,
}

public enum TrafficAdvisoryLocationRefinement
{
    None,
    BeginningOf,
    MiddleOf,
    EndOf
}

public enum TrafficAdvisoryDestinationType
{
    InPattern,
    ToWaypoint,
}

public record AcknowledgeIntent( //"Roger"
    IntentHeader Header
) : Intent(
    Header
);

public record GoAheadIntent(
    IntentHeader Header
) : Intent(
    Header
);

public record StandbyInstructionIntent(
    IntentHeader Header,
    TimeSpan? ExpectedTimeToWait = null
) : Intent(
    Header
);

public record HoldPositionInstructionIntent(
    IntentHeader Header,
    string? HoldingPositionName = null,
    IntentCondition? Condition = null
) : Intent(
    Header
);

public record PositionHoldInstructionReadbackIntent(
    IntentHeader Header,
    IntentCondition? Condition = null
) : Intent(
    Header
);

public record ParkedCheckInIntent(
    IntentHeader Header,
    string? ParkApronName = null,
    string? ParkStandName = null,
    string? AtisDesignator = null
) : Intent(
    Header
);

public record ShutdownCheckOutIntent(
    IntentHeader Header
) : Intent(
    Header
);

public record StartupClearance(
    FlightType FlightType,
    string? DestinationIcao,
    string? DepartureRunway,
    Pressure? Qnh,
    string? AtisDesignator,
    string? InitialWaypoint,
    Altitude? InitialAltitude,
    IntentCondition? Condition = null
);

public record RequestStartupIntent(
    IntentHeader Header,
    FlightType FlightType,
    string? ParkApronName,
    string? ParkStandName,
    string? AtisDesignator
) : Intent(
    Header
);

public record ApproveStartupIntent(
    IntentHeader Header,
    StartupClearance Clearance
) : Intent(
    Header
);

public record StartupApprovalReadbackIntent(
    IntentHeader Header,
    StartupClearance Clearance
) : Intent(
    Header
);

public record RejectStartupRequestIntent(
    IntentHeader Header,
    DepartureRejectReason Reason
) : Intent(
    Header
);

public record StartupRejectionReadbackIntent(
    IntentHeader Header,
    DepartureRejectReason Reason
) : Intent(
    Header
);

public record MonitorFrequencyInstruction(
    Frequency StationFrequency,
    string? StationName = null,
    Callsign? StationCallsign = null
);

public record MonitorFrequencyInstructionIntent(
    IntentHeader Header,
    MonitorFrequencyInstruction Instruction
) : Intent(
    Header
);

public record FrequencyMonitorInstructionReadbackIntent(
    IntentHeader Header,
    MonitorFrequencyInstruction Instruction
) : Intent(
    Header
);

public record TaxiClearance(
    string RunwayName,
    string? HoldingPositionName,
    //TODO: add taxi path/intructions
    IntentCondition? Condition
);

public record RequestTaxiIntent(
    IntentHeader Header,
    string? ParkApronName = null,
    string? ParkStandName = null,
    string? AtisDesignator = null
) : Intent(
    Header
);

public record ApproveTaxiIntent(
    IntentHeader Header,
    TaxiClearance Clearance
) : Intent(
    Header
);

public record TaxiApprovalReadbackIntent(
    IntentHeader Header,
    TaxiClearance Clearance
) : Intent(
    Header
);

public record ReportReadyToDepartureIntent(
    IntentHeader Header,
    string? RunwayName,
    string? HoldingPositionName,
    //TODO: add taxi path/intructions
    IntentCondition? Condition
) : Intent(
    Header
);

public record LineUpClearance(
    string RunwayName
);

public record LineupAndWaitInstructionIntent(
    IntentHeader Header,
    LineUpClearance Clearance
) : Intent(
    Header
);

public record LineupAndWaitInstructionReadbackIntent(
    IntentHeader Header,
    LineUpClearance Clearance
) : Intent(
    Header
);

public record TakeoffClearance(
    string RunwayName,
    Wind Wind,
    TrafficAdvisory? Traffic = null,
    Bearing? InitialHeading = null,
    Altitude? InitialAltitude = null,
    string? InitialWaypoint = null,
    MonitorFrequencyInstruction? AfterTakeoffFrequency = null
);

public record ClearToTakeoffIntent(
    IntentHeader Header,
    TakeoffClearance Clearance
) : Intent(
    Header
);

public record TakeoffClearanceReadbackIntent(
    IntentHeader Header,
    TakeoffClearance Clearance
) : Intent(
    Header
);

public record ReportDownwindIntent(
    IntentHeader Header
) : Intent(
    Header
);

public record PatternSequenceAssignmentInstructionIntent(
    IntentHeader Header,
    int SequenceNumber,
    TrafficAdvisory? Traffic
) : Intent(
    Header
);

public record PatternSequenceAssignmentInstructionReadbackIntent(
    IntentHeader Header,
    int SequenceNumber,
    bool? TrafficInSight
) : Intent(
    Header
);

public record ReportFinalIntent(
    IntentHeader Header,
    string RunwayName
) : Intent(
    Header
);

public record LandingClearance(
    string RunwayName,
    Wind Wind,
    LandingClearanceType LandingType
);

public record ClearToLandIntent(
    IntentHeader Header,
    LandingClearance Clearance
) : Intent(
    Header
);

public record LandingClearanceReadbackIntent(
    IntentHeader Header,
    LandingClearance Clearance
) : Intent(
    Header
);

public record ContinueApproachInstructionIntent(
    IntentHeader Header,
    string? RunwayName,
    Wind? Wind,
    bool? ExpectLateClearance
) : Intent(
    Header
);

public record ContinueApproachInstructionReadbackIntent(
    IntentHeader Header,
    string? RunwayName
) : Intent(
    Header
);

public record MissedApproachInstruction(
    Altitude? Altitude = null,
    Bearing? Heading = null
);

public record GoAroundInstructionIntent(
    IntentHeader Header,
    string? RunwayName = null,
    GoAroundReason? Reason = null
) : Intent(
    Header
);

public record GoAroundInstructionReadbackIntent(
    IntentHeader Header,
    string? RunwayName = null
) : Intent(
    Header
);

public record ReportGoingAroundIntent(
    IntentHeader Header,
    GoAroundReason? Reason = null
) : Intent(
    Header
);

public record MissedApproachInstructionIntent(
    IntentHeader Header,
    GoAroundReason? Reason = null
) : Intent(
    Header
);
