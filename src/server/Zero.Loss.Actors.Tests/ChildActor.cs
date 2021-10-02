namespace Zero.Loss.Actors.Tests
{
    public class ChildActor : StatefulActor<ChildActor.ChildState>
    {
        public static readonly string TypeString = "test/child";
        
        public record ChildState(int Num);

        public record ActivationEvent(string UniqueId, int Num) : IActivationStateEvent<ChildActor>;

        public record UpdateNumEvent(int NewNum) : IStateEvent;

        private readonly IStateStore _store;

        public ChildActor(ActivationEvent activation, IStateStore store) 
            : base(TypeString, activation.UniqueId, new ChildState(activation.Num))
        {
            _store = store;
        }

        public void UpdateNum(int newNum)
        {
            _store.Dispatch(this, new UpdateNumEvent(newNum));
        }


        public int Num => State.Num;
            
        protected override ChildState Reduce(ChildState stateBefore, IStateEvent @event)
        {
            switch (@event)
            {
                case UpdateNumEvent update:
                    return stateBefore with {
                        Num = update.NewNum
                    };
                default:                
                    return stateBefore;
            }
        }
            
        public static ActorRef<ChildActor> Create(ISupervisorActor supervisor, int num)
        {
            return supervisor.CreateActor(id => new ActivationEvent(id, num));                
        }

        public static void RegisterType(ISupervisorActorInit supervisor)
        {
            supervisor.RegisterActorType<ChildActor, ActivationEvent>(
                TypeString, 
                (e, ctx) => new ChildActor(e, ctx.Resolve<IStateStore>()));
        }
    }
}