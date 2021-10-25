using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.PortableExecutable;
using Atc.Data.Primitives;
using Atc.World.Abstractions;
using Atc.World.Comms;
using Zero.Loss.Actors;

namespace Atc.World.AI
{
    public class ImmutableStateMachine
    {
        public static readonly string IntentReceivedTriggerId = "INTENT_RECEIVED";
        public static readonly string ResumeAfterDelayTriggerId = "RESUME_AFTER_DELAY";
        
        public readonly ImmutableDictionary<string, StateDescription> StateByName;
        public readonly StateDescription State;
        public readonly ImmutableDictionary<Type, Intent> MemorizedIntentByType;

        private readonly Action<IStateEvent> _dispatchEvent;

        public ImmutableStateMachine(
            ImmutableDictionary<string, StateDescription> stateByName, 
            string currentStateName,
            ImmutableDictionary<Type, Intent> memorizedIntentByType,
            Action<IStateEvent> dispatchEvent)
        {
            StateByName = stateByName;
            State = currentStateName.Length > 0 ? StateByName[currentStateName] : StateDescription.Empty;
            MemorizedIntentByType = memorizedIntentByType;
            _dispatchEvent = dispatchEvent;
        }
        
        public void ReceiveIntent(Intent intent)
        {
            _dispatchEvent(new MachineEvent(IntentReceivedTriggerId, intent));
        }

        public void ReceiveTrigger(string triggerId)
        {
            _dispatchEvent(new MachineEvent(triggerId, Intent: null));
        }

        public static readonly ImmutableStateMachine Empty = new ImmutableStateMachine(
            ImmutableDictionary<string, StateDescription>.Empty, 
            string.Empty,
            ImmutableDictionary<Type, Intent>.Empty, 
            (e) => { });

        public static ImmutableStateMachine Reduce(ImmutableStateMachine machineBefore, IStateEvent @event)
        {
            //TBD
            return machineBefore;
        }

        public record StateDescription(
            string Name,
            Action OnEnter,
            ImmutableDictionary<string, TransitionDescription> TransitionByTriggerId)
        {
            public static readonly StateDescription Empty = new StateDescription(
                Name: string.Empty,
                OnEnter: () => { },
                TransitionByTriggerId: ImmutableDictionary<string, TransitionDescription>.Empty); 
        }

        public record TransitionDescription(
            string TriggerId,
            string StateName
        );

        public record MachineEvent(
            string TriggerId,
            Intent? Intent
        ) : IStateEvent;
        
        public class Builder
        {
            private readonly string _initialStateName;
            private readonly Action<IStateEvent> _dispatchEvent;
            private readonly Dictionary<string, StateBuilder> _stateByName = new();

            public Builder(string initialStateName, Action<IStateEvent> dispatchEvent)
            {
                _initialStateName = initialStateName;
                _dispatchEvent = dispatchEvent;
            }
            
            public Builder AddState(string name, Action<StateBuilder> build)
            {
                var newState = new StateBuilder(name);
                _stateByName.Add(name, newState);
                build(newState);
                return this;
            }

            public Builder AddConversationState(string name, Action<ConversationStateBuilder> state)
            {
                throw new NotImplementedException();
            }

            public ImmutableStateMachine Build()
            {
                var immutableStateByName = _stateByName
                    .Select(kvp => kvp.Value.BuildKeyValuePair())
                    .ToImmutableDictionary<string, StateDescription>();

                return new ImmutableStateMachine(
                    immutableStateByName,
                    _initialStateName,
                    memorizedIntentByType: ImmutableDictionary<Type, Intent>.Empty, 
                    _dispatchEvent);
            }
        }
        
        public class StateBuilder
        {
            private readonly string _name;
            private readonly Dictionary<string, StateBuilder> _stateByName = new();
            private SequenceBuilder? _onEnterSequence = null;

            public StateBuilder(string name)
            {
                _name = name;
            }

            public StateBuilder OnEnter(Action<SequenceBuilder> buildSequence)
            {
                _onEnterSequence = new SequenceBuilder();
                buildSequence(_onEnterSequence);
                return this;
            }

            public StateBuilder OnTrigger(string triggerId, string transitionTo)
            {
                throw new NotImplementedException();
            }

            public StateBuilder OnIntent<T>(
                string? transitionTo = null, 
                bool memorizeIntent = false,
                Func<T, Intent>? transmit = null)
            {
                throw new NotImplementedException();
            }
            
            internal void AppendStates(IList<StateDescription> destination)
            {
                
            }
        }

        public class ConversationStateBuilder
        {
            public ConversationStateBuilder Monitor(Frequency frequency)
            {
                throw new NotImplementedException();
            }

            public ConversationStateBuilder Transmit(Func<Intent> intent)
            {
                throw new NotImplementedException();
            }

            public ConversationStateBuilder Receive<T>(
                bool memorizeIntent = false,
                Func<Intent>? readback = null,
                string? transitionTo = null) where T : Intent
            {
                throw new NotImplementedException();
            }
        }

        public class SequenceBuilder
        {
            public SequenceBuilder AddDelayStep(TimeSpan fromMinutes)
            {
                throw new NotImplementedException();
            }

            public SequenceBuilder AddStep(Action action)
            {
                throw new NotImplementedException();
            }

            public SequenceBuilder AddTriggerStep(string triggerId)
            {
                throw new NotImplementedException();
            }

            internal void AppendStates(IList<StateDescription> destination)
            {
                
            }
        }
    }
}