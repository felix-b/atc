namespace Zero.Loss.Actors.Tests
{
    public class StartableActor : StatefulActor<StartableActor.StartableState>, IStartableActor
    {
        public static readonly string TypeString = "test/startable";
        
        public record StartableState(int StartCount);

        public record ActivationEvent(string UniqueId) : IActivationStateEvent<StartableActor>;

        public record IncrementStartCount() : IStateEvent;

        private readonly IStateStore _store;

        public StartableActor(ActivationEvent activation, IStateStore store) 
            : base(TypeString, activation.UniqueId, new StartableState(StartCount: 0))
        {
            _store = store;
        }

        void IStartableActor.Start()
        {
            _store.Dispatch(this, new IncrementStartCount());
        }

        public int StartCount => State.StartCount;
            
        protected override StartableState Reduce(StartableState stateBefore, IStateEvent @event)
        {
            switch (@event)
            {
                case IncrementStartCount update:
                    return stateBefore with {
                        StartCount = stateBefore.StartCount + 1
                    };
                default:                
                    return stateBefore;
            }
        }
            
        public static ActorRef<StartableActor> Create(ISupervisorActor supervisor)
        {
            return supervisor.CreateActor(id => new ActivationEvent(id));                
        }

        public static void RegisterType(ISupervisorActorInit supervisor)
        {
            supervisor.RegisterActorType<StartableActor, ActivationEvent>(
                TypeString, 
                (e, ctx) => new StartableActor(e, ctx.Resolve<IStateStore>()));
        }
    }
}