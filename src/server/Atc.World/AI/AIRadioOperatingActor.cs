using System;
using Atc.World.Abstractions;
using Atc.World.Comms;
using Zero.Loss.Actors;

namespace Atc.World.AI
{
    public abstract class AIRadioOperatingActor<TState> : RadioOperatingActor<TState>, IStartableActor
        where TState : RadioOperatorState
    {
        private ImmutableStateMachine _stateMachine;
        
        protected AIRadioOperatingActor(
            string typeString, 
            IStateStore store, 
            IVerbalizationService verbalizationService, 
            IWorldContext world, 
            PartyDescription party, 
            RadioOperatorActivationEvent activation, 
            TState initialState) 
            : base(typeString, store, verbalizationService, world, party, activation, initialState)
        {
            _stateMachine = ImmutableStateMachine.Empty;
        }

        void IStartableActor.Start()
        {
            OnStart();
        }
        
        protected virtual void OnStart()
        {
            _stateMachine = CreateStateMachine();
            _stateMachine.Start();
        }
        
        protected override void ReceiveIntent(Intent intent)
        {
            _stateMachine.ReceiveIntent(intent);
        }

        protected abstract ImmutableStateMachine CreateStateMachine();

        protected override void OnTransmissionStarted()
        {
            _stateMachine.ReceiveTrigger(AIRadioOperatingActor.TransmissionStartedTriggerId);
        }

        protected override void OnTransmissionFinished()
        {
            _stateMachine.ReceiveTrigger(AIRadioOperatingActor.TransmissionFinishedTriggerId);
        }

        protected void DispatchStateMachineEvent(IStateEvent @event)
        {
            Store.Dispatch(this, @event);
        }

        protected void ScheduleStateMachineDelay(TimeSpan interval, Action onDue)
        {
            World.DeferBy(interval, onDue);
        }

        protected ImmutableStateMachine.Builder CreateStateMachineBuilder(string initialStateName)
        {
            return new ImmutableStateMachine.Builder(initialStateName, DispatchStateMachineEvent, ScheduleStateMachineDelay);
        }
    }

    public static class AIRadioOperatingActor
    {
        public static readonly string TransmissionStartedTriggerId = "TRANSMISSION_STARTED";
        public static readonly string TransmissionFinishedTriggerId = "TRANSMISSION_FINISHED";
    }
}
