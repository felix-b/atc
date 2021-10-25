using Atc.World.Abstractions;
using Zero.Loss.Actors;

namespace Atc.World.Comms
{
    public interface IRadioOperatingActor : IStatefulActor, IHaveParty
    {
        void BeginQueuedTransmission(int cookie);
    }

    public interface IPilotRadioOperatingActor : IRadioOperatingActor
    {
        IntentHeader CreateIntentHeader(WellKnownIntentType type, int customCode = 0);
    }
}
