using System;
using System.Collections.Immutable;
using Atc.World.Abstractions;
using Atc.World.Comms;
using Zero.Loss.Actors;

namespace Atc.World.AI
{
    public abstract class AIRadioOperatingActor<TState> : RadioOperatingActor<TState>, IStartableActor
        where TState : AIRadioOperatorState
    {
        public record InitStateMachineEvent : IStateEvent;

        private readonly AIRadioOperatingActor.ILogger _logger;
        
        [NotEventSourced]
        private IDeferHandle? _currentStateTimeoutHandle = null;
        
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
            Store.Dispatch(this, new InitStateMachineEvent());
            StartNewState(State.StateMachine.Age);
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
                        StateMachine = CreateStateMachine() 
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
                    World.Defer(
                        $"{UniqueId}|start-state|{newStateMachine.State.Name}|age={newStateMachine.Age}",
                        () => StartNewState(newStateMachine.Age));
                    
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
            return World.DeferBy($"{UniqueId}|schedule-delay|{interval.TotalSeconds}", interval, onDue);
        }

        protected ImmutableStateMachine.Builder CreateStateMachineBuilder(string initialStateName)
        {
            return new ImmutableStateMachine.Builder(initialStateName, DispatchStateMachineEvent, ScheduleStateMachineDelay);
        }

        protected ImmutableStateMachine GetCurrentStateMachineSnapshot()
        {
            return State.StateMachine;
        }

        private void StartNewState(ulong machineAge)
        {
            if (State.StateMachine.Age > machineAge)
            {
                return;
            }
            
            _currentStateTimeoutHandle?.Cancel();
            
            var newMachine = State.StateMachine;
            var newAge = newMachine.Age;
            var newTimeoutInterval = newMachine.State.TimeoutInterval;
            if (newTimeoutInterval.HasValue)
            {
                _currentStateTimeoutHandle = World.DeferBy(
                    $"{UniqueId}|state-timeout|{newMachine.State.Name}",
                    newTimeoutInterval.Value, 
                    () => {
                        if (State.StateMachine.Age == newAge)
                        {
                            State.StateMachine.ReceiveTimeout();
                        }
                    }
                );
            }
            
            newMachine.Start();
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
