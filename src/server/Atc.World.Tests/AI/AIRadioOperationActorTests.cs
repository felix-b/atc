using System;
using Atc.Data.Primitives;
using Atc.Speech.Abstractions;
using Atc.World.Abstractions;
using Atc.World.AI;
using Atc.World.Comms;
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
            TestActor.RegisterType(setup.Supervisor);
            
            var radio = setup.AddGroundStation(
                Frequency.FromKhz(118000),
                new GeoPoint(32, 34),
                "R1").Station;  

            var actor = setup.Supervisor.CreateActor<TestActor>(uniqueId =>
                new TestActor.TestActivationEvent(uniqueId, radio)
            ).Get();

            actor.CurrentStateName.Should().Be("START");
            actor.ReceiveTestTrigger();
            actor.CurrentStateName.Should().Be("END");
        }

        [Test]
        public void CanTimeTravel()
        {
            var setup = new WorldSetup();
            TestActor.RegisterType(setup.Supervisor);
            
            var radio = setup.AddGroundStation(
                Frequency.FromKhz(118000),
                new GeoPoint(32, 34),
                "R1").Station;  

            var actor = setup.Supervisor.CreateActor<TestActor>(uniqueId =>
                new TestActor.TestActivationEvent(uniqueId, radio)
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
            TestActor.RegisterType(setup.Supervisor);

            var snapshot0 = setup.Supervisor.TimeTravel.TakeSnapshot();

            var radio = setup.AddGroundStation(
                Frequency.FromKhz(118000),
                new GeoPoint(32, 34),
                "R1").Station;  

            var actorRef = setup.Supervisor.CreateActor<TestActor>(uniqueId =>
                new TestActor.TestActivationEvent(uniqueId, radio)
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

        public class TestActor : AIRadioOperatingActor<TestActor.TestActorState>
        {
            public const string TypeString = "test";

            public record TestActorState(
                ActorRef<RadioStationActor> Radio,
                Intent? PendingTransmissionIntent,
                ImmutableStateMachine StateMachine
            ) : AIRadioOperatorState(Radio, PendingTransmissionIntent, StateMachine);
            
            public record TestActivationEvent(
                string UniqueId, 
                ActorRef<RadioStationActor> Radio
            ) : RadioOperatorActivationEvent(UniqueId, Radio), IActivationStateEvent<TestActor>;

            public TestActor(
                IStateStore store,
                IVerbalizationService verbalizationService,
                IWorldContext world,
                AIRadioOperatingActor.ILogger logger,
                TestActivationEvent activation)
                : base(
                    TypeString,
                    store,
                    verbalizationService,
                    world, 
                    logger,
                    CreateParty(activation), 
                    activation, 
                    CreateInitialState(activation))
            {
            }

            public void ReceiveTestTrigger()
            {
                DispatchStateMachineEvent(new ImmutableStateMachine.TriggerEvent("ABC"));
            }

            public string CurrentStateName => GetCurrentStateMachineSnapshot().State.Name;

            protected override ImmutableStateMachine CreateStateMachine()
            {
                var builder = CreateStateMachineBuilder(initialStateName: "START");

                builder.AddState("START", state => state.OnTrigger("ABC", transitionTo: "END"));
                builder.AddState("END", state => { });
 
                return builder.Build();
            }

            public static void RegisterType(ISupervisorActorInit supervisor)
            {
                supervisor.RegisterActorType<TestActor, TestActivationEvent>(
                    TypeString,
                    (activation, dependencies) => new TestActor(
                        dependencies.Resolve<IStateStore>(),
                        dependencies.Resolve<IVerbalizationService>(), 
                        dependencies.Resolve<IWorldContext>(), 
                        dependencies.Resolve<AIRadioOperatingActor.ILogger>(), 
                        activation
                    )
                );
            }
            
            private static PartyDescription CreateParty(TestActivationEvent activation)
            {
                return new PersonDescription(
                    activation.UniqueId, 
                    callsign: activation.Radio.Get().Callsign, 
                    NatureType.AI, 
                    VoiceDescription.Default, 
                    GenderType.Male, 
                    AgeType.Senior,
                    firstName: "Bob");
            }
        
            private static TestActorState CreateInitialState(TestActivationEvent activation)
            {
                return new TestActorState(
                    Radio: activation.Radio, 
                    PendingTransmissionIntent: null,
                    StateMachine: ImmutableStateMachine.Empty);
            }
        }
    }
}
