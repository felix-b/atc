using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using Atc.Data.Primitives;
using Zero.Loss.Actors;

namespace Atc.World.Abstractions
{
    public abstract record Intent(
        IntentHeader Header,
        IntentOptions Options)
    {
        public override string ToString()
        {
            return Header.ToString();
        }

        public string CallsignCalling => Header.OriginatorCallsign;

        public string CallsignReceivingOrThrow()
        {
            return 
                Header.RecipientCallsign 
                ?? throw new InvalidIntentException(this, "RecipieneCallsign is required");
        }
    }

    public record IntentHeader(
        WellKnownIntentType Type,
        int CustomCode,
        //TODO: IntentCategory Category, // for determining priority of handling
        //TODO: int? PriorityOverride, // for determining priority of handling
        string OriginatorUniqueId,
        string OriginatorCallsign,
        GeoPoint OriginatorPosition,
        string? RecipientUniqueId,
        string? RecipientCallsign,
        DateTime CreatedAtUtc)
    {
        public override string ToString()
        {
            return Type != WellKnownIntentType.Custom ? Type.ToString() : CustomCode.ToString();
        }
    }

    public enum WellKnownIntentType
    {
        Unspecified = 0,
        Custom = 0xFFFFFF,
        RadioCheckRequest = 0x010,
        RadioCheckReply = 0x011,
        Greeting = 0x020,
        GoAheadInstruction = 0x021,
        StartupRequest = 0x030,
        StartupApproval = 0x031,
        StartupApprovalReadback = 0x032,
        DepartureTaxiRequest = 0x040,
        DepartureTaxiClearance = 0x041,
        DepartureTaxiClearanceReadback = 0x042,
        HoldShortRunwayReport = 0x050,
        CrossRunwayInstruction = 0x051,
        CrossRunwayInstructionReadback = 0x052,
        HoldShortRunwayInstruction = 0x053,
        HoldShortRunwayInstructionReadback = 0x054,
        ReadyForDepartureReport = 0x060,
        TakeoffClearance = 0x061,
        TakeoffClearanceReadback = 0x062,
        LineUpAndWaitInstruction = 0x063,
        LineUpAndWaitInstructionReadback = 0x064,
        PilotPositionReport = 0x070,
        DirectToNavaidClearance = 0x080,
        DirectToNavaidClearanceReadback = 0x081,
        JoinPatternInstruction = 0x090,
        JoinPatternInstructionReadback = 0x091,
        DownwindPositionReport = 0x092,
        LandingSequenceAssignment = 0x0A0,
        LandingSequenceAssignmentReadback = 0x0A1,
        EnterTrainingZoneInstruction = 0x0B1,
        EnterTrainingZoneInstructionReadback = 0x0B2,
        LeaveTrainingZoneRequest = 0x0C0,
        LeaveTrainingZoneStandByInstruction = 0x0C1,
        LeaveTrainingZoneStandByInstructionReadback = 0x0C2,
        FinalApproachReport = 0x0D0,
        LandingClearance = 0x0D1,
        LandingClearanceReadback = 0x0D2,
        ContinueApproachInstruction = 0x0D3,
        ContinueApproachInstructionReadback = 0x0D4,
        GoAroundInstruction = 0x0D5,
        GoAroundInstructionInstructionReadback = 0x0D6,
        MonitorFrequencyInstruction = 0x0E0,
        MonitorFrequencyInstructionReadback = 0x0E1,
        //TBD.............
    }

    public class WellKnownIntentAttribute : Attribute
    {
        public WellKnownIntentAttribute(WellKnownIntentType intentType)
        {
            IntentType = intentType;
        }

        public WellKnownIntentType IntentType { get; init; }
    }

    public record IntentOptions(
        IntentCondition? Condition,
        IntentOptionFlags Flags = IntentOptionFlags.None)
    {
        public static readonly IntentOptions Default = new IntentOptions(Condition: null, IntentOptionFlags.None);
    }

    public record IntentCondition(
        // Type of subject/action the condition relates to
        ConditionSubjectType SubjectType,
        // Time relation to subject/action (before/after/when)    
        ConditionTimingType Timing,
        // If a subject is represented by an actor (such as aircraft, vehicle, controller)
        ActorRef<IStatefulActor>? Subject = null,
        // SubjectInterval and SubjectTime are mutually exclusive
        // SubjectInterval: a time interval that defines the time relation
        TimeSpan? SubjectInterval = null,
        // SubjectTime: a specific time that defines the time relation
        DateTime? SubjectTime = null
    );

    [Flags]
    public enum IntentOptionFlags
    {
        None = 0x00,
        HasGreeting = 0x01,    // e.g., good morning
        HasFarewell = 0x02,    // e.g., have a good one
        Expedite    = 0x04,    // e.g., without delay or immediate
    }

    public enum ConditionSubjectType
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
        Time
    }

    public enum ConditionTimingType
    {
        Before,
        After,
        When
    }

    public record TerminalInformation(
        string Icao, // Aerodrome ICAO code
        string Designator, // letter A-Z
        Wind Wind,
        Pressure Qnh,
        ImmutableList<string> ActiveRunwaysDeparture, 
        ImmutableList<string> ActiveRunwaysArrival 
        //TODO: add more
        //TODO: add clouds & visibility
        //TODO: add NOTAMs
    ) {
        public string ActiveRunwaysDepartureCommaSeparated => string.Join(", ", ActiveRunwaysDeparture);
        public string ActiveRunwaysArrivalCommaSeparated => string.Join(", ", ActiveRunwaysArrival);
   }


    public class InvalidIntentException : Exception
    {
        public InvalidIntentException(Intent intent, string message)
            : base($"Invalid intent {intent.Header.Type}/{intent.Header.CustomCode} : {message}")
        {
        }
    }
}
