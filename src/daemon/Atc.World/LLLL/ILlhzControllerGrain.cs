using Atc.Grains;
using Atc.Maths;
using Atc.World.Contracts.Communications;
using Atc.World.Contracts.Data;
using Atc.World.Control;

namespace Atc.World.LLLL;

public interface ILlhzControllerGrain : IControllerGrain, IGrainId
{
    void AcceptHandoff(LlhzHandoffIntent handoff); //TODO: lift to IControllerGrain 
}

public record LlhzHandoffIntent(
    IntentHeader Header,
    ControllerPositionType CallerControllerPosition,
    LlhzFlightStrip FlightStrip
) : Intent(Header);
