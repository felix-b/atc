using Atc.Data.Primitives;
using Atc.Sound;
using Atc.Speech.Abstractions;
using Atc.Speech.WinLocalPlugin;
using NUnit.Framework;

namespace Atc.World.Tests.Comms
{
    [TestFixture, Category("e2e")]
    public class EndToEndTests
    {
        [Test]
        public void ControllersTransmittingInCyclesAtLLHZ()
        {
            var setup = new WorldSetup(dependencies => dependencies
                .WithSingleton<ISpeechSynthesisPlugin>(new WindowsSpeechSynthesisPlugin())
                .WithSingleton<IRadioSpeechPlayer>(new RadioSpeechPlayer()));
        
            var llhzClrDel = setup.AddGroundStation(
                Frequency.FromKhz(130850),
                new GeoPoint(32.179766d, 34.834404d),
                "Hertzliya Clearance");  
            var llhzTwrPrimary = setup.AddGroundStation(
                Frequency.FromKhz(122200),
                new GeoPoint(32.179766d, 34.834404d),
                "Hertzliya Tower");  
            var llhzTwrSecondary = setup.AddGroundStation(
                Frequency.FromKhz(129400),
                new GeoPoint(32.179766d, 34.834404d),
                "Hertzliya Tower");
            var plutoPrimary = setup.AddGroundStation(
                Frequency.FromKhz(118400),
                new GeoPoint(32.179766d, 34.834404d),
                "Pluto");
            var plutoSecondary = setup.AddGroundStation(
                Frequency.FromKhz(119150),
                new GeoPoint(32.179766d, 34.834404d),
                "Pluto");

            using var audioContext = new AudioContextScope(setup.DependencyContext.);
            
            setup.Supervisor.CreateActor<DummyControllerActor>(
                id => new DummyControllerActor.DummyActivationEvent(id, llhzClrDel.Station));
            
            setup.Supervisor.CreateActor<DummyControllerActor>(
                id => new DummyControllerActor.DummyActivationEvent(id, llhzTwrPrimary.Station));

            setup.Supervisor.CreateActor<DummyControllerActor>(
                id => new DummyControllerActor.DummyActivationEvent(id, llhzTwrSecondary.Station));

            setup.Supervisor.CreateActor<DummyControllerActor>(
                id => new DummyControllerActor.DummyActivationEvent(id, plutoPrimary.Station));

            setup.Supervisor.CreateActor<DummyControllerActor>(
                id => new DummyControllerActor.DummyActivationEvent(id, plutoSecondary.Station));

        }
    }
}