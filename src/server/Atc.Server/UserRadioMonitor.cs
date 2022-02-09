using System;
using Atc.Data.Primitives;
using Atc.World;
using Atc.World.Abstractions;
using Atc.World.Comms;
using Zero.Loss.Actors;

namespace Atc.Server
{
    public class UserRadioMonitor : IDisposable
    {
        private readonly ISupervisorActor _supervisor;
        private readonly ActorRef<RadioStationActor> _radio;
        private readonly RadioStationSoundMonitor _monitor;
        private readonly IWorldContext _world;

        public UserRadioMonitor(
            IWorldContext world,
            ISupervisorActor supervisor,
            IVerbalizationService verbalization,
            ISpeechSynthesisPlugin synthesizer, 
            IRadioSpeechPlayer player, 
            ICommsLogger logger)
        {
            _world = world;
            _supervisor = supervisor;
            _radio = supervisor.CreateActor<RadioStationActor>(id => new RadioStationActor.ActivationEvent(
                id, 
                new GeoPoint(32.179766d, 34.834404d),
                Altitude.FromFeetMsl(1000),
                Frequency.FromKhz(121500), 
                Name: "user1",
                Callsign: "user1"));

            _monitor = new RadioStationSoundMonitor(
                supervisor,
                verbalization,
                synthesizer,
                player,
                logger,
                _radio.Get());

            _world.Defer("user-radio-power-on", () => _radio.Get().PowerOn());
        }

        public void Dispose()
        {
            _monitor.Dispose();
            _supervisor.DeleteActor(_radio);
        }

        public void TuneTo(Frequency frequency)
        {
            _radio.Get().TuneTo(frequency);
        }
    }
}