using System;
using System.Linq;
using System.Threading;
using Atc.Data.Primitives;
using FluentAssertions;
using NUnit.Framework;

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
                    DummyPingPongActor.PingPongRole.Ping));

            var pongActor = setup.Supervisor.CreateActor<DummyPingPongActor>(uniqueId =>
                new DummyPingPongActor.DummyActivationEvent(
                    uniqueId,
                    pongAirStation,
                    DummyPingPongActor.PingPongRole.Pong));
            
            pingActor.Get().SetCounterparty(pongActor);
            pongActor.Get().SetCounterparty(pingActor);
            
            for (int second = 1 ; second <= 60 ; second++)
            {
                setup.World.Get().ProgressBy(TimeSpan.FromSeconds(1));
            }

            var actualLog = pingActor.Get().IntentLog.Concat(pongActor.Get().IntentLog).OrderBy(s => s).ToArray();
            var expectedLog = new[] {
                "10:30:11:ping1->pong1:PING#1", //1sec start + 5sec delay + 5sec utterance
                "10:30:19:pong1->ping1:PONG#1", //3sec silence + 5sec utterance
                "10:30:29:ping1->pong1:PING#2",
                "10:30:37:pong1->ping1:PONG#2",
                "10:30:47:ping1->pong1:PING#3",
                "10:30:55:pong1->ping1:PONG#3"
            };

            actualLog.Should().BeStrictlyEquivalentTo(expectedLog);
        }
    }
}
