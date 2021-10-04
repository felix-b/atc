namespace Zero.Loss.Actors
{
    public static class StatefulActorExtensions
    {
        public static ActorRef<TActor> GetRef<TActor>(this TActor actor, ISupervisorActor supervisor)
            where TActor : class, IStatefulActor
        {
            return supervisor.GetRefToActorInstance(actor);
        }
    }
}