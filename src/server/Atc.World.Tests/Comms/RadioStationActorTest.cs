using System;
using System.Collections.Generic;
using System.Linq;
using Atc.Data.Primitives;
using Atc.Speech.Abstractions;
using Atc.World.Abstractions;
using Atc.World.Comms;
using FluentAssertions;
using NUnit.Framework;

namespace Atc.World.Tests.Comms
{
    [TestFixture]
    public class RadioStationActorTest
    {
        [Test]
        public void ShouldAddGroundStation()
        {
            var setup = new WorldSetup();
            
            var (station, aether) = setup.AddGroundStation(
                Frequency.FromKhz(121000), new GeoPoint(40, 12), "ground1");

            station.Get().PowerOn();
            
            station.Get().IsPoweredOn().Should().BeTrue();
            station.Get().Aether.Should().Be(aether);
            aether.Get().StationById[station.UniqueId].Should().Be(station);
        }

        [Test]
        public void ShouldTuneToGroundStation()
        {
            var setup = new WorldSetup();
            
            var (groundStation, aether) = setup.AddGroundStation(
                Frequency.FromKhz(121000), new GeoPoint(40, 12), "ground1");

            var airStation = setup.AddAirStation(
                Frequency.FromKhz(121000), new GeoPoint(40.1, 12.1), Altitude.FromFeetMsl(1000), callsign: "air1");

            airStation.Get().PowerOn();

            airStation.Get().Aether.Should().Be(aether);
            aether.Get().StationById[airStation.UniqueId].Should().Be(airStation);
        }

        [Test]
        public void ShouldNotTuneToUnreachableGroundStation()
        {
            var setup = new WorldSetup();
            
            var (groundStation, aether) = setup.AddGroundStation(
                Frequency.FromKhz(121000), new GeoPoint(-140, 12), "ground1");

            var airStation = setup.AddAirStation(
                Frequency.FromKhz(121000), new GeoPoint(40.1, 12.1), Altitude.FromFeetMsl(1000), callsign: "air1");

            airStation.Get().PowerOn();
            airStation.Get().Aether.HasValue.Should().BeFalse();
        }

        [Test]
        public void ShouldNotTuneToGroundStationOnDifferentFrequency()
        {
            var setup = new WorldSetup();
            
            var aether = setup.AddGroundStation(
                Frequency.FromKhz(129000), new GeoPoint(40, 12), "ground1");

            var airStation = setup.AddAirStation(
                Frequency.FromKhz(121000), new GeoPoint(40.1, 12.1), Altitude.FromFeetMsl(1000), callsign: "air1");

            airStation.Get().PowerOn();
            airStation.Get().Aether.HasValue.Should().BeFalse();
        }

        [Test]
        public void ShouldTransmitAndReceive()
        {
            var setup = new WorldSetup();
            var listenerLog = new List<string>();
            
            var (groundStation1, aether1) = setup.AddGroundStation(
                Frequency.FromKhz(123000), new GeoPoint(40, 12), "ground1");
            var airStation1 = setup.AddAirStation(
                Frequency.FromKhz(123000), new GeoPoint(40.1, 12), Altitude.FromFeetMsl(1000), callsign: "air1");
            var airStation2 = setup.AddAirStation(
                Frequency.FromKhz(123000), new GeoPoint(39.9, 12), Altitude.FromFeetMsl(1000), callsign: "air2");
            var airStation3 = setup.AddAirStation(
                Frequency.FromKhz(123000), new GeoPoint(40, 12.1), Altitude.FromFeetMsl(1000), callsign: "air3");

            groundStation1.Get().PowerOn();
            airStation1.Get().PowerOn();
            airStation2.Get().PowerOn();
            airStation3.Get().PowerOn();
            setup.World.Get().ProgressBy(TimeSpan.FromSeconds(30));

            RadioStationActor.ListenerCallback listener = (station, status, intent) => {
                listenerLog.Add(
                    $"{station.Callsign}:{status}" + 
                    (intent != null ? $"+received:{intent.Header.Type}" : string.Empty)
                );
            };
            RadioStationActor.IntentReceivedCallback intentReceived = (station, transmission, intent) => {
                listenerLog.Add(
                    $"{station.Callsign}:received-intent:{intent.Header.Type}");
            };
            
            groundStation1.Get().AddListener(listener, out _);
            groundStation1.Get().IntentReceived += intentReceived;
            
            airStation1.Get().AddListener(listener, out _);
            airStation1.Get().IntentReceived += intentReceived;
            
            airStation2.Get().AddListener(listener, out _);
            airStation2.Get().IntentReceived += intentReceived;
            
            airStation3.Get().AddListener(listener, out _);
            airStation3.Get().IntentReceived += intentReceived;

            var intent = new TestGreetingIntent(setup.WorldContext, 1, from: airStation2, to: groundStation1); 
            var transmission = new RadioTransmissionWave(
                new UtteranceDescription("en-US", new UtteranceDescription.Part[0], TimeSpan.FromSeconds(5)),
                VoiceDescription.Default,  
                null);
            
            airStation2.Get().BeginTransmission(transmission);
            airStation2.Get().CompleteTransmission(intent);
            
            listenerLog.Take(4).Should().BeEquivalentTo(new[] {
                "ground1:Silence",
                "air1:Silence",
                "air2:Silence",
                "air3:Silence"
            });
            listenerLog.Skip(4).Take(4).Should().BeEquivalentTo(new[] {
                "air3:ReceivingSingleTransmission",
                "air1:ReceivingSingleTransmission",
                "ground1:ReceivingSingleTransmission",
                "air2:Transmitting",
            });
            listenerLog.Skip(8).Should().BeEquivalentTo(new[] {
                "air3:received-intent:Greeting",
                "air3:DetectingSilence+received:Greeting",
                "air1:received-intent:Greeting",
                "air1:DetectingSilence+received:Greeting",
                "ground1:received-intent:Greeting",
                "ground1:DetectingSilence+received:Greeting",
                "air2:DetectingSilence"
            });
        }

        [Test]
        public void ShouldBeDetectingSilenceAfterCompletedTransmission()
        {
            var setup = new WorldSetup();
            
            var (groundStation1, aether) = setup.AddGroundStation(
                Frequency.FromKhz(121000), new GeoPoint(40, 12), "ground1");

            var airStation1 = setup.AddAirStation(
                Frequency.FromKhz(121000), new GeoPoint(40.1, 12.1), Altitude.FromFeetMsl(1000), callsign: "air1");

            groundStation1.Get().PowerOn();
            airStation1.Get().PowerOn();
            setup.World.Get().ProgressBy(TimeSpan.FromSeconds(30));

            var intent = new TestGreetingIntent(setup.WorldContext, 1, from: airStation1, to: groundStation1); 
            var transmission = new RadioTransmissionWave(
                new UtteranceDescription("en-US", new UtteranceDescription.Part[0], TimeSpan.FromSeconds(5)),
                VoiceDescription.Default,  
                null);

            airStation1.Get().BeginTransmission(transmission);
            
            airStation1.Get().GetStatus().Should().Be(TransceiverStatus.Transmitting);
            groundStation1.Get().GetStatus().Should().Be(TransceiverStatus.ReceivingSingleTransmission);

            airStation1.Get().CompleteTransmission(intent);
            
            airStation1.Get().GetStatus().Should().Be(TransceiverStatus.DetectingSilence);
            groundStation1.Get().GetStatus().Should().Be(TransceiverStatus.DetectingSilence);
        }

        [Test]
        public void ShouldBeDetectingSilenceAfterAbortedTransmission()
        {
            var setup = new WorldSetup();
            
            var (groundStation1, aether) = setup.AddGroundStation(
                Frequency.FromKhz(121000), new GeoPoint(40, 12), "ground1");

            var airStation1 = setup.AddAirStation(
                Frequency.FromKhz(121000), new GeoPoint(40.1, 12.1), Altitude.FromFeetMsl(1000), callsign: "air1");

            groundStation1.Get().PowerOn();
            airStation1.Get().PowerOn();
            setup.World.Get().ProgressBy(TimeSpan.FromSeconds(30));

            var transmission = new RadioTransmissionWave(
                new UtteranceDescription("en-US", new UtteranceDescription.Part[0], TimeSpan.FromSeconds(5)),
                VoiceDescription.Default,  
                null);

            airStation1.Get().BeginTransmission(transmission);
            
            airStation1.Get().GetStatus().Should().Be(TransceiverStatus.Transmitting);
            groundStation1.Get().GetStatus().Should().Be(TransceiverStatus.ReceivingSingleTransmission);

            airStation1.Get().AbortTransmission();
            
            airStation1.Get().GetStatus().Should().Be(TransceiverStatus.DetectingSilence);
            groundStation1.Get().GetStatus().Should().Be(TransceiverStatus.DetectingSilence);
        }

        [Test]
        public void ShouldBecomeSilentWithinIntervalAfterEndedTransmission()
        {
            var setup = new WorldSetup();
            
            var (groundStation1, aether) = setup.AddGroundStation(
                Frequency.FromKhz(121000), new GeoPoint(40, 12), "ground1");

            var airStation1 = setup.AddAirStation(
                Frequency.FromKhz(121000), new GeoPoint(40.1, 12.1), Altitude.FromFeetMsl(1000), callsign: "air1");

            groundStation1.Get().PowerOn();
            airStation1.Get().PowerOn();
            setup.World.Get().ProgressBy(TimeSpan.FromSeconds(30));

            var intent = new TestGreetingIntent(setup.WorldContext, 1, from: airStation1, to: groundStation1); 
            var transmission = new RadioTransmissionWave(
                new UtteranceDescription("en-US", new UtteranceDescription.Part[0], TimeSpan.FromSeconds(5)),
                VoiceDescription.Default,  
                null);

            airStation1.Get().BeginTransmission(transmission);
            airStation1.Get().CompleteTransmission(intent);

            setup.World.Get().ProgressBy(TimeSpan.FromMinutes(1));
            
            airStation1.Get().GetStatus().Should().Be(TransceiverStatus.Silence);
            groundStation1.Get().GetStatus().Should().Be(TransceiverStatus.Silence);
        }
    }
}
