using System.Collections.Immutable;

namespace Zero.Loss.Actors.Tests
{
    public class ParentActor : StatefulActor<ParentActor.ParentState>
    {
        public static readonly string TypeString = "test/parent";
            
        public record ParentState(string Str, ImmutableArray<ActorRef<ChildActor>> Children);

        public record ActivationEvent(string UniqueId, string Str) : IActivationStateEvent<ParentActor>;

        public ParentActor(ActivationEvent activation) : base(
            TypeString, 
            activation.UniqueId, 
            new ParentState(activation.Str, ImmutableArray<ActorRef<ChildActor>>.Empty))
        {
        }

        public string Str => State.Str;
        public ImmutableArray<ActorRef<ChildActor>> Children => State.Children;
            
        protected override ParentState Reduce(ParentState state, IStateEvent @event)
        {
            return state;
        }

        public static ActorRef<ParentActor> Create(ISupervisorActor supervisor, string str)
        {
            return supervisor.CreateActor(id => new ActivationEvent(id, str));                
        }
            
        public static void RegisterType(ISupervisorActorInit supervisor)
        {
            supervisor.RegisterActorType<ParentActor, ActivationEvent>(
                TypeString, 
                (e, ctx) => new ParentActor(e));
        }
    }
}