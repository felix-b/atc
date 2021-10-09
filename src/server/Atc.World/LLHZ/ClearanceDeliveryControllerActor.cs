using Atc.World.Abstractions;
using Atc.World.Comms;
using Zero.Loss.Actors;

namespace Atc.World.LLHZ
{
    public class ClearanceDeliveryControllerActor : 
        StatefulActor<ClearanceDeliveryControllerActor.DeliveryControllerState>,
        IRadioOperatingActor
    {
        public const string TypeString = "atc/llhz/clrdel";

        public record DeliveryControllerState(
            ActorRef<RadioStationActor> Radio
        );

        public record ActivationEvent(
            string UniqueId,
            ActorRef<RadioStationActor> Radio
        ) : IActivationStateEvent<ClearanceDeliveryControllerActor>;

        public ClearanceDeliveryControllerActor(ActivationEvent activation) 
            : base(TypeString, activation.UniqueId, new DeliveryControllerState(activation.Radio))
        {
            State.Radio.Get().PowerOn();
        }

        public void BeginQueuedTransmission(int cookie)
        {
            
        }
        
        public PartyDescription Party { get; }

        protected override DeliveryControllerState Reduce(DeliveryControllerState stateBefore, IStateEvent @event)
        {
            return stateBefore;
        }

    }
}
