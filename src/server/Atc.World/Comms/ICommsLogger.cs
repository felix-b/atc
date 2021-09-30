using Atc.World.Redux;

namespace Atc.World.Comms
{
    public interface ICommsLogger
    {
        void StationPoweringOn(string station);
        void StationTuningTo(string station, int khz);
        void AetherStationAdded(string aether, string station);
        void AetherStationRemoved(string aether, string station);
        void RegisteredPendingTransmission(ulong tokenId, string speaker, int cookie);
    }
}
