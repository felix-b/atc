using System;
using Atc.World.Abstractions;
using Atc.World.Abstractions;
using Atc.World.Comms;
using Zero.Loss.Actors;

namespace Atc.World.Testability.Comms
{
    public class DummyCycledTransmittingActor : RadioOperatingActor<DummyCycledTransmittingActor.DummyState>
    {
        public static readonly string TypeString = "dummy-cycle";
        
        public record DummyState(
            ActorRef<RadioStationActor> Radio,
            Intent? PendingTransmissionIntent,
            int RepeatCount
        ) : RadioOperatorState(Radio, PendingTransmissionIntent);
        
        public record DummyActivationEvent(
            string UniqueId, 
            ActorRef<RadioStationActor> Radio
        ) : RadioOperatorActivationEvent(UniqueId, Radio), IActivationStateEvent<DummyCycledTransmittingActor>;
        
        public record IncrementRepeatCountEvent() : IStateEvent;

        public DummyCycledTransmittingActor(IStateStore store, IWorldContext world, IVerbalizationService verbalizationService, DummyActivationEvent activation) 
            : base(TypeString, store, verbalizationService, world, CreateParty(activation), activation, CreateInitialState(activation))
        {
            World.DeferBy("transmit-first", TimeSpan.FromSeconds(5), () => {
                State.Radio.Get().PowerOn();
                TransmitGreeting();
            });
        }

        private static DummyState CreateInitialState(DummyActivationEvent activation)
        {
            return new DummyState(RepeatCount: 0, Radio: activation.Radio, PendingTransmissionIntent: null);
        }

        protected override DummyState Reduce(DummyState stateBefore, IStateEvent @event)
        {
            return @event is IncrementRepeatCountEvent
                ? stateBefore with {RepeatCount = stateBefore.RepeatCount + 1}
                : base.Reduce(stateBefore, @event);
        }

        private void TransmitGreeting()
        {
            InitiateTransmission(new TestGreetingIntent(World, State.RepeatCount, this, null));
            
            Store.Dispatch(this, new IncrementRepeatCountEvent());
            World.DeferBy("transmit-next", TimeSpan.FromSeconds(5), TransmitGreeting);
        }

        public static void RegisterType(ISupervisorActorInit supervisor)
        {
            supervisor.RegisterActorType<DummyCycledTransmittingActor, DummyActivationEvent>(
                TypeString,
                (activation, dependencies) => new DummyCycledTransmittingActor(
                    dependencies.Resolve<IStateStore>(),
                    dependencies.Resolve<IWorldContext>(), 
                    dependencies.Resolve<IVerbalizationService>(), 
                    activation
                )
            );
        }

        protected override void ReceiveIntent(Intent intent)
        {
            //nothing
        }

        private static PartyDescription CreateParty(DummyActivationEvent activation)
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
    }
}
