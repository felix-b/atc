using Atc.Grains;

namespace Atc.World;

public static class SiloExtensions
{
    public static readonly string WorldGrainId = ISiloGrains.MakeSingletonGrainId(WorldGrain.TypeString);
    
    public static GrainRef<IWorldGrain> GetWorld(this ISilo silo)
    {
        return silo.Grains.GetRefById<IWorldGrain>(WorldGrainId);
    }
}
