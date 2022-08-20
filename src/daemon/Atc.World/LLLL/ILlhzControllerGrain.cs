using Atc.Grains;
using Atc.Maths;
using Atc.World.Contracts.Communications;
using Atc.World.Contracts.Data;

namespace Atc.World.LLLL;

public interface ILlhzControllerGrain : IGrainId
{
    void AcceptHandoff(LlhzHandoffIntent handoff);
    Frequency Frequency { get; }
    Callsign Callsign { get; }
    ControllerPositionType PositionType { get; }
}

public record LlhzHandoffIntent(
    IntentHeader Header,
    ControllerPositionType CallerControllerPosition,
    LlhzFlightStrip FlightStrip
) : Intent(Header);
