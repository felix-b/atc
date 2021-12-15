using Atc.Data.Primitives;
using Atc.World.Abstractions;
using Zero.Loss.Actors;

namespace Atc.World.Comms
{
    public interface IRadioOperatingActor : IStatefulActor, IHaveParty
    {
        void MonitorFrequency(Frequency frequency);
        void InitiateTransmission(Intent intent);
        void BeginQueuedTransmission(int cookie);
    }

    public interface IPilotRadioOperatingActor : IRadioOperatingActor
    {
        IntentHeader CreateIntentHeader(WellKnownIntentType type, int customCode = 0);
        GeoPoint GetCurrentPosition();
    }
}
