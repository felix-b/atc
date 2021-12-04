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
    public class ImmutableStateMachine : IStateMachineContext
    {
        public static readonly string ResumeAfterDelayTriggerId = "RESUME_AFTER_DELAY";
        public static readonly string AnyIntentTriggerId = "ANY_INTENT";
        public static readonly StateEnterCallback NoopEnterCallback = (ctx) => { };
        public static readonly ImmutableDictionary<string, TransitionDescription> EmptyTransitionMap =
            ImmutableDictionary<string, TransitionDescription>.Empty;

        public readonly ulong Age;
        public readonly ImmutableDictionary<string, StateDescription> StateByName;
        public readonly StateDescription State;
        public readonly Intent? LastReceivedIntent;
        public readonly ImmutableDictionary<Type, Intent> MemorizedIntentByType;

        private readonly DispatchEventCallback _dispatchEvent;
        private readonly ScheduleDelayCallback _scheduleDelay;

        [NotEventSourced]
        private Action? _stateFinalizer = null;

        public ImmutableStateMachine(
            ImmutableDictionary<string, StateDescription> stateByName,
            string currentStateName,
            string? previousStateName,
            Intent? lastReceivedIntent,
            ImmutableDictionary<Type, Intent> memorizedIntentByType,
            DispatchEventCallback dispatchEvent,
            ScheduleDelayCallback scheduleDelay) 
        : this(
            age: 1,
            stateByName,
            currentStateName,
            previousStateName,
            lastReceivedIntent,
            memorizedIntentByType,
            dispatchEvent,
            scheduleDelay)
        {
        }

        public ImmutableStateMachine(
            ulong age,
            ImmutableDictionary<string, StateDescription> stateByName,
            string currentStateName,
            string? previousStateName,
            Intent? lastReceivedIntent,
            ImmutableDictionary<Type, Intent> memorizedIntentByType,
            DispatchEventCallback dispatchEvent,
            ScheduleDelayCallback scheduleDelay)
        {
            Age = age;
            StateByName = stateByName;
            State = currentStateName.Length > 0 ? StateByName[currentStateName] : StateDescription.Empty;
            LastReceivedIntent = lastReceivedIntent;
            MemorizedIntentByType = memorizedIntentByType;
            
            _dispatchEvent = dispatchEvent;
            _scheduleDelay = scheduleDelay;
        }

        public void Start()
        {
            State.OnEnter(this);
        }
            
        public bool ReceiveIntent(Intent intent)
        {
            var triggerId = GetTriggerIdFromIntentType(intent.GetType());
            
            if (State.TransitionByTriggerId.ContainsKey(triggerId))
            {
                _dispatchEvent(new TriggerEvent(Age, triggerId, intent));
                return true;
            }
            else if (State.TransitionByTriggerId.ContainsKey(AnyIntentTriggerId))
            {
                _dispatchEvent(new TriggerEvent(Age, AnyIntentTriggerId, intent));
                return true;
            }
            else
            {
                return false;
            }
        }

        public void ReceiveTrigger(string triggerId)
        {
            _dispatchEvent(new TriggerEvent(Age, triggerId, Intent: null));
        }

        public void TransitionTo(string stateName, bool resetLastReceivedIntent)
        {
            _dispatchEvent(new TransitionEvent(Age, stateName, resetLastReceivedIntent));
        }
        
        public T GetMemorizedIntent<T>() where T : Intent
        {
            return (T)MemorizedIntentByType[typeof(T)];
        }

        public bool TryGetMemorizedIntent<T>(out T? intent) where T : Intent
        {
            var found = MemorizedIntentByType.TryGetValue(typeof(T), out var anyIntent);
            intent = found ? (T?)anyIntent : null;
            return found;
        }

        public void ScheduleDelay(TimeSpan interval, Action onDue)
        {
            var handle = _scheduleDelay(interval, () => {
                try
                {
                    onDue();
                }
                catch (Exception e)
                {
                    //TODO: add log
                }
                finally
                {
                    ResetStateFinalizer();        
                }
            });
            
            SetStateFinalizer(() => handle.Cancel());
        }

        ulong IStateMachineContext.Age => Age;
        
        string IStateMachineContext.CurrentStateName => State.Name;

        Intent? IStateMachineContext.LastReceivedIntent => LastReceivedIntent;

        private void SetStateFinalizer(Action finalizer)
        {
            if (_stateFinalizer != null)
            {
                throw new InvalidOperationException("State finalizer callback was already set for this instance");
            }
            _stateFinalizer = finalizer;
        }
        
        private void RunStateFinalizer()
        {
            _stateFinalizer?.Invoke();
            _stateFinalizer = null;
        }

        private void ResetStateFinalizer()
        {
            _stateFinalizer = null;
        }

        public static readonly ImmutableStateMachine Empty = new ImmutableStateMachine(
            stateByName: ImmutableDictionary<string, StateDescription>.Empty, 
            currentStateName: string.Empty,
            previousStateName: null,
            lastReceivedIntent: null,
            memorizedIntentByType: ImmutableDictionary<Type, Intent>.Empty, 
            dispatchEvent: (e) => { },
            scheduleDelay: (t, onDue) => IDeferHandle.Noop);

        public static ImmutableStateMachine Reduce(
            ImmutableStateMachine machineBefore, 
            IImmutableStateMachineEvent @event)
        {
            var machineAfter = PureReduce();
            if (!object.ReferenceEquals(machineBefore, machineAfter))
            {
                machineBefore.RunStateFinalizer();
            }

            return machineAfter;

            ImmutableStateMachine PureReduce()
            {
                switch (@event)
                {
                    case TriggerEvent trigger:
                        return HandleTriggerEvent(trigger);
                    case TransitionEvent transitionTo:
                        return HandleTransitionEvent(transitionTo);
                    default:
                        return machineBefore;
                }
            }

            ImmutableStateMachine HandleTriggerEvent(TriggerEvent trigger)
            {
                var stateBefore = machineBefore.State;
                
                if (!stateBefore.TransitionByTriggerId.TryGetValue(trigger.TriggerId, out var transition))
                {
                    throw new KeyNotFoundException($"State '{stateBefore.Name}' has no transition for trigger '{trigger.TriggerId}'");
                }
                if (transition.MemorizeIntent && trigger.Intent == null)
                {
                    throw new InvalidOperationException($"Transition sets MemorizeIntent but trigger has null Intent");
                }

                return new ImmutableStateMachine(
                    age: machineBefore.Age + 1,
                    machineBefore.StateByName,
                    currentStateName: transition.TargetStateName,
                    previousStateName: machineBefore.State.Name,
                    lastReceivedIntent: trigger.Intent,
                    memorizedIntentByType: transition.MemorizeIntent 
                        ? machineBefore.MemorizedIntentByType.Add(trigger.Intent!.GetType(), trigger.Intent!)
                        : machineBefore.MemorizedIntentByType,
                    machineBefore._dispatchEvent,
                    machineBefore._scheduleDelay);
            }

            ImmutableStateMachine HandleTransitionEvent(TransitionEvent transitionTo)
            {
                return new ImmutableStateMachine(
                    age: machineBefore.Age + 1,
                    machineBefore.StateByName,
                    currentStateName: transitionTo.StateName,
                    previousStateName: machineBefore.State.Name,
                    lastReceivedIntent: transitionTo.ResetLastReceivedIntent 
                        ? null 
                        : machineBefore.LastReceivedIntent,
                    machineBefore.MemorizedIntentByType,
                    machineBefore._dispatchEvent,
                    machineBefore._scheduleDelay);
            }
        }

        private static string GetTriggerIdFromIntentType(Type intentType)
        {
            return intentType.FullName!;
        }

        public record StateDescription(
            string Name,
            StateEnterCallback OnEnter,
            ImmutableDictionary<string, TransitionDescription> TransitionByTriggerId)
        {
            public static readonly StateDescription Empty = new StateDescription(
                Name: string.Empty,
                OnEnter: (ctx) => { },
                TransitionByTriggerId: ImmutableDictionary<string, TransitionDescription>.Empty); 
        }
        
        public delegate void StateEnterCallback(IStateMachineContext context);

        public delegate void DispatchEventCallback(IStateEvent @event);

        public delegate IDeferHandle ScheduleDelayCallback(TimeSpan interval, Action onDue);

        public record TransitionDescription(
            string TriggerId,
            string TargetStateName,
            bool MemorizeIntent
        );

        public record TriggerEvent(
            ulong Age,
            string TriggerId,
            Intent? Intent = null) 
            : IStateEvent, IImmutableStateMachineEvent
        {
            public override string ToString()
            {
                return $"TRIG:{TriggerId}{(Intent != null ? $":{Intent}" : string.Empty)}";
            }
        }

        public record TransitionEvent(
            ulong Age,
            string StateName,
            bool ResetLastReceivedIntent) 
            : IStateEvent, IImmutableStateMachineEvent
        {
            public override string ToString()
            {
                return $"TRAN:{StateName}{(ResetLastReceivedIntent ? "+RLI" : string.Empty)}";
            }
        }
        
        public class Builder
        {
            private readonly string _initialStateName;
            private readonly DispatchEventCallback _dispatchEvent;
            private readonly ScheduleDelayCallback _scheduleDelay;
            private readonly Dictionary<string, IStateBuilder> _stateByName = new();

            public Builder(
                string initialStateName, 
                DispatchEventCallback dispatchEvent,
                ScheduleDelayCallback scheduleDelay)
            {
                _initialStateName = initialStateName;
                _dispatchEvent = dispatchEvent;
                _scheduleDelay = scheduleDelay;
            }
            
            public Builder AddState(string name, Action<RegularStateBuilder> build)
            {
                var newState = new RegularStateBuilder(name);
                _stateByName.Add(name, newState);
                build(newState);
                return this;
            }

            public Builder AddConversationState(IRadioOperatingActor actor, string name, Action<ConversationStateBuilder> build)
            {
                var newState = new ConversationStateBuilder(name, actor);
                _stateByName.Add(name, newState);
                build(newState);
                return this;
            }

            public ImmutableStateMachine Build()
            {
                var stateList = new List<StateDescription>();
                
                foreach (var topLevelState in _stateByName.Values)
                {
                    topLevelState.AppendStates(stateList);
                }
                
                return new ImmutableStateMachine(
                    stateList.ToImmutableDictionary(s => s.Name, s => s),
                    _initialStateName,
                    previousStateName: null,
                    lastReceivedIntent: null,
                    memorizedIntentByType: ImmutableDictionary<Type, Intent>.Empty, 
                    _dispatchEvent,
                    _scheduleDelay);
            }
        }

        public interface IStateBuilder
        {
            void AppendStates(IList<StateDescription> destination);
        }

        public class RegularStateBuilder : IStateBuilder
        {
            private readonly string _name;
            private readonly ImmutableDictionary<string, TransitionDescription>.Builder _transitionByTriggerId;
            private StateEnterCallback? _onEnterCallback = null;
            private SequenceBuilder? _onEnterSequence = null;

            public RegularStateBuilder(string name)
            {
                _name = name;
                _transitionByTriggerId = ImmutableDictionary.CreateBuilder<string, TransitionDescription>(); 
            }

            public RegularStateBuilder OnEnter(StateEnterCallback callback)
            {
                _onEnterSequence = null;
                _onEnterCallback = callback;
                return this;
            }

            public RegularStateBuilder OnEnterStartSequence(Action<SequenceBuilder> buildSequence)
            {
                _onEnterCallback = null;
                _onEnterSequence = new SequenceBuilder();
                buildSequence(_onEnterSequence);
                return this;
            }

            public RegularStateBuilder OnTrigger(string triggerId, string transitionTo, bool memorizeIntent = false)
            {
                _transitionByTriggerId.Add(
                    triggerId, 
                    new TransitionDescription(triggerId, transitionTo, memorizeIntent));

                return this;
            }

            public RegularStateBuilder OnIntent<T>(
                string transitionTo, 
                bool memorizeIntent = false)
            {
                var triggerId = GetTriggerIdFromIntentType(typeof(T));
                return OnTrigger(triggerId, transitionTo, memorizeIntent);
            }

            public RegularStateBuilder OnAnyIntent(
                string transitionTo, 
                bool memorizeIntent = false)
            {
                return OnTrigger(AnyIntentTriggerId, transitionTo, memorizeIntent);
            }

            void IStateBuilder.AppendStates(IList<StateDescription> destination)
            {
                if (HasSequence)
                {
                    AppendStatesWithSequence();
                }
                else
                {
                    destination.Add(new StateDescription(
                        _name,
                        _onEnterCallback ?? NoopEnterCallback,
                        _transitionByTriggerId.ToImmutable()));
                }

                void AppendStatesWithSequence()
                {
                    var firstSequenceStateName = _onEnterSequence!.GetFirstStepStateName(_name);
                    var immutableTransitions = _transitionByTriggerId.ToImmutable();
                    var thisState = new StateDescription(
                        _name,
                        machine => {
                            machine.TransitionTo(firstSequenceStateName, resetLastReceivedIntent: false);
                        },
                        immutableTransitions);
                    var finishState = new StateDescription(
                        SequenceBuilder.GetSequenceFinishStateName(_name),
                        NoopEnterCallback,
                        immutableTransitions);

                    destination.Add(thisState);

                    _onEnterSequence!.AppendStates(thisState, finishState, destination);

                    destination.Add(finishState);
                }
            }

            internal bool HasSequence => _onEnterSequence != null && !_onEnterSequence.IsEmpty;
        }

        public class ConversationStateBuilder : IStateBuilder
        {
            private readonly string _name;
            private readonly IRadioOperatingActor _actor;
            private readonly List<ReceiveEntry> _receive = new();
            private readonly ImmutableDictionary<string, TransitionDescription>.Builder _transitionByTriggerId;
            private Frequency? _monitor = null;
            private StateEnterCallback? _onEnterCallback = null;
            private Func<Intent>? _transmit = null;
            private string? _afterTransmitTransitionTo = null;

            public ConversationStateBuilder(string name, IRadioOperatingActor actor)
            {
                _name = name;
                _actor = actor;
                _transitionByTriggerId = ImmutableDictionary.CreateBuilder<string, TransitionDescription>(); 
            }

            public ConversationStateBuilder OnEnter(StateEnterCallback callback)
            {
                _onEnterCallback = callback;
                return this;
            }

            public ConversationStateBuilder OnTrigger(string triggerId, string transitionTo, bool memorizeIntent = false)
            {
                _transitionByTriggerId.Add(
                    triggerId, 
                    new TransitionDescription(triggerId, transitionTo, memorizeIntent));

                return this;
            }

            public ConversationStateBuilder Monitor(Frequency frequency)
            {
                _monitor = frequency;
                return this;
            }

            public ConversationStateBuilder Transmit(Func<Intent> intent, string? transitionTo = null)
            {
                _transmit = intent;
                _afterTransmitTransitionTo = transitionTo;
                return this;
            }

            public ConversationStateBuilder Receive<T>(
                bool memorizeIntent = false,
                Func<Intent>? readback = null,
                string? transitionTo = null) where T : Intent
            {
                _receive.Add(new ReceiveEntry(
                    IntentType: typeof(T),
                    memorizeIntent,
                    readback,
                    transitionTo));
                return this;
            }

            void IStateBuilder.AppendStates(IList<StateDescription> destination)
            {
                var readyStateName = $"{_name}/$READY";
                    
                AppendReceiveStates(destination, out var receiveStateName);
                AppendInitialTransmitStates(
                    destination, 
                    afterTransmissionTransitionTo: receiveStateName ?? _afterTransmitTransitionTo ?? readyStateName, 
                    out var mainNextStateName);

                CreateMonitorPart(out var onMainStateEnter);

                var mainState = new StateDescription(
                    _name,
                    onMainStateEnter,
                    _transitionByTriggerId.ToImmutable());
                destination.Add(mainState);

                var needReadyState = _transmit == null && _receive.Count == 0;
                if (needReadyState)
                {
                    destination.Add(new StateDescription(
                        readyStateName,
                        NoopEnterCallback,
                        _transitionByTriggerId.ToImmutable()));
                }
                
                void CreateMonitorPart(out StateEnterCallback outOnMainStateEnter)
                {
                    outOnMainStateEnter = machine => {
                        if (_monitor != null)
                        {
                            _actor.MonitorFrequency(_monitor.Value);
                        }
                        _onEnterCallback?.Invoke(machine);
                        machine.TransitionTo(mainNextStateName);
                    };
                }

                void AppendInitialTransmitStates(
                    IList<StateDescription> outReceiveStateList,
                    string afterTransmissionTransitionTo, 
                    out string firstState)
                {
                    if (_transmit == null)
                    {
                        firstState = afterTransmissionTransitionTo;
                        return;
                    }
                    
                    AppendTransmissionStates(
                        outReceiveStateList,
                        _transmit,
                        baseStateName: $"{_name}/TRANSMIT",
                        afterTransmissionTransitionTo,
                        out firstState);
                }

                void AppendReceiveStates(IList<StateDescription> outReceiveStateList, out string? receiveStateName)
                {
                    if (_receive.Count == 0)
                    {
                        receiveStateName = null;
                        return;
                    }

                    var transitions = EmptyTransitionMap;
                    var listIndex = outReceiveStateList.Count;
                    
                    for (int i = 0 ; i < _receive.Count ; i++)
                    {
                        var entry = _receive[i];
                        var triggerId = GetTriggerIdFromIntentType(entry.IntentType);

                        if (entry.Readback != null)
                        {
                            AppendTransmissionStates(
                                outReceiveStateList, 
                                intentFactory: entry.Readback, 
                                baseStateName: $"{_name}/READBACK_OF_{entry.IntentType.Name.ToUpper()}",
                                afterTransmissionTransitionTo: entry.TransitionTo,
                                out var readbackStateName);
                            transitions = transitions.AddTransition(triggerId, readbackStateName, entry.MemorizeIntent);
                        }
                        else if (entry.TransitionTo != null)
                        {
                            transitions = transitions.AddTransition(triggerId, entry.TransitionTo, entry.MemorizeIntent);
                        }
                    }

                    receiveStateName = $"{_name}/AWAIT_RECEIVE"; 
                    outReceiveStateList.Insert(listIndex, new StateDescription(
                        receiveStateName,
                        NoopEnterCallback,
                        transitions));
                }
                
                void AppendTransmissionStates(
                    IList<StateDescription> outReceiveStateList, 
                    Func<Intent> intentFactory,
                    string baseStateName,
                    string? afterTransmissionTransitionTo,
                    out string awaitStateName)
                {
                    awaitStateName = $"{baseStateName}/AWAIT_SILENCE";

                    var transmitState = new StateDescription(
                        $"{baseStateName}/TRANSMIT",
                        NoopEnterCallback,
                        TransitionByTriggerId: afterTransmissionTransitionTo != null
                            ? EmptyTransitionMap.AddTransition(
                                AIRadioOperatingActor.TransmissionFinishedTriggerId,
                                afterTransmissionTransitionTo,
                                memorizeIntent: false)
                            : EmptyTransitionMap
                    );

                    var awaitState = new StateDescription(
                        awaitStateName,
                        OnEnter: machne => {
                            var intent = intentFactory();
                            _actor.InitiateTransmission(intent);
                        },
                        EmptyTransitionMap.AddTransition(
                            AIRadioOperatingActor.TransmissionStartedTriggerId,
                            transmitState.Name,
                            memorizeIntent: false)
                    );
                    
                    outReceiveStateList.Add(awaitState);
                    outReceiveStateList.Add(transmitState);
                }
            }

            private record ReceiveEntry(
                Type IntentType,
                bool MemorizeIntent,
                Func<Intent>? Readback,
                string? TransitionTo
            );
        }

        public class SequenceBuilder
        {
            private readonly List<Step> _steps = new();
            
            public SequenceBuilder AddDelayStep(string name, TimeSpan interval, bool inheritTriggers)
            {
                _steps.Add(new DelayStep(name, interval, inheritTriggers));
                return this;
            }

            public SequenceBuilder AddStep(string name, StateEnterCallback action)
            {
                _steps.Add(new ActionStep(name, action));
                return this;
            }

            public SequenceBuilder AddTriggerStep(string name, string triggerId)
            {
                _steps.Add(new TriggrtStep(name, triggerId));
                return this;
            }

            public SequenceBuilder AddTransitionStep(string name, string targetStateName)
            {
                _steps.Add(new TransitionStep(name, targetStateName));
                return this;
            }

            internal void AppendStates(StateDescription parentState, StateDescription finishState, IList<StateDescription> destination)
            {
                for (int i = 0 ; i < _steps.Count ; i++)
                {
                    _steps[i].AppendStates(
                        parentState,
                        finishState,
                        nextStep: i < _steps.Count - 1 ? _steps[i+1] : null,
                        destination);
                }
            }

            internal string GetFirstStepStateName(string parentStateName)
            {
                if (IsEmpty)
                {
                    throw new InvalidOperationException("Current sequence has no steps");
                }

                return GetStepStateName(parentStateName, _steps[0]);
            }

            internal bool IsEmpty => _steps.Count == 0;

            internal static string GetSequenceFinishStateName(string parentStateName)
            {
                return $"{parentStateName}/$READY";
            }

            internal static string GetStepStateName(string parentStateName, Step step)
            {
                return $"{parentStateName}/{step.Name}";
            }

            internal abstract record Step(string Name)
            {
                public abstract void AppendStates(
                    StateDescription parentState,
                    StateDescription finishState,
                    Step? nextStep,
                    IList<StateDescription> destination);

                protected string GetNextStateName(
                    StateDescription parentState,
                    Step? nextStep)
                {
                    return nextStep != null
                        ? GetStepStateName(parentState.Name, nextStep)
                        : GetSequenceFinishStateName(parentState.Name);
                }
            }

            private record ActionStep(string Name, StateEnterCallback Action) : Step(Name)
            {
                public override void AppendStates(
                    StateDescription parentState,
                    StateDescription finishState,
                    Step? nextStep,
                    IList<StateDescription> destination)
                {
                    var nextStateName = GetNextStateName(parentState, nextStep);
                    var state = new StateDescription(
                        Name: GetStepStateName(parentState.Name, this),
                        OnEnter: machine => {
                            this.Action(machine);
                            machine.TransitionTo(nextStateName);
                        },
                        TransitionByTriggerId: EmptyTransitionMap
                    );
                    destination.Add(state);
                }
            }

            private record TransitionStep(string Name, string TargetStateName) : Step(Name)
            {
                public override void AppendStates(
                    StateDescription parentState,
                    StateDescription finishState,
                    Step? nextStep,
                    IList<StateDescription> destination)
                {
                    var state = new StateDescription(
                        Name: GetStepStateName(parentState.Name, this),
                        OnEnter: machine => {
                            machine.TransitionTo(TargetStateName);
                        },
                        TransitionByTriggerId: EmptyTransitionMap
                    );
                    destination.Add(state);
                }
            }
            
            private record TriggrtStep(string Name, string TriggerId) : Step(Name)
            {
                public override void AppendStates(
                    StateDescription parentState,
                    StateDescription finishState,
                    Step? nextStep,
                    IList<StateDescription> destination)
                {
                    var nextStateName = GetNextStateName(parentState, nextStep);
                    var state = new StateDescription(
                        Name: GetStepStateName(parentState.Name, this),
                        OnEnter: machine => {
                            machine.ReceiveTrigger(TriggerId);
                        },
                        TransitionByTriggerId: finishState.TransitionByTriggerId 
                    );
                    destination.Add(state);
                }
            }

            private record DelayStep(string Name, TimeSpan Interval, bool InheritTriggers) : Step(Name)
            {
                public override void AppendStates(
                    StateDescription parentState,
                    StateDescription finishState,
                    Step? nextStep,
                    IList<StateDescription> destination)
                {
                    var thisStateName = GetStepStateName(parentState.Name, this);
                    var nextStateName = GetNextStateName(parentState, nextStep);

                    StateEnterCallback onEnter = (machine) => {
                        machine.ScheduleDelay(Interval, () => {
                            machine.ReceiveTrigger(ResumeAfterDelayTriggerId);
                        });
                    };

                    var baseTransitions = InheritTriggers
                        ? parentState.TransitionByTriggerId
                        : ImmutableDictionary<string, TransitionDescription>.Empty;
                    
                    var state = new StateDescription(
                        Name: thisStateName,
                        OnEnter: onEnter,
                        TransitionByTriggerId: baseTransitions.Add(
                            ResumeAfterDelayTriggerId,
                            new TransitionDescription(
                                ResumeAfterDelayTriggerId, 
                                nextStateName, 
                                MemorizeIntent: false)));
                    destination.Add(state);
                }
            }
        }
    }
    
    public interface IStateMachineContext
    {
        bool ReceiveIntent(Intent intent);
        void ReceiveTrigger(string triggerId);
        void TransitionTo(string stateName, bool resetLastReceivedIntent = true);
        T GetMemorizedIntent<T>() where T : Intent;
        bool TryGetMemorizedIntent<T>(out T? intent) where T : Intent;
        void ScheduleDelay(TimeSpan interval, Action onDue);
        ulong Age { get; }
        string CurrentStateName { get; }
        Intent? LastReceivedIntent { get; }
    }

    public static class TransitionMapExtensions
    {
        public static ImmutableDictionary<string, ImmutableStateMachine.TransitionDescription> AddTransition(
            this ImmutableDictionary<string, ImmutableStateMachine.TransitionDescription> source,
            string triggerId,
            string targetStateName, 
            bool memorizeIntent)
        {
            return source.Add(triggerId, new ImmutableStateMachine.TransitionDescription(
                triggerId, 
                targetStateName, 
                memorizeIntent));
        }
    }

    public interface IImmutableStateMachineEvent : IStateEvent
    {
        ulong Age { get; }
    }
}
