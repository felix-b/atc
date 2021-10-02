using System.Collections.Immutable;

namespace Zero.Loss.Actors.Tests
{
    public class ParentActor : StatefulActor<ParentActor.ParentState>
    {
        public static readonly string TypeString = "test/parent";
            
        public record ParentState(string Str, ImmutableArray<ActorRef<ChildActor>> Children);

        public record ActivationEvent(string UniqueId, string Str) : IActivationStateEvent<ParentActor>;
        
        public record UpdateStrEvent(string NewStr) : IStateEvent;

        public record AddChildEvent(ActorRef<ChildActor> child) : IStateEvent;

        public record RemoveChildEvent(ActorRef<ChildActor> child) : IStateEvent;

        private readonly IStateStore _store;
        
        public ParentActor(ActivationEvent activation, IStateStore store) : base(
            TypeString, 
            activation.UniqueId, 
            new ParentState(activation.Str, ImmutableArray<ActorRef<ChildActor>>.Empty))
        {
            _store = store;
        }

        public void AddChild(ActorRef<ChildActor> child)
        {
            _store.Dispatch(this, new AddChildEvent(child));
        }

        public void RemoveChild(ActorRef<ChildActor> child)
        {
            _store.Dispatch(this, new RemoveChildEvent(child));
        }

        public void UpdateStr(string newStr)
        {
            _store.Dispatch(this, new UpdateStrEvent(newStr));
        }

        public string Str => State.Str;
        public ImmutableArray<ActorRef<ChildActor>> Children => State.Children;
            
        protected override ParentState Reduce(ParentState stateBefore, IStateEvent @event)
        {
            switch (@event)
            {
                case UpdateStrEvent update:
                    return stateBefore with {
                        Str = update.NewStr
                    };
                case AddChildEvent addChild:
                    return stateBefore with {
                        Children = stateBefore.Children.Add(addChild.child)
                    };
                case RemoveChildEvent removeChild:
                    return stateBefore with {
                        Children = stateBefore.Children.RemoveAll(c => c.UniqueId == removeChild.child.UniqueId)
                    };
                default:                
                    return stateBefore;
            }
        }

        public static ActorRef<ParentActor> Create(ISupervisorActor supervisor, string str)
        {
            return supervisor.CreateActor(id => new ActivationEvent(id, str));                
        }
            
        public static void RegisterType(ISupervisorActorInit supervisor)
        {
            supervisor.RegisterActorType<ParentActor, ActivationEvent>(
                TypeString, 
                (e, ctx) => new ParentActor(e, ctx.Resolve<IStateStore>()));
        }
    }
}