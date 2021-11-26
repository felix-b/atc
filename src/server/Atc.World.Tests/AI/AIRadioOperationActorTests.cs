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

        public class TestActor : AIRadioOperatingActor<RadioOperatorState>
        {
            public const string TypeString = "test";
            
            public record TestActivationEvent(
                string UniqueId, 
                ActorRef<RadioStationActor> Radio
            ) : RadioOperatorActivationEvent(UniqueId, Radio), IActivationStateEvent<TestActor>;
            
            public TestActor(
                IStateStore store, 
                IVerbalizationService verbalizationService, 
                IWorldContext world, 
                TestActivationEvent activation) 
                : base(
                    TypeString, 
                    store, 
                    verbalizationService, 
                    world, 
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
        
            private static RadioOperatorState CreateInitialState(TestActivationEvent activation)
            {
                return new RadioOperatorState(
                    Radio: activation.Radio, 
                    PendingTransmissionIntent: null);
            }
        }
    }
}
