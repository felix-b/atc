using Atc.Grains;
using Atc.Maths;
using Atc.World.Traffic;

namespace Atc.World.LLLL;

public static class LlllTrafficFactory
{
    public static GrainRef<IAircraftGrain> SpawnParkedAircraft(
        ISilo silo,
        string tailNo,
        GeoPoint position,
        Bearing heading)
    {
        return silo.Grains.CreateGrain<AircraftGrain>(grainId =>
            new AircraftGrain.GrainActivationEvent(
                grainId,
                tailNo,
                position,
                heading)
        ).As<IAircraftGrain>();
    }
}
