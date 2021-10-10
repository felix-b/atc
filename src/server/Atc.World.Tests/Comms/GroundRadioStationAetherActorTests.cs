using System;
using System.Net;
using Atc.Data.Primitives;
using Atc.World.Comms;
using FluentAssertions;
using NUnit.Framework;

namespace Atc.World.Tests.Comms
{
    [TestFixture]
    public class GroundRadioStationAetherActorTests
    {
        [Test, Ignore("need to update")]
        public void Silence_EnqueueTransmission_TransmitImmediately()
        {
            var setup = new WorldSetup();

            var (groundStation, aether) = setup.AddGroundStation(
                Frequency.FromKhz(121000), new GeoPoint(40, 12), "ground1");
            groundStation.Get().PowerOn();

            var airStation1 = setup.AddAirStation(
                Frequency.FromKhz(121000), new GeoPoint(40.1, 12.1), Altitude.FromFeetMsl(1000), callsign: "air1");
            airStation1.Get().PowerOn();

            setup.World.Get().ProgressBy(TimeSpan.FromSeconds(30));
            
            // var wave = new RadioTransmissionWave(
            //     "en-US",
            //     new byte[0], 
            //     TimeSpan.FromSeconds(3), 
            //     new TestGreetingIntent(setup.WorldContext, 1, groundStation, airStation1));
            
            //groundStation.Get().AIEnqueueForTransmission(this, cookie: 123);

            setup.World.Get().ProgressBy(TimeSpan.FromMilliseconds(1));

            airStation1.Get().GetStatus().Should().Be(TransceiverStatus.ReceivingSingleTransmission);
        }
    }
}