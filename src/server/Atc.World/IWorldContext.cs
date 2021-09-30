using System;
using Atc.Data.Primitives;
using Atc.World.Comms;

namespace Atc.World
{
    public interface IWorldContext
    {
        ulong CreateUniqueId(ulong lastId);
        GroundRadioStationAether? TryFindRadioAether(RuntimeRadioStation fromStation);
        IDeferHandle DeferBy(TimeSpan time, Action action);
        IDeferHandle DeferUntil(DateTime utc, Action action);
        IDeferHandle DeferUntil(Func<bool> predicate, DateTime utc, Action onPredicateTrue, Action onTimeout);
        DateTime UtcNow();
    }

    public interface IDeferHandle
    {
        void Cancel();
    }
}
