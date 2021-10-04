using System;
using System.Collections.Generic;
using Zero.Loss.Actors.Impl;

namespace Zero.Loss.Actors
{
    public interface ISupervisorActor
    {
        ActorRef<TActor> CreateActor<TActor>(ActivationEventFactory<IActivationStateEvent<TActor>> activationEventFactory)
            where TActor : class, IStatefulActor;

        void DeleteActor<TActor>(ActorRef<TActor> actor)
            where TActor : class, IStatefulActor;

        bool TryGetActorById<TActor>(string uniqueId, out ActorRef<TActor>? actor)
            where TActor : class, IStatefulActor;

        IEnumerable<ActorRef<TActor>> GetAllActorsOfType<TActor>()
            where TActor : class, IStatefulActor;
        
        ActorRef<TActor> GetRefToActorInstance<TActor>(TActor actorInstance)
            where TActor : class, IStatefulActor;
        
        ISupervisorActorTimeTravel TimeTravel { get; }

        public ActorRef<TActor> GetActorByIdOrThrow<TActor>(string uniqueId)
            where TActor : class, IStatefulActor
        {
            if (TryGetActorById<TActor>(uniqueId, out var actor))
            {
                return actor!.Value;
            }

            throw new ActorNotFoundException($"Actor '{uniqueId}' could not be found");
        }
    }

    public interface ISupervisorActorTimeTravel
    {
        ActorStateSnapshot TakeSnapshot();
        void RestoreSnapshot(ActorStateSnapshot snapshot);
        void ReplayEvents(IEnumerable<StateEventEnvelope> events);
    }
    
    public interface IActorDependencyContext
    {
        T Resolve<T>() where T : class;
    }

    public sealed class ActorStateSnapshot
    {
        internal ActorStateSnapshot(ulong nextSequenceNo, object opaque)
        {
            NextSequenceNo = nextSequenceNo;
            Opaque = opaque;
        }

        public ulong NextSequenceNo { get; }
        
        public object Opaque { get; }
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
