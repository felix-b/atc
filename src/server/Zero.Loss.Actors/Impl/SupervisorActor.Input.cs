using System;
using System.Collections.Generic;

namespace Zero.Loss.Actors.Impl
{
    public partial class SupervisorActor : ISupervisorActor, ISupervisorActorInit
    {
        private readonly Dictionary<Type, ActorTypeRegistration> _registrationByActorType = new();
        private readonly Dictionary<Type, ActorTypeRegistration> _registrationByActivationEventType = new();
        private readonly IStateStore _stateStore;

        public SupervisorActor(string uniqueId, IStateStore stateStore) 
            : base(uniqueId, CreateInitialState())
        {
            _stateStore = stateStore;
        }

        public TActor CreateActor<TActor>(ActivationEventFactory<IActivationStateEvent<TActor>> activationEventFactory) 
            where TActor : class, IStatefulActor
        {
            if (!_registrationByActorType.TryGetValue(typeof(TActor), out var registration))
            {
                throw new KeyNotFoundException($"Actor with CLR type '{typeof(TActor).Name}' was not registered");
            }

            var instanceId = GetNextInstanceId(registration.TypeString);
            var uniqueId = $"{registration.TypeString}/#{instanceId}";
            var activationEvent = activationEventFactory(uniqueId);
            
            _stateStore.Dispatch(this, activationEvent);

            var actor = _lastCreatedActorTemp as TActor 
                ?? throw new Exception("Internal error: actor was not created or type mismatch.");
            _lastCreatedActorTemp = null;
            
            return actor;
        }

        public void RegisterActorType<TActor, TActivationEvent>(string type, ActorFactoryCallback<TActor, TActivationEvent> factory) 
            where TActor : class, IStatefulActor 
            where TActivationEvent : class, IActivationStateEvent
        {
            var registration = new ActorTypeRegistration(type, factory);
            
            _registrationByActorType.Add(typeof(TActor), registration);
            _registrationByActivationEventType.Add(typeof(TActivationEvent), registration);
        }

        private ulong GetNextInstanceId(string typeString)
        {
            return State.LastInstanceIdPerTypeString.TryGetValue(typeString, out var id)
                ? id
                : 1;
        }

        private record ActorTypeRegistration(string TypeString, Delegate Factory)
        {
            public ActorFactoryCallback<TActor, TActivationEvent> As<TActor, TActivationEvent>()
                where TActor : class, IStatefulActor
                where TActivationEvent : class, IActivationStateEvent

            {
                return (ActorFactoryCallback<TActor, TActivationEvent>)Factory;
            }
        }
    }
}