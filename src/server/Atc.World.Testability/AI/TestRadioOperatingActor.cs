using Atc.World.Abstractions;
using Atc.World.AI;
using Atc.World.Comms;
using Zero.Loss.Actors;

namespace Atc.World.Testability.AI
{
    public class TestRadioOperatingActor : AIRadioOperatingActor<TestRadioOperatingActor.TestActorState>
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
        ) : RadioOperatorActivationEvent(UniqueId, Radio), IActivationStateEvent<TestRadioOperatingActor>;

        public TestRadioOperatingActor(
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
            DispatchStateMachineEvent(new ImmutableStateMachine.TriggerEvent(Age: 1, "ABC"));
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
            supervisor.RegisterActorType<TestRadioOperatingActor, TestActivationEvent>(
                TypeString,
                (activation, dependencies) => new TestRadioOperatingActor(
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