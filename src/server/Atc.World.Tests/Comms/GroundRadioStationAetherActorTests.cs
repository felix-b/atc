using System;
using System.Net;
using Atc.Data.Primitives;
using Atc.World.Abstractions;
using Atc.World.Comms;
using Atc.World.Testability;
using Atc.World.Testability.AI;
using Atc.World.Testability.Comms;
using Atc.World.Tests.AI;
using FluentAssertions;
using NUnit.Framework;
using Zero.Loss.Actors;

namespace Atc.World.Tests.Comms
{
    [TestFixture]
    public class GroundRadioStationAetherActorTests
    {
        private record TestCaseEnvironment (
            WorldSetup Setup,
            ActorRef<GroundRadioStationAetherActor> Aether,
            ActorRef<RadioStationActor> GroundStation,
            ActorRef<TestRadioOperatingActor> GroundActor,
            ActorRef<RadioStationActor> AirStation1,
            ActorRef<TestRadioOperatingActor> AirActor1,
            ActorRef<RadioStationActor> AirStation2,
            ActorRef<TestRadioOperatingActor> AirActor2
        );
        
        private TestCaseEnvironment CreateTestCaseEnvironment()
        {
            var setup = new WorldSetup();

            var (groundStation, aether) = setup.AddGroundStation(
                Frequency.FromKhz(121000),
                new GeoPoint(40, 12),
                callsign: "ground1");
            groundStation.Get().PowerOn();

            var airStation1 = setup.AddAirStation(
                Frequency.FromKhz(121000), 
                new GeoPoint(40.1, 12.1), 
                Altitude.FromFeetMsl(1000), 
                callsign: "air1");
            airStation1.Get().PowerOn();

            var airStation2 = setup.AddAirStation(
                Frequency.FromKhz(121000), 
                new GeoPoint(40.1, 12.1), 
                Altitude.FromFeetMsl(1000), 
                callsign: "air2");
            airStation2.Get().PowerOn();

            TestRadioOperatingActor.RegisterType(setup.Supervisor);
            var groundActor = setup.Supervisor.CreateActor<TestRadioOperatingActor>(uniqueId =>
                new TestRadioOperatingActor.TestActivationEvent(uniqueId, groundStation)
            );
            var airActor1 = setup.Supervisor.CreateActor<TestRadioOperatingActor>(uniqueId =>
                new TestRadioOperatingActor.TestActivationEvent(uniqueId, airStation1)
            );
            var airActor2 = setup.Supervisor.CreateActor<TestRadioOperatingActor>(uniqueId =>
                new TestRadioOperatingActor.TestActivationEvent(uniqueId, airStation2)
            );

            return new TestCaseEnvironment(
                setup, 
                aether, 
                groundStation, 
                groundActor, 
                airStation1, 
                airActor1, 
                airStation2, 
                airActor2);
        }
        
        [Test]
        public void Silence_EnqueueTransmission_TransmitImmediately()
        {
            var t = CreateTestCaseEnvironment();
            
            t.Setup.World.Get().ProgressBy(TimeSpan.FromSeconds(30));
            t.AirStation1.Get().GetStatus().Should().Be(TransceiverStatus.Silence);

            var intent = new TestSimplestIntent(t.Setup.WorldContext.UtcNow(), t.GroundStation, t.AirStation1); 
            t.GroundActor.Get().InitiateTransmission(intent);

            t.Setup.World.Get().ProgressBy(TimeSpan.FromMilliseconds(1));
            t.AirStation1.Get().GetStatus().Should().Be(TransceiverStatus.ReceivingSingleTransmission);
        }

        [Test]
        public void TransmissionInProgress_EnqueueTransmission_Pending()
        {
            var t = CreateTestCaseEnvironment();
        
            t.Setup.World.Get().ProgressBy(TimeSpan.FromSeconds(30));
            t.AirStation1.Get().GetStatus().Should().Be(TransceiverStatus.Silence);

            var intent1 = new TestSimplestIntent(t.Setup.WorldContext.UtcNow(), t.GroundStation, t.AirStation1); 
            t.GroundActor.Get().InitiateTransmission(intent1);
            t.Setup.World.Get().ProgressBy(TimeSpan.FromMilliseconds(1));
            
            var intent2 = new TestSimplestIntent(t.Setup.WorldContext.UtcNow(), t.AirStation2, t.GroundStation); 
            t.AirActor1.Get().InitiateTransmission(intent2);
            t.Setup.World.Get().ProgressBy(TimeSpan.FromMilliseconds(1));
            
            t.AirStation1.Get().GetStatus().Should().Be(TransceiverStatus.ReceivingSingleTransmission);

            t.Setup.World.Get().ProgressBy(TimeSpan.FromSeconds(6));

            t.AirStation1.Get().GetStatus().Should().Be(TransceiverStatus.DetectingSilence);
        }

        [Test]
        public void TransmissionInProgress_EnqueueTransmission_WaitForSilenceBeforeNewConversation()
        {
            var t = CreateTestCaseEnvironment();
        
            t.Setup.World.Get().ProgressBy(TimeSpan.FromSeconds(30));

            var intent1 = new TestSimplestIntent(t.Setup.WorldContext.UtcNow(), t.GroundStation, t.AirStation1); 
            t.GroundActor.Get().InitiateTransmission(intent1);
            
            var intent2 = new TestSimplestIntent(t.Setup.WorldContext.UtcNow(), t.AirStation2, t.GroundStation); 
            t.AirActor2.Get().InitiateTransmission(intent2);

            t.AirStation1.Get().GetStatus().Should().Be(TransceiverStatus.ReceivingSingleTransmission);

            t.Setup.World.Get().ProgressBy(TimeSpan.FromMilliseconds(5001));
            
            t.AirStation1.Get().GetStatus().Should().Be(TransceiverStatus.DetectingSilence);

            t.Setup.World.Get().ProgressBy(TimeSpan.FromMilliseconds(2998));

            t.AirStation1.Get().GetStatus().Should().Be(TransceiverStatus.DetectingSilence);

            t.Setup.World.Get().ProgressBy(TimeSpan.FromMilliseconds(2));

            t.AirStation1.Get().GetStatus().Should().Be(TransceiverStatus.ReceivingSingleTransmission);
        }

        [Test]
        public void TransmissionFinished_EnqueueReadbackTransmission_NoWait()
        {
            var t = CreateTestCaseEnvironment();
        
            t.Setup.World.Get().ProgressBy(TimeSpan.FromSeconds(30));

            var intent1 = new TestSimplestIntent(t.Setup.WorldContext.UtcNow(), t.GroundStation, t.AirStation1); 
            t.GroundActor.Get().InitiateTransmission(intent1);
            
            t.AirStation1.Get().GetStatus().Should().Be(TransceiverStatus.ReceivingSingleTransmission);

            t.Setup.World.Get().ProgressBy(TimeSpan.FromMilliseconds(5001));
            
            t.AirStation1.Get().GetStatus().Should().Be(TransceiverStatus.DetectingSilence);

            var intent2 = new TestSimplestIntent(t.Setup.WorldContext.UtcNow(), t.AirStation1, t.GroundStation); 
            t.AirActor1.Get().InitiateTransmission(intent2);

            t.AirStation1.Get().GetStatus().Should().Be(TransceiverStatus.Transmitting);
        }
    }
}
