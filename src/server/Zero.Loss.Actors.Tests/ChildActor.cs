namespace Zero.Loss.Actors.Tests
{
    public class ChildActor : StatefulActor<ChildActor.ChildState>
    {
        public static readonly string TypeString = "test/child";
        
        public record ChildState(int Num);

        public record ActivationEvent(string UniqueId, int Num) : IActivationStateEvent<ChildActor>;

        public ChildActor(ActivationEvent activation) 
            : base(TypeString, activation.UniqueId, new ChildState(activation.Num))
        {
        }

        public int Num => State.Num;
            
        protected override ChildState Reduce(ChildState state, IStateEvent @event)
        {
            return state;
        }
            
        public static ActorRef<ChildActor> Create(ISupervisorActor supervisor, int num)
        {
            return supervisor.CreateActor(id => new ActivationEvent(id, num));                
        }

        public static void RegisterType(ISupervisorActorInit supervisor)
        {
            supervisor.RegisterActorType<ChildActor, ActivationEvent>(
                TypeString, 
                (e, ctx) => new ChildActor(e));
        }
    }
}