using System;

namespace Atc.World
{
    public interface IWorldContext
    {
        ulong CreateUniqueId(ulong lastId);
        DateTime UtcNow();
    }
}
