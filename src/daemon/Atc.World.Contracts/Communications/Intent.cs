namespace Atc.World.Contracts.Communications;

public abstract record Intent(
    IntentHeader Header
) {
    public bool ConcludesConversation => (Header.Flags & IntentFlags.ConcludesConversation) != 0;
}

public record IntentHeader(
    ulong Id,
    WellKnownIntentType WellKnownType,
    AirGroundPriority Priority,
    Callsign Caller,
    Callsign? Callee, // null if broadcast to all stations 
    IntentFlags Flags,
    ToneTraits Tone
);

[Flags]
public enum IntentFlags
{
    None = 0x0,
    HasGreeting = 0x01,
    HasFarewell = 0x02,
    ConcludesConversation = 0x04
}
