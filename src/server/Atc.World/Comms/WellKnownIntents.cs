using System;
using System.Collections.Immutable;
using Atc.Data.Control;
using Atc.Data.Primitives;
using Atc.World.Abstractions;

namespace Atc.World.Comms
{
    public enum DepartureIntentType
    {
        Unspecified = 0,
        ToDestination = 0x01,
        ToStayInPattern = 0x02,
        ToTrainingZones = 0x03,
    }

    // public readonly struct IntentCondition
    // {
    //     public IntentCondition(ConditionSubjectType? action = null, ConditionTimingType? timing = null)
    //     {
    //         Action = action;
    //         Timing = timing;
    //     }
    //
    //     public readonly ConditionSubjectType? Action;
    //     public readonly ConditionTimingType? Timing;
    // }

    public record VfrClearance(
        DepartureIntentType DepartureType,
        string? DestinationIcao,
        string? InitialNavaid,
        Bearing? InitialHeading,
        Altitude? InitialAltitude
    );

    public record DepartureTaxiClearance(
        string ActiveRunway,
        string? HoldingPoint,
        ImmutableList<string> TaxiPath,
        ImmutableList<string> HoldShortRunways
    );

    [WellKnownIntent(WellKnownIntentType.Greeting)]
    public record GreetingIntent(
        IntentHeader Header,
        IntentOptions Options
    ) : Intent(Header, Options)
    {
        public GreetingIntent(IPilotRadioOperatingActor pilot)
            : this(
                pilot.CreatePilotToAtcIntentHeader(WellKnownIntentType.Greeting), 
                new IntentOptions(Condition: null, IntentOptionFlags.HasGreeting))
        {
        }
    }

    [WellKnownIntent(WellKnownIntentType.GoAheadInstruction)]
    public record GoAheadIntent(
        IntentHeader Header,
        IntentOptions Options
    ) : Intent(Header, Options);

    [WellKnownIntent(WellKnownIntentType.StartupRequest)]
    public record StartupRequestIntent(
        IntentHeader Header,
        IntentOptions Options,
        DepartureIntentType DepartureType,
        string? DestinationIcao
    ) : Intent(Header, Options)
    {
        public StartupRequestIntent(IPilotRadioOperatingActor pilot, DepartureIntentType departureType, string? destinationIcao)
            : this(
                pilot.CreatePilotToAtcIntentHeader(WellKnownIntentType.StartupRequest), 
                IntentOptions.Default,
                departureType,
                destinationIcao)
        {
        }
    }

    [WellKnownIntent(WellKnownIntentType.StartupApproval)]
    public record StartupApprovalIntent(
        IntentHeader Header,
        IntentOptions Options,
        TerminalInformation? Atis,
        VfrClearance? VfrClearance  
    ) : Intent(Header, Options);

    [WellKnownIntent(WellKnownIntentType.StartupApprovalReadback)]
    public record StartupApprovalReadbackIntent(
        IntentHeader Header,
        IntentOptions Options,
        StartupApprovalIntent OriginalIntent
    ) : Intent(Header, Options)
    {
        public StartupApprovalReadbackIntent(IPilotRadioOperatingActor pilot, StartupApprovalIntent originalIntent)
            : this(
                pilot.CreatePilotToAtcIntentHeader(WellKnownIntentType.StartupApprovalReadback), 
                IntentOptions.Default,
                originalIntent)
        {
        }
    }        
    
    [WellKnownIntent(WellKnownIntentType.MonitorFrequencyInstruction)]
    public record MonitorFrequencyIntent(
        IntentHeader Header,
        IntentOptions Options,
        Frequency Frequency,
        ControllerPositionType? ControllerType,
        string? ControllerCallsign
    ) : Intent(Header, Options);

    [WellKnownIntent(WellKnownIntentType.MonitorFrequencyInstructionReadback)]
    public record MonitorFrequencyReadbackIntent(
        IntentHeader Header,
        IntentOptions Options,
        MonitorFrequencyIntent OriginalIntent
    ) : Intent(Header, Options)
    {
        public MonitorFrequencyReadbackIntent(IPilotRadioOperatingActor pilot, MonitorFrequencyIntent originalIntent)
            : this(
                pilot.CreatePilotToAtcIntentHeader(WellKnownIntentType.MonitorFrequencyInstructionReadback), 
                IntentOptions.Default,
                originalIntent)
        {
        }
    }

    [WellKnownIntent(WellKnownIntentType.DepartureTaxiRequest)]
    public record DepartureTaxiRequestIntent(
        IntentHeader Header,
        IntentOptions Options,
        string? ParkingStandName = null,
        string? AtisDesignator = null,
        Pressure? Qnh = null
    ) : Intent(Header, Options);
    
    [WellKnownIntent(WellKnownIntentType.DepartureTaxiClearance)]
    public record DepartureTaxiClearanceIntent(
        IntentHeader Header,
        IntentOptions Options,
        DepartureTaxiRequestIntent OriginalRequest,
        bool Cleared,
        DepartureTaxiClearance? Clearance
    )  : Intent(Header, Options);

    [WellKnownIntent(WellKnownIntentType.DepartureTaxiClearanceReadback)]
    public record DepartureTaxiClearanceReadbackIntent(
        IntentHeader Header,
        IntentOptions Options,
        DepartureTaxiClearanceIntent OriginalIntent
    )  : Intent(Header, Options);
    
    [WellKnownIntent(WellKnownIntentType.ReadyForDepartureReport)]
    public record ReportReadyForDepartureIntent(
        IntentHeader Header,
        IntentOptions Options,
        string? HoldingPoint = null,
        string? ActiveRunway = null,
        bool ReadyForRollingTakeoff = false
    )  : Intent(Header, Options);
    
    [WellKnownIntent(WellKnownIntentType.TakeoffClearance)]
    public record TakeoffClearanceIntent(
        IntentHeader Header,
        IntentOptions Options,
        string Runway,
        Wind Wind,
        Altitude? InitialAltitude = null,
        Bearing? InitialHeading = null,
        string? InitialNavaid = null,
        bool RollingTakeoff = false
    )  : Intent(Header, Options);

    [WellKnownIntent(WellKnownIntentType.TakeoffClearanceReadback)]
    public record TakeoffClearanceReadbackIntent(
        IntentHeader Header,
        IntentOptions Options,
        TakeoffClearanceIntent OriginalIntent
    )  : Intent(Header, Options);
    
    [WellKnownIntent(WellKnownIntentType.LineUpAndWaitInstruction)]
    public record LineUpAndWaitIntent(
        IntentHeader Header,
        IntentOptions Options,
        string Runway
    )  : Intent(Header, Options);
    
    [WellKnownIntent(WellKnownIntentType.LineUpAndWaitInstructionReadback)]
    public record LineUpAndWaitReadbackIntent(
        IntentHeader Header,
        IntentOptions Options,
        LineUpAndWaitIntent OriginalIntent
    )  : Intent(Header, Options);

    [WellKnownIntent(WellKnownIntentType.HoldShortRunwayInstruction)]
    public record HoldShortRunwayIntent(
        IntentHeader Header,
        IntentOptions Options,
        string Runway
    )  : Intent(Header, Options);

    [WellKnownIntent(WellKnownIntentType.HoldShortRunwayInstructionReadback)]
    public record HoldShortRunwayReadbackIntent(
        IntentHeader Header,
        IntentOptions Options,
        HoldShortRunwayIntent OriginalIntent
    )  : Intent(Header, Options);

    [WellKnownIntent(WellKnownIntentType.DownwindPositionReport)]
    public record ReportDownwindIntent(
        IntentHeader Header,
        IntentOptions Options
    )  : Intent(Header, Options);

    [WellKnownIntent(WellKnownIntentType.LandingSequenceAssignment)]
    public record LandingSequenceAssignmentIntent(
        IntentHeader Header,
        IntentOptions Options,
        string Runway,
        int LandingSequenceNumber
    )  : Intent(Header, Options);

    [WellKnownIntent(WellKnownIntentType.LandingSequenceAssignmentReadback)]
    public record LandingSequenceAssignmentReadbackIntent(
        IntentHeader Header,
        IntentOptions Options,
        LandingSequenceAssignmentIntent OriginalIntent
    )  : Intent(Header, Options);

    [WellKnownIntent(WellKnownIntentType.FinalApproachReport)]
    public record FinalApproachReportIntent(
        IntentHeader Header,
        IntentOptions Options,
        string Runway
    )  : Intent(Header, Options);

    [WellKnownIntent(WellKnownIntentType.LandingClearance)]
    public record LandingClearanceIntent(
        IntentHeader Header,
        IntentOptions Options,
        string Runway,
        Wind Wind,
        LandingType LandingType
    )  : Intent(Header, Options);

    public enum LandingType
    {
        TouchAndGo,
        FullStop
    }

    [WellKnownIntent(WellKnownIntentType.LandingClearanceReadback)]
    public record LandingClearanceReadbackIntent(
        IntentHeader Header,
        IntentOptions Options,
        LandingClearanceIntent OriginalIntent
    )  : Intent(Header, Options);

    [WellKnownIntent(WellKnownIntentType.ContinueApproachInstruction)]
    public record ContinueApproachIntent(
        IntentHeader Header,
        IntentOptions Options,
        string Runway
    )  : Intent(Header, Options);

    [WellKnownIntent(WellKnownIntentType.ContinueApproachInstructionReadback)]
    public record ContinueApproachReadbackIntent(
        IntentHeader Header,
        IntentOptions Options,
        ContinueApproachIntent OriginalIntent
    )  : Intent(Header, Options);

    [WellKnownIntent(WellKnownIntentType.GoAroundInstruction)]
    public record GoAroundInstructionIntent(
        IntentHeader Header,
        IntentOptions Options,
        GoAroundReason? Reason
    )  : Intent(Header, Options);

    public enum GoAroundReason
    {
        RunwayOccupied = 1,
        ApproachUnstable = 2
    }

    [WellKnownIntent(WellKnownIntentType.GoAroundInstructionInstructionReadback)]
    public record GoAroundInstructionReadbackIntent(
        IntentHeader Header,
        IntentOptions Options,
        GoAroundInstructionIntent OriginalIntent
    )  : Intent(Header, Options);
}
