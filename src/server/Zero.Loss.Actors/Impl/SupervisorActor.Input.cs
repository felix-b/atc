using System;
using System.Collections.Generic;

namespace Zero.Loss.Actors.Impl
{
    public partial class SupervisorActor : ISupervisorActor, ISupervisorActorInit
    {
        public static readonly string TypeString = "SUPERVISOR";
        
        private readonly Dictionary<Type, ActorTypeRegistration> _registrationByActorType = new();
        private readonly Dictionary<Type, ActorTypeRegistration> _registrationByActivationEventType = new();
        private readonly IInternalStateStore _stateStore;
        private readonly IActorDependencyContext _dependencyContext;

        public SupervisorActor(IInternalStateStore stateStore, IActorDependencyContext dependencyContext) 
            : base(TypeString, $"{TypeString}/#1", CreateInitialState())
        {
            _stateStore = stateStore;
            _dependencyContext = dependencyContext;

            var selfEntry = new ActorEntry(this, new DummySelfActivationEvent(UniqueId), SequenceNo: 0);
            (this as IStatefulActor).SetState(State with {
                ActorByUniqueId = State.ActorByUniqueId.Add(UniqueId, selfEntry)
            });
        }

        public ActorRef<TActor> CreateActor<TActor>(ActivationEventFactory<IActivationStateEvent<TActor>> activationEventFactory) 
            where TActor : class, IStatefulActor
        {
            if (!_registrationByActorType.TryGetValue(typeof(TActor), out var registration))
            {
                throw new ActorTypeNotFoundException($"Actor with CLR type '{typeof(TActor).Name}' was not registered");
            }

            var instanceId = GetNextInstanceId(registration.TypeString);
            var uniqueId = $"{registration.TypeString}/#{instanceId}";
            var activationEvent = activationEventFactory(uniqueId);
            
            _stateStore.Dispatch(this, activationEvent);

            var actor = State.LastCreatedActor as TActor 
                ?? throw new Exception("Internal error: actor was not created or type mismatch.");

            (actor as IStartableActor)?.Start();
            
            _stateStore.Dispatch(this, new ClearLastCreatedActorEvent());
            return new ActorRef<TActor>(this, actor.UniqueId);
        }

        public void DeleteActor<TActor>(ActorRef<TActor> actor)
            where TActor : class, IStatefulActor
        {
            if (!State.ActorByUniqueId.TryGetValue(actor.UniqueId, out var actorEntry))
            {
                throw new ActorNotFoundException($"Actor '{actor.UniqueId}' not found");
            }

            if (!(actorEntry.Actor is TActor))
            {
                throw new ActorTypeMismatchException(
                    $"Expected actor '{actor.UniqueId}' to be '{typeof(TActor).Name}', but found '{actorEntry.Actor.GetType().Name}'");
            }
            
            _stateStore.Dispatch(this, new DeactivateActorEvent(actor.UniqueId));
        }

        public void RegisterActorType<TActor, TActivationEvent>(string type, ActorFactoryCallback<TActor, TActivationEvent> factory) 
            where TActor : class, IStatefulActor 
            where TActivationEvent : class, IActivationStateEvent
        {
            Func<IActivationStateEvent, IStatefulActor> genericFactory = e => {
                var typedEvent = (TActivationEvent) e;
                return factory(typedEvent, _dependencyContext);
            };
            
            var registration = new ActorTypeRegistration(type, genericFactory);
            
            _registrationByActorType.Add(typeof(TActor), registration);
            _registrationByActivationEventType.Add(typeof(TActivationEvent), registration);
        }

        private ulong GetNextInstanceId(string typeString)
        {
            return State.LastInstanceIdPerTypeString.TryGetValue(typeString, out var id)
                ? id + 1
                : 1;
        }

        private record ActorTypeRegistration(string TypeString, Func<IActivationStateEvent, IStatefulActor> Factory);
    }
}