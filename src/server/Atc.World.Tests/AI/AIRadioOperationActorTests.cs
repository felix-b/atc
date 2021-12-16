using System;
using Atc.Data.Primitives;
using Atc.World.Abstractions;
using Atc.World.Abstractions;
using Atc.World.AI;
using Atc.World.Comms;
using Atc.World.Testability;
using Atc.World.Testability.AI;
using FluentAssertions;
using NUnit.Framework;
using Zero.Loss.Actors;

namespace Atc.World.Tests.AI
{
    [TestFixture]
    public class AIRadioOperationActorTests
    {
        [Test]
        public void CanDispatchStateMachineEvents()
        {
            var setup = new WorldSetup();
            TestRadioOperatingActor.RegisterType(setup.Supervisor);
            
            var radio = setup.AddGroundStation(
                Frequency.FromKhz(118000),
                new GeoPoint(32, 34),
                "R1").Station;  

            var actor = setup.Supervisor.CreateActor<TestRadioOperatingActor>(uniqueId =>
                new TestRadioOperatingActor.TestActivationEvent(uniqueId, radio)
            ).Get();

            actor.CurrentStateName.Should().Be("START");
            actor.ReceiveTestTrigger();
            actor.CurrentStateName.Should().Be("END");
        }

        [Test]
        public void CanTimeTravel()
        {
            var setup = new WorldSetup();
            TestRadioOperatingActor.RegisterType(setup.Supervisor);
            
            var radio = setup.AddGroundStation(
                Frequency.FromKhz(118000),
                new GeoPoint(32, 34),
                "R1").Station;  

            var actor = setup.Supervisor.CreateActor<TestRadioOperatingActor>(uniqueId =>
                new TestRadioOperatingActor.TestActivationEvent(uniqueId, radio)
            ).Get();

            actor.CurrentStateName.Should().Be("START");
            var snapshot = setup.Supervisor.TimeTravel.TakeSnapshot();
            
            actor.ReceiveTestTrigger();
            actor.CurrentStateName.Should().Be("END");

            setup.Supervisor.TimeTravel.RestoreSnapshot(snapshot);
            actor.CurrentStateName.Should().Be("START");
        }

        [Test]
        public void CanTimeTravelWithResurrection()
        {
            var setup = new WorldSetup();
            TestRadioOperatingActor.RegisterType(setup.Supervisor);

            var snapshot0 = setup.Supervisor.TimeTravel.TakeSnapshot();

            var radio = setup.AddGroundStation(
                Frequency.FromKhz(118000),
                new GeoPoint(32, 34),
                "R1").Station;  

            var actorRef = setup.Supervisor.CreateActor<TestRadioOperatingActor>(uniqueId =>
                new TestRadioOperatingActor.TestActivationEvent(uniqueId, radio)
            );

            actorRef.Get().CurrentStateName.Should().Be("START");
            var snapshot1 = setup.Supervisor.TimeTravel.TakeSnapshot();
            
            actorRef.Get().ReceiveTestTrigger();
            actorRef.Get().CurrentStateName.Should().Be("END");
            var snapshot2 = setup.Supervisor.TimeTravel.TakeSnapshot();

            setup.Supervisor.TimeTravel.RestoreSnapshot(snapshot0);
            Assert.Throws<ActorNotFoundException>(() => actorRef.Get());
            
            setup.Supervisor.TimeTravel.RestoreSnapshot(snapshot2);
            actorRef.Get().CurrentStateName.Should().Be("END");
            
            setup.Supervisor.TimeTravel.RestoreSnapshot(snapshot1);
            actorRef.Get().CurrentStateName.Should().Be("START");
        }
    }
}
