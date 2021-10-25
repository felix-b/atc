using Atc.Speech.Abstractions;
using Atc.World.Abstractions;
using Atc.World.Comms;
using Zero.Loss.Actors;

namespace Atc.World.LLHZ
{
    public class LlhzClearanceControllerActor : 
        RadioOperatingActor<LlhzClearanceControllerActor.DeliveryControllerState>
    {
        public const string TypeString = "llhz/atc/clrdel";

        public record DeliveryControllerState(
            ActorRef<RadioStationActor> Radio,
            Intent? PendingTransmissionIntent
        ) : RadioOperatorState(Radio, PendingTransmissionIntent);

        public record ActivationEvent(
            string UniqueId,
            ActorRef<RadioStationActor> Radio
        ) : RadioOperatorActivationEvent(UniqueId, Radio), IActivationStateEvent<LlhzClearanceControllerActor>;

        public LlhzClearanceControllerActor(ActivationEvent activation, IStateStore store, IWorldContext world, IVerbalizationService verbaliarionService) 
            : base(
                TypeString, 
                store, 
                verbaliarionService, 
                world, 
                CreateParty(), 
                activation, 
                new DeliveryControllerState(activation.Radio, null))
        {
            State.Radio.Get().PowerOn();
        }

        protected override void ReceiveIntent(Intent intent)
        {
            throw new System.NotImplementedException();
        }

        protected override DeliveryControllerState Reduce(DeliveryControllerState stateBefore, IStateEvent @event)
        {
            return stateBefore;
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
    }
}
