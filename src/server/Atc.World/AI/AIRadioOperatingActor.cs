using System;
using Atc.World.Abstractions;
using Atc.World.Comms;
using Zero.Loss.Actors;

namespace Atc.World.AI
{
    public abstract class AIRadioOperatingActor<TState> : RadioOperatingActor<TState>, IStartableActor
        where TState : AIRadioOperatorState
    {
        public record InitStateMachineEvent(
            ImmutableStateMachine StateMachine
        ) : IStateEvent;
        
        private readonly AIRadioOperatingActor.ILogger _logger;
        
        protected AIRadioOperatingActor(
            string typeString, 
            IStateStore store, 
            IVerbalizationService verbalizationService, 
            IWorldContext world,
            AIRadioOperatingActor.ILogger logger,
            PartyDescription party, 
            RadioOperatorActivationEvent activation, 
            TState initialState) 
            : base(typeString, store, verbalizationService, world, party, activation, initialState)
        {
            _logger = logger;
        }

        void IStartableActor.Start()
        {
            OnStart();
        }
        
        protected virtual void OnStart()
        {
            var stateMachine = CreateStateMachine();
            Store.Dispatch(this, new InitStateMachineEvent(stateMachine));
            stateMachine.Start();
        }
        
        protected override void ReceiveIntent(Intent intent)
        {
            State.StateMachine.ReceiveIntent(intent);
        }

        protected override TState Reduce(TState stateBefore, IStateEvent @event)
        {
            switch (@event)
            {
                case InitStateMachineEvent init:
                    return stateBefore with {
                        StateMachine = init.StateMachine 
                    };
                case IImmutableStateMachineEvent machineEvent:
                    var oldStateMachine = stateBefore.StateMachine;
                    if (machineEvent.Age < oldStateMachine.Age)
                    {
                        throw new InvalidActorEventException(
                            $"Attempt to dispatch an older age event ({machineEvent.Age}), but machine age is {oldStateMachine.Age}");
                    }
                    var newStateMachine = ImmutableStateMachine.Reduce(oldStateMachine, machineEvent);
                    if (newStateMachine == oldStateMachine)
                    {
                        return stateBefore;
                    }
                    
                    _logger.ActorTransitionedState(UniqueId, oldStateMachine.State.Name, machineEvent.ToString()!, newStateMachine.State.Name);
                    World.Defer(() => {
                        newStateMachine.Start();
                    });

                    return stateBefore with {
                        StateMachine = newStateMachine
                    };
                default:            
                    return base.Reduce(stateBefore, @event);
            }
        }

        protected abstract ImmutableStateMachine CreateStateMachine();

        protected override void OnTransmissionStarted()
        {
            State.StateMachine.ReceiveTrigger(AIRadioOperatingActor.TransmissionStartedTriggerId);
        }

        protected override void OnTransmissionFinished()
        {
            State.StateMachine.ReceiveTrigger(AIRadioOperatingActor.TransmissionFinishedTriggerId);
        }

        protected void DispatchStateMachineEvent(IStateEvent @event)
        {
            if (@event is IImmutableStateMachineEvent machineEvent && machineEvent.Age < State.StateMachine.Age)
            {
                return;
            }
                
            Store.Dispatch(this, @event);
        }

        protected IDeferHandle ScheduleStateMachineDelay(TimeSpan interval, Action onDue)
        {
            return World.DeferBy(interval, onDue);
        }

        protected ImmutableStateMachine.Builder CreateStateMachineBuilder(string initialStateName)
        {
            return new ImmutableStateMachine.Builder(initialStateName, DispatchStateMachineEvent, ScheduleStateMachineDelay);
        }

        protected ImmutableStateMachine GetCurrentStateMachineSnapshot()
        {
            return State.StateMachine;
        }
    }

    public static class AIRadioOperatingActor
    {
        public static readonly string TransmissionStartedTriggerId = "TRANSMISSION_STARTED";
        public static readonly string TransmissionFinishedTriggerId = "TRANSMISSION_FINISHED";

        public interface ILogger
        {
            void ActorTransitionedState(string actorId, string oldState, string trigger, string newState);
        }
    }

    public abstract record AIRadioOperatorState(
        ActorRef<RadioStationActor> Radio,
        Intent? PendingTransmissionIntent,
        ImmutableStateMachine StateMachine
    ) : RadioOperatorState(Radio, PendingTransmissionIntent);
}
