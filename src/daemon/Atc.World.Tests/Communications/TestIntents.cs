using Atc.World.Contracts.Communications;

namespace Atc.World.Tests.Communications;

public record TestIntentA(
    ulong Id,
    IntentFlags Flags = IntentFlags.None
) : Intent(
    new IntentHeader(
        Id, 
        WellKnownType: WellKnownIntentType.None,
        Priority: AirGroundPriority.FlightSafetyNormal,
        Caller: new Callsign("A", "A"),
        Callee: new Callsign("B", "B"),
        Flags: Flags,
        Tone: ToneTraits.Neutral
    )
);
