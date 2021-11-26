using Atc.Data.Primitives;
using Atc.Speech.Abstractions;
using Atc.World.Abstractions;
using Atc.World.AI;
using Atc.World.Comms;
using Zero.Loss.Actors;

namespace Atc.World.LLHZ
{
    public class LlhzDeliveryControllerActor : 
        AIRadioOperatingActor<LlhzDeliveryControllerActor.DeliveryControllerState>
    {
        public const string TypeString = "llhz-atc-clrdel";

        public record DeliveryControllerState(
            ActorRef<RadioStationActor> Radio,
            Intent? PendingTransmissionIntent
        ) : RadioOperatorState(Radio, PendingTransmissionIntent);

        public record ActivationEvent(
            string UniqueId,
            ActorRef<RadioStationActor> Radio
        ) : RadioOperatorActivationEvent(UniqueId, Radio), IActivationStateEvent<LlhzDeliveryControllerActor>;

        public LlhzDeliveryControllerActor(ActivationEvent activation, IStateStore store, IWorldContext world, IVerbalizationService verbaliarionService) 
            : base(
                TypeString, 
                store, 
                verbaliarionService, 
                world, 
                CreateParty(), 
                activation, 
                CreateInitialState(activation))
        {
            State.Radio.Get().PowerOn();
            State.Radio.Get().TuneTo(Frequency.FromKhz(130850));
        }

        protected override ImmutableStateMachine CreateStateMachine()
        {
            throw new System.NotImplementedException();
        }

        private static PartyDescription CreateParty()
        {
            return new PersonDescription(
                "#1", 
                "Herzlia Clearance", 
                NatureType.AI, 
                VoiceDescription.Default, 
                GenderType.Male, 
                AgeType.Senior, 
                "Bob");
        }
        
        private static DeliveryControllerState CreateInitialState(ActivationEvent activation)
        {
            return new DeliveryControllerState(
                Radio: activation.Radio, 
                PendingTransmissionIntent: null);
        }
    }
}
