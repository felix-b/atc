using Atc.Grains;
using Atc.Maths;
using Atc.World.Contracts.Communications;
using Atc.World.Contracts.Data;

namespace Atc.World.Control;

public interface IControllerGrain : IGrainId
{
    Frequency Frequency { get; }
    Callsign Callsign { get; }
    ControllerPositionType PositionType { get; }
}
