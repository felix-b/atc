using Atc.Grains;
using Atc.Maths;
using Atc.World.Contracts.Airports;
using Atc.World.Control;
using Atc.World.LLLL;

namespace Atc.World.Airports;

public interface IAirportGrain : IGrainId
{
    string Icao { get; }
    GeoPoint Datum { get; }
    TerminalInformation CurrentAtis { get; }
    GrainRef<IControllerGrain> Tower { get; } 
    GrainRef<IControllerGrain> Clearance { get; } 
}
