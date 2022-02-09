using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Atc.Data.Primitives;
using Atc.World.Testability;
using Atc.World.Testability.AI;
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
            var radioLog = new List<string>();
            var setup = new WorldSetup();

            var groundStation = setup.AddGroundStation(
                Frequency.FromKhz(118000),
                new GeoPoint(32, 34),
                "Ground");  
            groundStation.Station.Get().AddListener((station, status, intent) => {
                radioLog.Add($"{setup.WorldContext.UtcNow().TimeOfDay}:{status}:{intent?.GetType()?.Name ?? "no-intent"}");
            }, out _);
            groundStation.Station.Get().PowerOn();
            
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

            radioLog.ForEach(Console.WriteLine);
            
            var actualLog = pingActor.Get().IntentLog.Concat(pongActor.Get().IntentLog).OrderBy(s => s).ToArray();
            var expectedLog = new[] {
                "10:30:11:ping1->pong1:PING#1",
                "10:30:16:pong1->ping1:PONG#1",
                "10:30:26:ping1->pong1:PING#2",
                "10:30:31:pong1->ping1:PONG#2",
                "10:30:41:ping1->pong1:PING#3",
                "10:30:46:pong1->ping1:PONG#3",
                "10:30:56:ping1->pong1:PING#4",
            };

            actualLog.Should().BeStrictlyEquivalentTo(expectedLog);
        }
    }
}


