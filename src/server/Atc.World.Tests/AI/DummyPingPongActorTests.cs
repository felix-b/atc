using System;
using System.Threading;
using Atc.Data.Primitives;
using NUnit.Framework;
using Zero.Doubt.Logging;

namespace Atc.World.Tests.AI
{
    [TestFixture]
    public class DummyPingPongActorTests
    {
        [Test]
        public void CanRunSinglePingPongPair()
        {            
            var setup = new WorldSetup();

            var groundStation = setup.AddGroundStation(
                Frequency.FromKhz(118000),
                new GeoPoint(32, 34),
                "Ground");  
            
            var pingAirStation = setup.AddAirStation(
                Frequency.FromKhz(118000), 
                new GeoPoint(32, 34), 
                Altitude.FromFeetMsl(1000), callsign: "ping1");  

            var pongAirStation = setup.AddAirStation(
                Frequency.FromKhz(118000), 
                new GeoPoint(32, 34), 
                Altitude.FromFeetMsl(1000), callsign: "pong1");

            var pingActor = setup.Supervisor.CreateActor<DummyPingPongActor>(uniqueId =>
                new DummyPingPongActor.DummyActivationEvent(
                    uniqueId,
                    pingAirStation,
                    DummyPingPongActor.PingPongRole.Ping)
            ).Get();

            var pongActor = setup.Supervisor.CreateActor<DummyPingPongActor>(uniqueId =>
                new DummyPingPongActor.DummyActivationEvent(
                    uniqueId,
                    pongAirStation,
                    DummyPingPongActor.PingPongRole.Pong)
            ).Get();
            
            for (int second = 1 ; second <= 60 ; second++)
            {
                setup.World.Get().ProgressBy(TimeSpan.FromSeconds(second));
            }

            var pingLog = pingActor.IntentLog;
            var pongLog = pongActor.IntentLog;
        }
    }
}
