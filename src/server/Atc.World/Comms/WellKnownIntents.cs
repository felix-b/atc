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

    public readonly struct IntentCondition
    {
        public IntentCondition(ConditionSubjectType? action = null, ConditionTimingType? timing = null)
        {
            Action = action;
            Timing = timing;
        }

        public readonly ConditionSubjectType? Action;
        public readonly ConditionTimingType? Timing;
    }
    
    public record VfrClearance(
        DepartureIntentType DepartureType,
        string? DestinationIcao,
        string? InitialNavaid,
        Bearing? InitialHeading,
        Altitude? InitialAltitude
    );

    public record GreetingIntent(
        IntentHeader Header,
        IntentOptions Options
    ) : Intent(Header, Options)
    {
        public GreetingIntent(IPilotRadioOperatingActor pilot)
            : this(
                pilot.CreateIntentHeader(WellKnownIntentType.Greeting), 
                new IntentOptions(Condition: null, IntentOptionFlags.HasGreeting))
        {
        }
    }

    public record GoAheadIntent(
        IntentHeader Header,
        IntentOptions Options
    ) : Intent(Header, Options);

    public record StartupRequestIntent(
        IntentHeader Header,
        IntentOptions Options,
        DepartureIntentType DepartureType,
        string? DestinationIcao
    ) : Intent(Header, Options)
    {
        public StartupRequestIntent(IPilotRadioOperatingActor pilot, DepartureIntentType departureType, string? destinationIcao)
            : this(
                pilot.CreateIntentHeader(WellKnownIntentType.StartupRequest), 
                IntentOptions.Default,
                departureType,
                destinationIcao)
        {
        }
    }

    public record StartupApprovalIntent(
        IntentHeader Header,
        IntentOptions Options,
        TerminalInformation? Atis,
        VfrClearance? VfrClearance  
    ) : Intent(Header, Options);

    public record StartupApprovalReadbackIntent(
        IntentHeader Header,
        IntentOptions Options,
        StartupApprovalIntent OriginalIntent
    ) : Intent(Header, Options)
    {
        public StartupApprovalReadbackIntent(IPilotRadioOperatingActor pilot, StartupApprovalIntent originalIntent)
            : this(
                pilot.CreateIntentHeader(WellKnownIntentType.StartupApprovalReadback), 
                IntentOptions.Default,
                originalIntent)
        {
        }
    }        
    
    public record MonitorFrequencyIntent(
        IntentHeader Header,
        IntentOptions Options,
        Frequency Frequency,
        ControllerPositionType? ControllerType
    ) : Intent(Header, Options);

    public record MonitorFrequencyReadbackIntent(
        IntentHeader Header,
        IntentOptions Options,
        MonitorFrequencyIntent OriginalIntent
    ) : Intent(Header, Options)
    {
        public MonitorFrequencyReadbackIntent(IPilotRadioOperatingActor pilot, MonitorFrequencyIntent originalIntent)
            : this(
                pilot.CreateIntentHeader(WellKnownIntentType.MonitorFrequencyInstructionReadback), 
                IntentOptions.Default,
                originalIntent)
        {
        }
    }
}
