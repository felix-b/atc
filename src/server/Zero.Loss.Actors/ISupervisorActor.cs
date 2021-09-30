using System;
using System.Collections.Generic;

namespace Zero.Loss.Actors
{
    public interface ISupervisorActor
    {
        TActor CreateActor<TActor>(ActivationEventFactory<IActivationStateEvent<TActor>> activationEventFactory)
            where TActor : class, IStatefulActor;
        
        bool TryGetActorById<TActor>(string uniqueId, out TActor? actor)
            where TActor : class, IStatefulActor;

        IEnumerable<TActor> GetAllActorsOfType<TActor>()
            where TActor : class, IStatefulActor;
    }

    public interface IActorDependencyContext
    {
        T Resolve<T>() where T : class;
    }

    public delegate TActivationEvent ActivationEventFactory<TActivationEvent>(string uniqueId)
        where TActivationEvent : class, IActivationStateEvent;

    public delegate TActor ActorFactoryCallback<TActor, TActivationEvent>(TActivationEvent constructorEvent, IActorDependencyContext dependencies)
        where TActor : class, IStatefulActor
        where TActivationEvent : class, IActivationStateEvent;
    
    public interface ISupervisorActorInit
    {
        void RegisterActorType<TActor, TActivationEvent>(string type, ActorFactoryCallback<TActor, TActivationEvent> factory) 
            where TActor : class, IStatefulActor
            where TActivationEvent : class, IActivationStateEvent;
    }
}
