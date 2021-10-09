using System;
using System.Threading;
using Atc.Data.Primitives;
using Atc.Sound;
using Atc.Speech.Abstractions;
using Atc.Speech.WinLocalPlugin;
using Atc.World.Abstractions;
using Atc.World.Comms;
using NUnit.Framework;
using Zero.Doubt.Logging;

namespace Atc.World.Tests.Comms
{
    [TestFixture, Category("e2e")]
    public class EndToEndTests
    {
        [Test]
        public void ControllersTransmittingInCyclesAtLLHZ()
        {
            LogEngine.SetTargetToConsole();
            var setup = new WorldSetup(dependencies => dependencies
                .WithSingleton<LogWriter>(LogEngine.Writer)
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
            
            llhzClrDel.Station.Get().PowerOn();
            llhzTwrPrimary.Station.Get().PowerOn();
            llhzTwrSecondary.Station.Get().PowerOn();
            plutoPrimary.Station.Get().PowerOn();
            plutoSecondary.Station.Get().PowerOn();

            using var audioContext = new AudioContextScope(setup.DependencyContext.Resolve<ISoundSystemLogger>());
            
            var llhzClrDelController = setup.Supervisor.CreateActor<DummyControllerActor>(
                id => new DummyControllerActor.DummyActivationEvent(id, llhzClrDel.Station));
            
            var llhzTwrController = setup.Supervisor.CreateActor<DummyControllerActor>(
                id => new DummyControllerActor.DummyActivationEvent(id, llhzTwrPrimary.Station));

            // setup.Supervisor.CreateActor<DummyControllerActor>(
            //     id => new DummyControllerActor.DummyActivationEvent(id, llhzTwrSecondary.Station));

            var plutoController = setup.Supervisor.CreateActor<DummyControllerActor>(
                id => new DummyControllerActor.DummyActivationEvent(id, plutoPrimary.Station));

            // setup.Supervisor.CreateActor<DummyControllerActor>(
            //     id => new DummyControllerActor.DummyActivationEvent(id, plutoSecondary.Station));

            var airStation1 = setup.AddAirStation(
                Frequency.FromKhz(130850), new GeoPoint(32.18d, 34.83d), Altitude.FromFeetMsl(1000), callsign: "air1");
            
            airStation1.Get().PowerOn();
            
            using var monitor = new RadioStationSoundMonitor(
                setup.Supervisor,
                setup.DependencyContext.Resolve<IVerbalizationService>(),
                setup.DependencyContext.Resolve<ISpeechSynthesisPlugin>(),
                setup.DependencyContext.Resolve<IRadioSpeechPlayer>(),
                setup.DependencyContext.Resolve<ICommsLogger>(),
                airStation1.Get());

            setup.World.Get().ProgressBy(TimeSpan.FromMilliseconds(1));
            
            while (setup.World.Get().Timestamp < TimeSpan.FromMinutes(1))
            {
                var t0 = DateTime.UtcNow;
                Thread.Sleep(1000);
                var t1 = DateTime.UtcNow;
                setup.World.Get().ProgressBy(t1 - t0);
            }
        }
    }
}