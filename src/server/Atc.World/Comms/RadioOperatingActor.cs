using System;
using Atc.World.Abstractions;
using Zero.Loss.Actors;

namespace Atc.World.Comms
{
    public record RadioOperatorActivationEvent(
        string UniqueId,
        ActorRef<RadioStationActor> Radio
    ) : IActivationStateEvent;

    public record RadioOperatorState(
        ActorRef<RadioStationActor> Radio,
        Intent? PendingTransmissionIntent 
    );

    public record SetPendingIntentEvent(Intent? Intent) : IStateEvent;
    
    public abstract class RadioOperatingActor<TState> : StatefulActor<TState>, IRadioOperatingActor
        where TState : RadioOperatorState
    {
        protected RadioOperatingActor(
            string typeString,
            IStateStore store,
            IWorldContext world,
            PartyDescription party, 
            RadioOperatorActivationEvent activation, 
            TState initialState) : 
            base(typeString, activation.UniqueId, initialState)
        {
            Store = store;
            World = world;
            Party = party;
            
            Radio.IntentReceived += (station, transmission, intent) => ReceiveIntent(intent);
            Radio.PowerOn();
        }

        protected override TState Reduce(TState stateBefore, IStateEvent @event)
        {
            switch (@event)
            {
                case SetPendingIntentEvent setPending:
                    return stateBefore with {
                        PendingTransmissionIntent = setPending.Intent
                    };
                    break;
                default:
                    return stateBefore;
            }
        }

        public void BeginQueuedTransmission(int cookie)
        {
            var intent = State.PendingTransmissionIntent
                ?? throw new InvalidOperationException($"{UniqueId}: no pending intent in {nameof(BeginQueuedTransmission)}");
                
            var wave = new RadioTransmissionWave(
                "en-US", //TODO: allow specifying a language
                new byte[0],
                TimeSpan.FromSeconds(5),
                intent);
            
            Store.Dispatch(this, new SetPendingIntentEvent(null));
            State.Radio.Get().BeginTransmission(wave);
            
            //TODO: we must have a feedback from speech synthesis about the actual duration of the transmission!!
            World.DeferBy(TimeSpan.FromSeconds(5), () => {  
                State.Radio.Get().CompleteTransmission(wave.GetIntentOrThrow());
            });
        }

        public PartyDescription Party { get; }
        public IStateStore Store { get; }
        public IWorldContext World { get; }

        public RadioStationActor Radio => State.Radio.Get();

        protected abstract void ReceiveIntent(Intent intent);

        protected void Transmit(Intent intent)
        {
            Store.Dispatch(this, new SetPendingIntentEvent(intent));
            Radio.AIEnqueueForTransmission(this, 0 ,out _);
        }
    }
}