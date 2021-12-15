using System;
using Atc.Data.Primitives;
using Atc.Sound;
using Atc.Speech.AzurePlugin;
using Atc.World.Abstractions;
using Atc.World.Comms;
using Atc.World.LLHZ;
using Atc.World.Testability;
using NUnit.Framework;

namespace Atc.World.Tests.LLHZ
{
    [TestFixture, Category("e2e")]
    public class LlhzEndToEndTests
    {
        private LlhzBufferContext _bufferContext = null;
        
        [OneTimeSetUp]
        public void BeforeAll()
        {
            _bufferContext = new();
        }

        [OneTimeTearDown]
        public void AfterAll()
        {
            _bufferContext.Dispose();
        }

        [Test]
        public void StartupForPatternFlight()
        {
            var setup = new WorldSetup();
            
            using var audioContext = new AudioContextScope(setup.ResolveDependency<ISoundSystemLogger>());
            
            setup.DependencyContextBuilder
                .WithSingleton<ISpeechSynthesisPlugin>(new AzureSpeechSynthesisPlugin())
                .WithSingleton<IRadioSpeechPlayer>(new RadioSpeechPlayer(setup.Environment))
                .WithSingleton<IVerbalizationService>(new LlhzVerbalizationService(setup.Environment));
            
            var llhz = setup.Supervisor.CreateActor<LlhzAirportActor>(
                uniqueId => new LlhzAirportActor.LlhzAirportActivationEvent(uniqueId)
            );

            var airStation1 = setup.AddAirStation(
                Frequency.FromKhz(130850), 
                new GeoPoint(32.18d, 34.83d), 
                Altitude.FromFeetMsl(1000), callsign: "air1");
            airStation1.Get().PowerOn();
 
            using var monitor = new RadioStationSoundMonitor(
                setup.Supervisor,
                setup.ResolveDependency<IVerbalizationService>(),
                setup.ResolveDependency<ISpeechSynthesisPlugin>(),
                setup.ResolveDependency<IRadioSpeechPlayer>(),
                setup.ResolveDependency<ICommsLogger>(),
                airStation1.Get());

            setup.RunWorldFastForward(TimeSpan.FromSeconds(1), 57);
            setup.RunWorldRealTime(TimeSpan.FromSeconds(1), 57);
        }
    }
}