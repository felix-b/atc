using System;
using System.Collections.Immutable;
using Atc.Data.Primitives;
using Atc.World.Abstractions;
using Atc.World.AI;
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
    
    public record SetPendingIntentEvent(Intent Intent) : IStateEvent;

    public record ClearPendingIntentEvent() : IStateEvent;

    public abstract class RadioOperatingActor<TState> : StatefulActor<TState>, IPilotRadioOperatingActor
        where TState : RadioOperatorState
    {
        [NotEventSourced] 
        private IDeferHandle? _transmissionCompletionHandle = null;

        protected RadioOperatingActor(
            string typeString,
            IStateStore store,
            IVerbalizationService verbalizationService,
            IWorldContext world,
            PartyDescription party, 
            RadioOperatorActivationEvent activation, 
            TState initialState) : 
            base(typeString, activation.UniqueId, initialState)
        {
            Store = store;
            VerbalizationService = verbalizationService;
            World = world;
            Party = party;
            
            Radio.IntentReceived += (station, transmission, intent) => ReceiveIntent(intent);
            world.Defer($"power-on-radio|{UniqueId}", () => Radio.PowerOn());
        }

        public void MonitorFrequency(Frequency frequency)
        {
            Radio.TuneTo(frequency);
        }

        public virtual void InitiateTransmission(Intent intent)
        {
            Store.Dispatch(this, new SetPendingIntentEvent(intent));
            Radio.AIEnqueueForTransmission(this, 0 ,out _);
        }

        public virtual void BeginQueuedTransmission(int cookie)
        {
            var intent = State.PendingTransmissionIntent
                ?? throw new InvalidOperationException($"{UniqueId}: no pending intent in {nameof(BeginQueuedTransmission)}");
                
            var verbalizer = VerbalizationService.GetVerbalizer(Party);
            var utterance = verbalizer.VerbalizeIntent(Party, intent);
            var wave = new RadioTransmissionWave(
                Utterance: utterance,
                Voice: Party.Voice,
                SoundBuffers: null);
            
            Store.Dispatch(this, new ClearPendingIntentEvent());
            State.Radio.Get().BeginTransmission(wave, actualDuration => {
                _transmissionCompletionHandle.UpdateDeadline(World.UtcNow() + actualDuration);
            });

            OnTransmissionStarted();

            _transmissionCompletionHandle = World.DeferBy($"end-transmission|{UniqueId}", utterance.EstimatedDuration, () => {  
                _transmissionCompletionHandle = null;
                State.Radio.Get().CompleteTransmission(intent);
                OnTransmissionFinished();
            });
        }

        public PartyDescription Party { get; }

        IntentHeader IPilotRadioOperatingActor.CreateIntentHeader(WellKnownIntentType type, int customCode)
        {
            var fromStation = State.Radio.Get();
            var toStation = fromStation.Aether!.Value.Get().GroundStation.Get();

            return new IntentHeader(
                type,
                customCode,
                fromStation.UniqueId,
                fromStation.Callsign,
                toStation.UniqueId,
                toStation.Callsign,
                World.UtcNow());
        }
        
        protected override TState Reduce(TState stateBefore, IStateEvent @event)
        {
            switch (@event)
            {
                case SetPendingIntentEvent setPending:
                    return stateBefore with {
                        PendingTransmissionIntent = setPending.Intent
                    };
                case ClearPendingIntentEvent:
                    return stateBefore with {
                        PendingTransmissionIntent = null
                    };
                default:
                    return stateBefore;
            }
        }

        protected virtual void OnTransmissionStarted()
        {
        }

        protected virtual void OnTransmissionFinished()
        {
        }

        protected abstract void ReceiveIntent(Intent intent);

        protected IStateStore Store { get; }
        protected IWorldContext World { get; }
        protected IVerbalizationService VerbalizationService { get; }
        protected RadioStationActor Radio => State.Radio.Get();
    }
}
